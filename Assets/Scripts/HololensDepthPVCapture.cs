#define INCLUDE_TSDF
#define ONLY_UNPROJECT
#define COLOR_FROM_PLUGIN

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.Windows.WebCam;
using System.Linq;
//using Microsoft.MixedReality.Toolkit;

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
using HL2UnityPlugin;
#endif
#endif

public class HololensDepthPVCapture : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
    HL2ResearchMode researchMode;
#endif
#endif

	[SerializeField]
	float _captureTime = 1f;
	
	public RenderTexture _colorRT;
	//public RenderTexture _depthRT;
	
	public Material _colorCopyMaterial;
	public Material _depthCopyMaterial;
	
	Texture2D _ourColor = null;
	//Texture2D _ourDepth = null;
	Texture2D targetTexture = null;
#if COLOR_FROM_PLUGIN
	Texture2D targetTexturePNG = null;
#endif

#if ONLY_UNPROJECT
	ComputeBuffer _ourPoints = null;
	ComputeBuffer _ourColorBuffer = null;
	float[] _pointData;
	uint[] _colorData;
#endif

	bool _isCapturing = false;
	
	float _lastCaptureTime = 0.0f;
	
	const int DEPTH_WIDTH = 320;
	const int DEPTH_HEIGHT = 288;
	
	const int DEPTH_RESOLUTION = DEPTH_WIDTH * DEPTH_HEIGHT;
	//3904 x 2196...
	//1952 x 1100...
	const int COLOR_WIDTH = 760;
	const int COLOR_HEIGHT = 428;
	
	byte[] depthTextureBytes = new byte[DEPTH_RESOLUTION];
	byte[] depthTextureFilteredBytes = new byte[DEPTH_RESOLUTION*2];
	byte[] floatDepthTextureBytesX = new byte[DEPTH_RESOLUTION*4];
	byte[] floatDepthTextureBytesY = new byte[DEPTH_RESOLUTION*4];
	byte[] floatDepthTextureBytesZ = new byte[DEPTH_RESOLUTION*4];
	

#if INCLUDE_TSDF
	
	const uint NUM_GRIDS = 4096;		
	const uint NUM_GRIDS_CPU = 4096;
	const uint TOTAL_GRID_SIZE_X = 1024;
	const uint TOTAL_GRID_SIZE_Y = 512;
	const uint TOTAL_GRID_SIZE_Z = 1024;

	const uint GRID_SIZE_X = 32;
	const uint GRID_SIZE_Y = 16;
	const uint GRID_SIZE_Z = 32;
	
	const uint TOTAL_NUM_OCTANTS = (TOTAL_GRID_SIZE_X/GRID_SIZE_X) * (TOTAL_GRID_SIZE_Y/GRID_SIZE_Y) * (TOTAL_GRID_SIZE_Z/GRID_SIZE_Z);

#if SINGLE_GRID
	Vector4 volumeGridSize = new Vector4(512f, 256f, 512f, 0f);
	const uint TOTAL_CELLS = 512 * 256 * 512;
#else
	Vector4 volumeGridSize = new Vector4((float)TOTAL_GRID_SIZE_X, (float)TOTAL_GRID_SIZE_Y, (float)TOTAL_GRID_SIZE_Z, 0f);
	const uint TOTAL_CELLS = GRID_SIZE_X * GRID_SIZE_Y * GRID_SIZE_Z;
#endif

	const uint GRID_BYTE_COUNT = TOTAL_CELLS * sizeof(ushort);
	const uint COLOR_BYTE_COUNT = TOTAL_CELLS * sizeof(uint);

	//store where each possible "octant" maps to on the GPU, -1 if not on gpu.
	public ComputeShader _tsdfShader;

	ComputeBuffer octantBuffer=null;

	int[] octantData = null;

	Vector4 volumeBounds = new Vector4(16f, 8f, 16f, 0f);
	Vector4 volumeOrigin = new Vector4(0f, 0f, 0f, 0f);		
	Vector4 cellDimensions = new Vector4(32f, 16f, 32f, 0f);

	int clearID = -1;
	int clearTextureID = -1;
	int octantComputeID = -1;

	uint _totalRes = 0;
	uint _currWidth = 0;
	uint _currHeight = 0;

	int _lastCopyCount = 0;

	bool _waitingForGrids = false;
	bool _gridsFound = false;
	
	const float SCREEN_MULTIPLIER = 1.0f;

#endif

	[SerializeField]
	bool _writeImagesToDisk = false;
	public bool WriteImagesToDisk => _writeImagesToDisk;
	
	bool _firstHeadsetSend = true;
	
	float _startTimer = 0f;
	
	int _fileOutNumber = 0;
	
	bool _firstLoop = true;
	
    void Start()
    {
		// Depth sensor should be initialized in only one mode
        targetTexture = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGBA32, false);
#if COLOR_FROM_PLUGIN
		targetTexturePNG = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGBA32, false);
#endif
		_ourColor = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RGBA32, false);
		//_ourDepth = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.R8, false);
#if ONLY_UNPROJECT
		_ourPoints = new ComputeBuffer(DEPTH_RESOLUTION, sizeof(float)*4);
		_ourColorBuffer = new ComputeBuffer(DEPTH_RESOLUTION, sizeof(uint));
		_pointData = new float[DEPTH_WIDTH*DEPTH_HEIGHT*4];
		_colorData = new uint[DEPTH_WIDTH*DEPTH_HEIGHT];
#endif
		_startTimer = UnityEngine.Time.time;
		
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode = new HL2ResearchMode();
	
        researchMode.SetPointCloudDepthOffset(0);
		
		
		//if(longDepthPreviewPlane != null)
		//{
			researchMode.InitializeLongDepthSensor();
			//researchMode.InitializePVCamera();
			
			researchMode.StartLongDepthSensorLoop();
			researchMode.SetShouldGetDepth();
#if COLOR_FROM_PLUGIN
			researchMode.StartPVCameraLoop();
#endif
			//PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		/*}
		else if(depthPreviewPlane && shortAbImagePreviewPlane)
		{
			researchMode.InitializeDepthSensor();
			researchMode.StartDepthSensorLoop();
		}*/

		researchMode.InitializeSpatialCamerasFront();
		researchMode.StartSpatialCamerasFrontLoop();
		
#endif
#endif
		
#if INCLUDE_TSDF
		InitializeTSDF();
#endif
    }

	void OnDestroy()
	{
		_isCapturing = false;
#if INCLUDE_TSDF

		if(octantBuffer != null)
		{
			octantBuffer.Release();
			octantBuffer = null;
		}

#endif

	}
	
#if INCLUDE_TSDF
	void InitializeTSDF()
	{
		Debug.Log("Initializing TSDF");
		
		//_depthTexFromHololensX = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		//_depthTexFromHololensY = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		//_depthTexFromHololensZ = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		
		if(_tsdfShader != null)
		{

			if(clearID == -1)
			{
				clearID = _tsdfShader.FindKernel("CSClear");
				if(clearID != -1)
				{
					Debug.Log("Found clear shader");
				}
			}

			if(clearTextureID == -1)
			{
				clearTextureID = _tsdfShader.FindKernel("CSClearTexture");
				if(clearTextureID != -1)
				{
					Debug.Log("Found clear texture");
				}
			}

			if(octantComputeID == -1)
			{
				octantComputeID = _tsdfShader.FindKernel("CSOctant");
				if(octantComputeID != -1)
				{
					Debug.Log("Found CSOctant shader");
				}
			}
		}

		if(octantBuffer == null)
		{
			octantBuffer = new ComputeBuffer(DEPTH_RESOLUTION, sizeof(int));
			octantData = new int[DEPTH_RESOLUTION];
			//Debug.Log("Image depth: " + imageDepth.width + " " + imageDepth.height);
			for(int i = 0; i < DEPTH_RESOLUTION; ++i)
			{
				octantData[i] = -1;
			}
		}

		octantBuffer.SetData(octantData);

		int screenWidthMult = (int)((float)Screen.width * SCREEN_MULTIPLIER);
		int screenHeightMult = (int)((float)Screen.height * SCREEN_MULTIPLIER);

		_totalRes = DEPTH_RESOLUTION;

		_currWidth = (uint)DEPTH_WIDTH;
		_currHeight = (uint)DEPTH_HEIGHT;

		//Debug.Log(_currWidth + " " +_currHeight);

		_tsdfShader.SetInt("numVolumes", (int)NUM_GRIDS);
		if((NUM_GRIDS*TOTAL_CELLS+1023)/1024 > 65535)
		{
			int numCalls = (int)(NUM_GRIDS*TOTAL_CELLS / (1024*65535)) + 1;
			int totalCount = 0;
			int singleCallMax = (65535 * 1024);
			for(int j = 0; j < numCalls; ++j)
			{
				_tsdfShader.SetInt("volumeOffset", j * singleCallMax);
				if((j+1) * singleCallMax > NUM_GRIDS*TOTAL_CELLS)
				{
					_tsdfShader.Dispatch(clearID, (int)((NUM_GRIDS*TOTAL_CELLS-totalCount) + 1023) / 1024, 1, 1);
				}
				else
				{
					_tsdfShader.Dispatch(clearID, singleCallMax / 1024, 1, 1);
				}
				totalCount += singleCallMax;
			}
		}
		else
		{
			_tsdfShader.Dispatch(clearID, ((int)NUM_GRIDS*(int)TOTAL_CELLS + 1023) / 1024, 1, 1);
		}

		float gridSizeDiag = (float)(volumeBounds.magnitude / volumeGridSize.magnitude) * (float)1.05f;
		//float gridSizeDiag = volumeBounds.x / volumeGridSize.x;

		_tsdfShader.SetFloat("gridSizeDiag", gridSizeDiag);
		_tsdfShader.SetVector("volumeBounds", volumeBounds);
		_tsdfShader.SetVector("volumeOrigin", volumeOrigin);
		_tsdfShader.SetVector("volumeGridSize", volumeGridSize);
		_tsdfShader.SetVector("volumeGridSizeWorld", new Vector4(volumeBounds.x / volumeGridSize.x, volumeBounds.y / volumeGridSize.y, volumeBounds.z / volumeGridSize.z, 1f));
		_tsdfShader.SetVector("octantDimensions", new Vector4(volumeGridSize.x / cellDimensions.x, volumeGridSize.y / cellDimensions.y, volumeGridSize.z / cellDimensions.z, 1f));
		_tsdfShader.SetVector("octantWorldLength", new Vector4(volumeBounds.x / (volumeGridSize.x / cellDimensions.x), volumeBounds.y / (volumeGridSize.y / cellDimensions.y), volumeBounds.z / (volumeGridSize.z / cellDimensions.z), 1.0f));
		_tsdfShader.SetVector("volumeMin", volumeOrigin - volumeBounds * 0.5f);
		_tsdfShader.SetVector("cellDimensions", cellDimensions);
		_tsdfShader.SetFloat("depthWidth", (float)_currWidth);
		_tsdfShader.SetFloat("depthHeight", (float)_currHeight);
		_tsdfShader.SetFloat("depthResolution", _totalRes);
		_tsdfShader.SetInt("orientation", (int)Screen.orientation);
		_tsdfShader.SetInt("screenWidth", screenWidthMult);
		_tsdfShader.SetInt("screenHeight", screenHeightMult);
		_tsdfShader.SetInt("volumeOffset", 0);
		_tsdfShader.SetInt("totalCells", (int)TOTAL_CELLS);
		_tsdfShader.SetInt("computeMaxEdgeSize", 256);

		_tsdfShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//_tsdfShader.SetTexture(octantComputeID, "depthTextureX", _depthTexFromHololensX);
		//_tsdfShader.SetTexture(octantComputeID, "depthTextureY", _depthTexFromHololensY);
		//_tsdfShader.SetTexture(octantComputeID, "depthTextureZ", _depthTexFromHololensZ);
		
#if ONLY_UNPROJECT
		_tsdfShader.SetBuffer(octantComputeID, "pointBuffer", _ourPoints);
		_tsdfShader.SetBuffer(octantComputeID, "colorBuffer", _ourColorBuffer);
		_tsdfShader.SetTexture(octantComputeID, "colorTexture", _ourColor);
#endif
	}

	void FindSuboctants()
	{
		//this doesn't use a co-routine + AsyncGPUReadback, so the GetData call is slow
		_waitingForGrids = true;
		
		//octreeShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//octreeShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//octreeShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);
		_tsdfShader.Dispatch(octantComputeID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);

		octantBuffer.GetData(octantData);
		
#if ONLY_UNPROJECT
		_ourPoints.GetData(_pointData);
		_ourColorBuffer.GetData(_colorData);
		
		string debugOut = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("M_dd_yyyy_hh_mm_ss")+".txt");
		//Debug.Log("Writing data to: " + debugOut);
		
		StreamWriter s = new StreamWriter(File.Open(debugOut, FileMode.Create));
		for(int i = 0; i < DEPTH_RESOLUTION; ++i)
		{
			float xPos = _pointData[i*4];
			float yPos = _pointData[i*4+1];
			float zPos = _pointData[i*4+2];
			uint c = _colorData[i];
			uint red = c & 0x000000FF;
			uint green = ((c >> 8) & 0x000000FF);
			uint blue = ((c >> 16) & 0x000000FF);
			
			if(red == 0 && green == 0 && blue == 0)
			{
				
			}
			else
			{
				s.Write((xPos).ToString("F4") + " " + (yPos).ToString("F4") + " " + (zPos).ToString("F4") + " ");
				
				//float red = (float)_colorData[i * (int)COLOR_BYTE_COUNT+bufIdx*4];
				//float green = (float)_colorData[i * (int)COLOR_BYTE_COUNT+bufIdx*4+1];
				//float blue = (float)_colorData[i * (int)COLOR_BYTE_COUNT+bufIdx*4+2];
				
				s.Write(red.ToString() + " " + green.ToString() + " " + blue.ToString() + "\n");
			}
		}
		
		s.Close();
#endif
		_gridsFound = true;
	}
	
#endif

    void LateUpdate()
    {
		
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(_firstLoop)
		{
			researchMode.SetShouldGetDepth();
			_firstLoop = false;
		}
		
		 // update long depth map texture

        //researchMode.SetLongDepthMapTextureUpdatedOff();
        //Debug.Log("Starting Photo Capture");
        float currTime = Time.time;
        
        if(_lastCaptureTime == 0.0)
        {
            _lastCaptureTime = currTime;
        }
        
        if(currTime - _lastCaptureTime > _captureTime)
        {
            _lastCaptureTime = currTime;

#if COLOR_FROM_PLUGIN

            byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            
            if (frameTexture.Length > 0)
            {
                /*byte[] colorTextureBuffer = researchMode.GetPVColorBuffer();
                
                ushort[] frameTextureFiltered = researchMode.GetDepthMapBufferFiltered();
                float[] pointCloudBuffer = researchMode.GetPointCloudBuffer();
                float[] depthPos = researchMode.GetDepthToWorld();
                float[] camToWorld = researchMode.GetPVMatrix();
                Matrix4x4 cameraToWorldMatrix = Matrix4x4.identity;
                
                int pointCloudLength = pointCloudBuffer.Length / 3;
                Vector3[] pointCloudVector3 = new Vector3[pointCloudLength];
                
                if (pointCloudBuffer.Length > 0)
                {	
                    //string debugOut = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("M_dd_yyyy_hh_mm_ss_")+_fileOutNumber.ToString()+".xyz");
                    //_fileOutNumber++;
                    //StreamWriter s = new StreamWriter(File.Open(debugOut, FileMode.Create));
                    
                    for (int i = 0; i < pointCloudLength; i++)
                    {
                        pointCloudVector3[i] = new Vector3(pointCloudBuffer[3 * i], pointCloudBuffer[3 * i + 1], pointCloudBuffer[3 * i + 2]);
                        //s.Write(i.ToString() + ": " + pointCloudVector3[i].x.ToString("F4") + " " + pointCloudVector3[i].y.ToString("F4")+ " " + pointCloudVector3[i].z.ToString("F4") + "\n");
                    }
                    //s.Close();
                }
                
                for(int i = 0; i < DEPTH_HEIGHT; ++i)
                {
                    int b2Row = i * 2 * DEPTH_WIDTH;
                    int b4Row = i * 4 * DEPTH_WIDTH;
                    for(int j = 0; j < DEPTH_WIDTH; ++j)
                    {
                        int idx = i * DEPTH_WIDTH + j;
                        int ourIdx = i * DEPTH_WIDTH + j;
                        int otherIndex = i * DEPTH_WIDTH + (DEPTH_WIDTH-1-j);
                        depthTextureBytes[ourIdx] = frameTexture[idx];
                        byte[] bd = BitConverter.GetBytes(frameTextureFiltered[idx]);
                        depthTextureFilteredBytes[b2Row + j * 2] = bd[0];
                        depthTextureFilteredBytes[b2Row + j * 2 + 1] = bd[1];
                        float fD = (float)frameTextureFiltered[idx] / 1000f;//values are in millimeters..., so divide by 1000 to get meters...
                        float fDX = pointCloudVector3[idx].x * fD;	
                        float fDY = pointCloudVector3[idx].y * fD;	
                        float fDZ = pointCloudVector3[idx].z * fD;
                        
                        byte[] bX = BitConverter.GetBytes(fDX);
                        byte[] bY = BitConverter.GetBytes(fDY);
                        byte[] bZ = BitConverter.GetBytes(fDZ);
                        
                        floatDepthTextureBytesX[b4Row + j * 4] = bX[0];
                        floatDepthTextureBytesX[b4Row + j * 4 + 1] = bX[1];
                        floatDepthTextureBytesX[b4Row + j * 4 + 2] = bX[2];
                        floatDepthTextureBytesX[b4Row + j * 4 + 3] = bX[3];
                        
                        floatDepthTextureBytesY[b4Row + j * 4] = bY[0];
                        floatDepthTextureBytesY[b4Row + j * 4 + 1] = bY[1];
                        floatDepthTextureBytesY[b4Row + j * 4 + 2] = bY[2];
                        floatDepthTextureBytesY[b4Row + j * 4 + 3] = bY[3];
                        
                        floatDepthTextureBytesZ[b4Row + j * 4] = bZ[0];
                        floatDepthTextureBytesZ[b4Row + j * 4 + 1] = bZ[1];
                        floatDepthTextureBytesZ[b4Row + j * 4 + 2] = bZ[2];
                        floatDepthTextureBytesZ[b4Row + j * 4 + 3] = bZ[3];
                    }
                }
                
                _depthTexFromHololensX.LoadRawTextureData(floatDepthTextureBytesX);
                _depthTexFromHololensX.Apply();
                _depthTexFromHololensY.LoadRawTextureData(floatDepthTextureBytesY);
                _depthTexFromHololensY.Apply();
                _depthTexFromHololensZ.LoadRawTextureData(floatDepthTextureBytesZ);
                _depthTexFromHololensZ.Apply();
                
                //only if previewing the depth within our view do we need to load the depth data...

                //List<byte> imageBufferList = new List<byte>();
                //photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
                
                int stride = 4;
                float denominator = 1.0f / 255.0f;
                List<Color> colorArray = new List<Color>();
                for (int i = 0; i < colorTextureBuffer.Length; i += stride)
                {
                    //int idx = colorTextureBuffer.Length-stride-i;
                    float a = (int)(colorTextureBuffer[i + 3]) * denominator;
                    float r = (int)(colorTextureBuffer[i + 2]) * denominator;
                    float g = (int)(colorTextureBuffer[i + 1]) * denominator;
                    float b = (int)(colorTextureBuffer[i]) * denominator;

                    colorArray.Add(new Color(r, g, b, a));
                }

                //targetTexture.SetPixelData(ImageConversion.EncodeArrayToPNG(colorArray.ToArray(), targetTexture.graphicsFormat, COLOR_WIDTH, COLOR_HEIGHT, COLOR_WIDTH*4), 0, 0);

                targetTexture.SetPixels(colorArray.ToArray());
                targetTexture.Apply();
                
                //targetTexture.LoadRawTextureData(ImageConversion.EncodeArrayToPNG(colorArray.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, COLOR_WIDTH, COLOR_HEIGHT));
                
                //targetTexture.Apply();

                //ImageConversion.LoadImage(targetTexturePNG, targetTexture.EncodeToPNG(), false);
                
                //targetTexturePNG.Apply();

                Matrix4x4 scanTrans = Matrix4x4.identity;
                //Matrix4x4 currPosMat = Matrix4x4.identity;
                //Matrix4x4 currRotMat = Matrix4x4.identity;
                
                //float[] currRot = researchMode.GetCurrRotation();
                //float[] currPos = researchMode.GetCurrPosition();
                
                for(int i = 0; i < 16; ++i)
                {
                    scanTrans[i] = depthPos[i];
                    cameraToWorldMatrix[i] = camToWorld[i];
                    //currPosMat[i] = currPos[i];
                    //currRotMat[i] = currRot[i];
                }
                
                //in the manual processing, the cameraToWorldMatrix here corresponds to the color camera's extrinsic matrix
                //its 3rd column is negated, and we take the transpose of it
                Matrix4x4 scanTransPV = cameraToWorldMatrix;//.transpose;
                //Matrix4x4 scanTransPV = cameraToWorldMatrix;//.transpose;
                Matrix4x4 zScale2 = Matrix4x4.identity;
                Vector4 col3 = zScale2.GetColumn(2);
                col3 = -col3;
                zScale2.SetColumn(2, col3);
                
                scanTransPV = zScale2 * scanTransPV;
                
                /*Matrix4x4 cameraToWorld = zScale2 * cameraToWorldMatrix;
                
                cameraToWorld[3] = cameraToWorld[12];
                cameraToWorld[7] = -cameraToWorld[13];
                cameraToWorld[11] = cameraToWorld[14];
                
                cameraToWorld[12] = cameraToWorld[12];
                cameraToWorld[13] = -cameraToWorld[13];
                cameraToWorld[14] = cameraToWorld[14];*/
                
                /*Matrix4x4 worldToCamera = scanTransPV.inverse;
                
                worldToCamera[3] = -worldToCamera[12];
                worldToCamera[7] = -worldToCamera[13];
                worldToCamera[11] = -worldToCamera[14];

                //photoCaptureFrame.TryGetProjectionMatrix( out Matrix4x4 projectionMatrix);// out Matrix4x4 projectionMatrix);
                //principal points:  373.018,200.805
                //focal length:  587.359,585.931

                //scanTransPV is now the MVP matrix of the color camera, this is used to project back unprojected depth image data to the color image
                //to look up what corresponding color matches the depth, if any
                Matrix4x4 projectionMatrix = Matrix4x4.identity;
                float[] fovVals = researchMode.GetPVFOV();
                
                //Debug.Log(fovVals[0] + " " + fovVals[1]);
                
                Camera c = Camera.main;
                
                projectionMatrix[0] = 1f / fovVals[0];
                projectionMatrix[5] = 1f / fovVals[1];
                projectionMatrix[8] = 373.018f/fovVals[0];
                projectionMatrix[9] = 200.805f/fovVals[1];
                projectionMatrix[10] = 1f;//-(c.farClipPlane + c.nearClipPlane)/(c.farClipPlane - c.nearClipPlane);
                projectionMatrix[11] = 0f;//1f;
                projectionMatrix[14] = 0f;//-2f * (c.farClipPlane * c.nearClipPlane)/(c.farClipPlane - c.nearClipPlane);
                projectionMatrix[15] = 0f;
                
                
                scanTransPV = projectionMatrix * worldToCamera;
                
                //scanTrans = zScale2 * scanTrans;	//don't want this here as this translates incorrectly...
                scanTrans[3] = scanTrans[12];
                scanTrans[7] = -scanTrans[13];
                scanTrans[11] = scanTrans[14];

                _tsdfShader.SetMatrix("localToWorld", scanTrans);
                _tsdfShader.SetMatrix("viewProjMatrix", scanTransPV);
                

                var commandBuffer = new UnityEngine.Rendering.CommandBuffer();
                commandBuffer.name = "Color Blit Pass";
                
                _colorCopyMaterial.SetTexture("_MainTex", targetTexture);
                _colorCopyMaterial.SetTexture("_DepthTexX", _depthTexFromHololensX);
                _colorCopyMaterial.SetTexture("_DepthTexY", _depthTexFromHololensY);
                _colorCopyMaterial.SetTexture("_DepthTexZ", _depthTexFromHololensZ);
                _colorCopyMaterial.SetFloat("_depthWidth", (float)DEPTH_WIDTH);
                _colorCopyMaterial.SetFloat("_depthHeight", (float)DEPTH_HEIGHT);
                _colorCopyMaterial.SetMatrix("_mvpColor", scanTransPV);
                _colorCopyMaterial.SetMatrix("_depthToWorld", scanTrans);
                
                RenderTexture currentActiveRT = RenderTexture.active;
                
                Graphics.SetRenderTarget(_colorRT.colorBuffer,_colorRT.depthBuffer);
                commandBuffer.ClearRenderTarget(false, true, Color.black);
                commandBuffer.Blit(targetTexture, UnityEngine.Rendering.BuiltinRenderTextureType.CurrentActive, _colorCopyMaterial);
                Graphics.ExecuteCommandBuffer(commandBuffer);
                
                _ourColor.ReadPixels(new Rect(0, 0, _ourColor.width, _ourColor.height), 0, 0, false);
                _ourColor.Apply();
                
                if(currentActiveRT != null)
                {
                    Graphics.SetRenderTarget(currentActiveRT.colorBuffer, currentActiveRT.depthBuffer);
                }
                else
                {
                    RenderTexture.active = null;
                }

                //instead let's just project out the points in compute shader, read back and write out....	
                //FindSuboctants();
                
                if(WriteImagesToDisk)
                {
                    //TODO - use above matrices to project color onto depth, write color image that matches depth image size and that have pixels with valid depth..
                    string filenameC = string.Format(@"CapturedImage{0}_n.png", currTime);
                    string filenameC2 = string.Format(@"TargetImage{0}_n.png", currTime);
                    File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC2), targetTexture.EncodeToPNG());
                    
                    //File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC2), ImageConversion.EncodeArrayToPNG(colorArray.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, COLOR_WIDTH, COLOR_HEIGHT));
                    //File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC), ImageConversion.EncodeArrayToPNG(imageBufferList.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 760, 428));
                    string outPathColorImage = System.IO.Path.Combine(Application.persistentDataPath, filenameC);
                    File.WriteAllBytes(outPathColorImage, _ourColor.EncodeToPNG());//ImageConversion.EncodeArrayToPNG(_ourColor.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)_ourColor.width, (uint)_ourColor.height));
                    //if(_firstHeadsetSend)
                    {
                    //	StartCoroutine(UploadImage(outPathColorImage, _ourColor));
                    //	_firstHeadsetSend = false;
                    }
                }

                //not using 16 bit depth as the raw depth buffer from the research mode plugin doesn't handle the sigma buffer within it ahead of time..
                if(WriteImagesToDisk)
                {
                    string filename = string.Format(@"CapturedImageDepth{0}_n.png", currTime);
                    File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(depthTextureFilteredBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, DEPTH_WIDTH, DEPTH_HEIGHT));*/
                    /*string filenameR = string.Format(@"CapturedImageRDepth{0}_n.exr", currTime);
                    string filenameG = string.Format(@"CapturedImageGDepth{0}_n.exr", currTime);
                    string filenameB = string.Format(@"CapturedImageBDepth{0}_n.exr", currTime);
                    File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameR), ImageConversion.EncodeArrayToEXR(_depthTexFromHololensX.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, DEPTH_WIDTH, DEPTH_HEIGHT));
                    File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameG), ImageConversion.EncodeArrayToEXR(_depthTexFromHololensY.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, DEPTH_WIDTH, DEPTH_HEIGHT));
                    File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameB), ImageConversion.EncodeArrayToEXR(_depthTexFromHololensZ.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat, DEPTH_WIDTH, DEPTH_HEIGHT));*/
                /*}
                
                if(WriteImagesToDisk)
                {
                    string depthString = scanTrans[0].ToString("F4") + " " + scanTrans[1].ToString("F4") + " " + scanTrans[2].ToString("F4") + " " + scanTrans[3].ToString("F4") + "\n";
                    depthString = depthString + (scanTrans[4].ToString("F4") + " " + scanTrans[5].ToString("F4") + " " + scanTrans[6].ToString("F4") + " " + scanTrans[7].ToString("F4") + "\n");
                    depthString = depthString + (scanTrans[8].ToString("F4") + " " + scanTrans[9].ToString("F4") + " " + scanTrans[10].ToString("F4") + " " + scanTrans[11].ToString("F4") + "\n");
                    depthString = depthString + (scanTrans[12].ToString("F4") + " " + scanTrans[13].ToString("F4") + " " + scanTrans[14].ToString("F4") + " " + scanTrans[15].ToString("F4") + "\n");*/
                    
                    /*string depthString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
                    depthString = depthString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + projectionMatrix[0].ToString("F4") + "\n");
                    depthString = depthString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + projectionMatrix[5].ToString("F4") + "\n");
                    depthString = depthString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");*/
                    
                /*    string filenameTxt = string.Format(@"DepthToWorldMatrix{0}_n.txt", currTime);
                    System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);
                }*/
#endif
			}
        }
		
		if(UnityEngine.Time.time - _startTimer > 30f)
		{
			_startTimer = UnityEngine.Time.time;
		}
		
#endif
#endif
    }


    #region Button Event Functions
    
    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode.StopAllSensorDevice();
#endif
#endif
    }

    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }
	
	IEnumerator StartNewSubgridFind()
	{
		_waitingForGrids = true;
		//_updateImages = false;
		
		//Debug.Log("Starting new grid find");

		FindSubgrids();

		UnityEngine.Rendering.AsyncGPUReadbackRequest currRequest = UnityEngine.Rendering.AsyncGPUReadback.Request(octantBuffer); 

		while(!currRequest.done)
		{
			yield return null;
		}

		if(!currRequest.hasError)
		{
			Unity.Collections.NativeArray<int>.Copy(currRequest.GetData<int>(), octantData);
		}
		else
		{
			Debug.Log("Error!");
		}

		_gridsFound = true;
	}
	
	void FindSubgrids()
	{
		//octreeShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//octreeShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//octreeShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);
		_tsdfShader.Dispatch(octantComputeID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);
	}
}