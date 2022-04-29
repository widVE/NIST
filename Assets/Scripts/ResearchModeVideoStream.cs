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

public class ResearchModeVideoStream : MonoBehaviour
{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
    HL2ResearchMode researchMode;
#endif
#endif

	private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;

    //TCPClient tcpClient;

    public GameObject depthPreviewPlane = null;
    private Material depthMediaMaterial = null;
    private Texture2D depthMediaTexture = null;
    private byte[] depthFrameData = null;

    public GameObject shortAbImagePreviewPlane = null;
    private Material shortAbImageMediaMaterial = null;
    private Texture2D shortAbImageMediaTexture = null;
    private byte[] shortAbImageFrameData = null;

    public GameObject longDepthPreviewPlane = null;
    private Material longDepthMediaMaterial = null;
    private Texture2D longDepthMediaTexture = null;
    private byte[] longDepthFrameData = null;

    public GameObject LFPreviewPlane = null;
    private Material LFMediaMaterial = null;
    private Texture2D LFMediaTexture = null;
    private byte[] LFFrameData = null;

    public GameObject RFPreviewPlane = null;
    private Material RFMediaMaterial = null;
    private Texture2D RFMediaTexture = null;
    private byte[] RFFrameData = null;

    public GameObject LRPreviewPlane = null;
    private Material LRMediaMaterial = null;
    private Texture2D LRMediaTexture = null;
    private byte[] LRFrameData = null;

    public GameObject RRPreviewPlane = null;
    private Material RRMediaMaterial = null;
    private Texture2D RRMediaTexture = null;
    private byte[] RRFrameData = null;
	
    public GameObject pointCloudRendererGo;
    public Color pointColor = Color.white;
    private PointCloudRenderer pointCloudRenderer;
	
	public RenderTexture _colorRT;
	public RenderTexture _depthRT;
	
	public Material _colorCopyMaterial;
	public Material _depthCopyMaterial;
	
	//for writing the rotated versions...
	Texture2D _ourColor = null;
	//Texture2D _ourDepth = null;
	Texture2D targetTexture = null;
	
	bool startRealtimePreview = true;
	bool renderPointCloud = false;
	bool _isCapturing = false;
	
	float _lastCaptureTime = 0.0f;
	
	const int DEPTH_WIDTH = 320;
	const int DEPTH_HEIGHT = 288;
	
	const int COLOR_WIDTH = 760;
	const int COLOR_HEIGHT = 428;
	
	byte[] depthTextureBytes = new byte[DEPTH_WIDTH*DEPTH_HEIGHT];
	
	[SerializeField]
	float _writeTime = 1f;
	public float WriteTime => _writeTime;
	
	[SerializeField]
	bool _performTSDFReconstruction = false;
	public bool PerformTSDF => _performTSDFReconstruction;
	
	const uint NUM_GRIDS = 4096;		
	const uint NUM_GRIDS_CPU = 4096;
	const uint TOTAL_GRID_SIZE_X = 2048;
	const uint TOTAL_GRID_SIZE_Y = 1024;
	const uint TOTAL_GRID_SIZE_Z = 2048;

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
	int[] octantToBufferMapGPU = new int[(int)TOTAL_NUM_OCTANTS];

	public ComputeShader _tsdfShader;
		
	Dictionary<uint, uint> octantToBufferMapCPU = new Dictionary<uint, uint>();
	Dictionary<uint, uint> colorToBufferMapCPU = new Dictionary<uint, uint>();
	Dictionary<uint, uint> octantToBufferMap = new Dictionary<uint, uint>();
	Dictionary<uint, uint> colorToBufferMap = new Dictionary<uint, uint>();
	
	ComputeBuffer octantBuffer=null;
	ComputeBuffer cellBuffer=null;
	
	int[] octantData = null;
	int[] cellData = null;
	
	Vector4 volumeBounds = new Vector4(8f, 4f, 8f, 0f);
	Vector4 volumeOrigin = new Vector4(0f, 0f, 0f, 0f);		
	Vector4 cellDimensions = new Vector4(32f, 16f, 32f, 0f);

	int processID = -1;
	int processIDSingle = -1;
	int clearID = -1;
	int clearTextureID = -1;
	int octantComputeID = -1;
	int renderID = -1;
		
	[SerializeField]
	bool _writeImagesToDisk = false;
	public bool WriteImagesToDisk => _writeImagesToDisk;
	
	[SerializeField]
	bool _writePosToEdge = false;
	public bool WritePosToEdge => _writePosToEdge;
	
	[SerializeField]
	bool _showImagesInView = false;
	public bool ShowImagesInView => _showImagesInView;
	
	bool _firstHeadsetSend = true;
	
	//[SerializeField]
	//Texture2D _testTexture;
	[SerializeField]
	GameObject _headsetPrefab;
	
	[ContextMenu("TestJSON")]
	public void TestJSON()
	{
		//StartCoroutine(UploadHeadset());
		/*Headset s = new Headset();
		s.name = "Tester";
		s.transform = new EasyVizARTransform();
		s.transform.pos = new Vector3(1f, 2f, 3f);
		s.transform.rot = new Vector3(0f, 0f, 1f);
		Debug.Log(JsonUtility.ToJson(s, true));*/
		
		//StartCoroutine(UploadImage("Assets/testSendImage.png", _testTexture));
	}
	
    void Start()
    {
		GameObject g = Instantiate(_headsetPrefab);
		EasyVizARHeadset h = g.GetComponent<EasyVizARHeadset>();
		
		if(depthPreviewPlane != null)
		{
			depthMediaMaterial = depthPreviewPlane.GetComponent<MeshRenderer>().material;
			depthMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
			depthMediaMaterial.mainTexture = depthMediaTexture;
		}
		
		if(shortAbImagePreviewPlane != null)
		{
			shortAbImageMediaMaterial = shortAbImagePreviewPlane.GetComponent<MeshRenderer>().material;
			shortAbImageMediaTexture = new Texture2D(512, 512, TextureFormat.Alpha8, false);
			shortAbImageMediaMaterial.mainTexture = shortAbImageMediaTexture;
		}

		if(longDepthPreviewPlane != null)
		{
			longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
			longDepthMediaTexture = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.R8, false);
			longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
		}
		
		if(LFPreviewPlane != null)
		{
			LFMediaMaterial = LFPreviewPlane.GetComponent<MeshRenderer>().material;
			LFMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
			LFMediaMaterial.mainTexture = LFMediaTexture;
		}

		if(RFPreviewPlane != null)
		{
			RFMediaMaterial = RFPreviewPlane.GetComponent<MeshRenderer>().material;
			RFMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
			RFMediaMaterial.mainTexture = RFMediaTexture;
		}
		
		if(LRPreviewPlane != null)
		{
			LRMediaMaterial = LRPreviewPlane.GetComponent<MeshRenderer>().material;
			LRMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
			LRMediaMaterial.mainTexture = LRMediaTexture;
		}
		
		if(RRPreviewPlane != null)
		{
			RRMediaMaterial = RRPreviewPlane.GetComponent<MeshRenderer>().material;
			RRMediaTexture = new Texture2D(640, 480, TextureFormat.Alpha8, false);
			RRMediaMaterial.mainTexture = RRMediaTexture;
		}

		if(pointCloudRendererGo != null)
		{
			pointCloudRenderer = pointCloudRendererGo.GetComponent<PointCloudRenderer>();
		}

        //tcpClient = GetComponent<TCPClient>();

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode = new HL2ResearchMode();
	
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor should be initialized in only one mode
        targetTexture = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGBA32, false);
		
		_ourColor = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGBA32, false);
		//_ourDepth = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.R8, false);
		
		//if(longDepthPreviewPlane != null)
		//{
			researchMode.InitializeLongDepthSensor();
			researchMode.StartLongDepthSensorLoop(); 
		/*}
		else if(depthPreviewPlane && shortAbImagePreviewPlane)
		{
			researchMode.InitializeDepthSensor();
			researchMode.StartDepthSensorLoop();
		}*/
        
		if(LFPreviewPlane != null || RFPreviewPlane != null || RRPreviewPlane != null || RFPreviewPlane != null)
		{
			researchMode.InitializeSpatialCamerasFront();
			researchMode.StartSpatialCamerasFrontLoop();
		}
#endif
#endif
    }

	/*public void SetVisualizationOfSpatialMapping(Microsoft.MixedReality.Toolkit.SpatialAwarenessSystem.SpatialAwarenessMeshDisplayOptions option)
	{
		if (Microsoft.MixedReality.Toolkit.CoreServices.SpatialAwarenessSystem is IMixedRealityDataProviderAccess provider)
		{
			foreach (var observer in provider.GetDataProviders())
			{
				if (observer is IMixedRealitySpatialAwarenessMeshObserver meshObs)
				{
					meshObs.DisplayOption = option;
				}
			}
		}
	}*/
	
	void OnPhotoCaptureCreated(UnityEngine.Windows.WebCam.PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
		/*foreach (Resolution resolution in PhotoCapture.SupportedResolutions)
        {
            Debug.Log(resolution);
        }*/
		
		CameraParameters c = new UnityEngine.Windows.WebCam.CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = COLOR_WIDTH;//cameraResolution.width;
		c.cameraResolutionHeight = COLOR_HEIGHT;//cameraResolution.height;
		c.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

		captureObject.StartPhotoModeAsync(c, delegate(PhotoCapture.PhotoCaptureResult result) {
			// Take a picture
			//string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
			//string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

			//photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
			photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
		});
	}
	
	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
	}
	
	private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
			string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
			
			photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
			//photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
		}
		else
		{
			Debug.LogError("Unable to start photo mode!");
		}
	}
	
	//this the function we're using for image capture (color and depth), goto memory first, so we can write additional synchronized info (transforms)
	void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
	{
		float currTime = Time.time;
		
		if(result.success)
		{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
			if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated())
			{
				//ushort[] frameTexture = researchMode.GetLongDepthMapBuffer();
				byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				if (frameTexture.Length > 0)
				{
					for(int i = 0; i < DEPTH_HEIGHT; ++i)
					{
						for(int j = 0; j < DEPTH_WIDTH; ++j)
						{
							int idx = (DEPTH_HEIGHT-i-1) * DEPTH_WIDTH + j;
							int ourIdx = i * DEPTH_WIDTH + j;
							depthTextureBytes[ourIdx] = frameTexture[idx];
						}
					}
					
					//only if previewing the depth within our view do we need to load the depth data...

					List<byte> imageBufferList = new List<byte>();
					photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
					
					int stride = 4;
					float denominator = 1.0f / 255.0f;
					List<Color> colorArray = new List<Color>();
					for (int i = 0; i < imageBufferList.Count; i += stride)
					{
						//int idx = imageBufferList.Count-1-i;
						float a = (int)(imageBufferList[i + 3]) * denominator;
						float r = (int)(imageBufferList[i + 2]) * denominator;
						float g = (int)(imageBufferList[i + 1]) * denominator;
						float b = (int)(imageBufferList[i]) * denominator;

						colorArray.Add(new Color(r, g, b, a));
					}

					targetTexture.SetPixels(colorArray.ToArray());
					targetTexture.Apply();
					
					var commandBuffer = new UnityEngine.Rendering.CommandBuffer();
					commandBuffer.name = "Color Blit Pass";
					
					_colorCopyMaterial.SetTexture("_MainTex", targetTexture);
					
					RenderTexture currentActiveRT = RenderTexture.active;
					
					Graphics.SetRenderTarget(_colorRT.colorBuffer,_colorRT.depthBuffer);
					//commandBuffer.ClearRenderTarget(false, true, Color.black);
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
					
					float[] depthPos = researchMode.GetDepthToWorld();
					string depthString = depthPos[0].ToString("F4") + " " + depthPos[1].ToString("F4") + " " + depthPos[2].ToString("F4") + " " + depthPos[3].ToString("F4") + "\n";
					depthString = depthString + (depthPos[4].ToString("F4") + " " + depthPos[5].ToString("F4") + " " + depthPos[6].ToString("F4") + " " + depthPos[7].ToString("F4") + "\n");
					depthString = depthString + (depthPos[8].ToString("F4") + " " + depthPos[9].ToString("F4") + " " + depthPos[10].ToString("F4") + " " + depthPos[11].ToString("F4") + "\n");
					depthString = depthString + (depthPos[12].ToString("F4") + " " + depthPos[13].ToString("F4") + " " + depthPos[14].ToString("F4") + " " + depthPos[15].ToString("F4") + "\n");
					
					if(photoCaptureFrame.hasLocationData)
					{
						//Matrix4x4 cameraToWorldMatrix = Matrix4x4.identity;
						//Matrix4x4 projectionMatrix = Matrix4x4.identity;
						
						photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

						//Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
						//Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

						photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);
						
						//write pv / projection matrices...
						string colorString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
						colorString = colorString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + cameraToWorldMatrix[7].ToString("F4") + "\n");
						colorString = colorString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + cameraToWorldMatrix[11].ToString("F4") + "\n");
						colorString = colorString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");
						
						colorString = colorString + (projectionMatrix[0].ToString("F4") + " " + projectionMatrix[1].ToString("F4") + " " + projectionMatrix[2].ToString("F4") + " " + projectionMatrix[3].ToString("F4") + "\n");
						colorString = colorString + (projectionMatrix[4].ToString("F4") + " " + projectionMatrix[5].ToString("F4") + " " + projectionMatrix[6].ToString("F4") + " " + projectionMatrix[7].ToString("F4") + "\n");
						colorString = colorString + (projectionMatrix[8].ToString("F4") + " " + projectionMatrix[9].ToString("F4") + " " + projectionMatrix[10].ToString("F4") + " " + projectionMatrix[11].ToString("F4") + "\n");
						colorString = colorString + (projectionMatrix[12].ToString("F4") + " " + projectionMatrix[13].ToString("F4") + " " + projectionMatrix[14].ToString("F4") + " " + projectionMatrix[15].ToString("F4") + "\n");
						
						
						if(WriteImagesToDisk)
						{
							string filenameTxtC = string.Format(@"CapturedImage{0}_n.txt", currTime);
							System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxtC), colorString);
						}
						
						/*if(_firstHeadsetSend)
						{
							//StartCoroutine(UploadHeadset(new Vector3(cameraToWorldMatrix[12], cameraToWorldMatrix[13], cameraToWorldMatrix[14]), new Vector3(1f,2f,3f)));
							_firstHeadsetSend = false;
						}*/
					}
					
					if(WriteImagesToDisk)
					{
						//TODO - use above matrices to project color onto depth, write color image that matches depth image size and that have pixels with valid depth..
						string filenameC = string.Format(@"CapturedImage{0}_n.png", currTime);
						//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC), targetTexture.EncodeToPNG());//ImageConversion.EncodeArrayToPNG(imageBufferList.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 760, 428));
						//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC), ImageConversion.EncodeArrayToPNG(imageBufferList.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 760, 428));
						string outPathColorImage = System.IO.Path.Combine(Application.persistentDataPath, filenameC);
						File.WriteAllBytes(outPathColorImage, ImageConversion.EncodeArrayToPNG(_ourColor.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, (uint)_ourColor.width, (uint)_ourColor.height));
						//if(_firstHeadsetSend)
						{
							StartCoroutine(UploadImage(outPathColorImage, _ourColor));
							_firstHeadsetSend = false;
						}
					}
					
					/*var commandBufferDepth = new UnityEngine.Rendering.CommandBuffer();
					commandBufferDepth.name = "Depth Blit Pass";
					
					_depthCopyMaterial.SetTexture("_MainTex", longDepthMediaTexture);
					
					currentActiveRT = RenderTexture.active;
					
					Graphics.SetRenderTarget(_depthRT.colorBuffer,_depthRT.depthBuffer);
					//commandBuffer.ClearRenderTarget(false, true, Color.black);
					commandBufferDepth.Blit(longDepthMediaTexture, UnityEngine.Rendering.BuiltinRenderTextureType.CurrentActive, _depthCopyMaterial);
					Graphics.ExecuteCommandBuffer(commandBufferDepth);
					
					_ourDepth.ReadPixels(new Rect(0, 0, _ourDepth.width, _ourDepth.height), 0, 0, false);
					_ourDepth.Apply();
					
					if(currentActiveRT != null)
					{
						Graphics.SetRenderTarget(currentActiveRT.colorBuffer, currentActiveRT.depthBuffer);
					}
					else
					{
						RenderTexture.active = null;
					}*/
					
					//not using 16 bit depth as the raw depth buffer from the research mode plugin doesn't handle the sigma buffer within it ahead of time..
					if(WriteImagesToDisk)
					{
						string filename = string.Format(@"CapturedImageDepth{0}_n.png", currTime);
						File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(depthTextureBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 320, 288));
						//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(frameTexture, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 320, 288));
						//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(_ourDepth.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, 320, 288));
					}
					
					if(WriteImagesToDisk)
					{
						string filenameTxt = string.Format(@"CapturedImageDepth{0}_n.txt", currTime);
						System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);
					}
				}
			}
#endif
#endif
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
		}
		
		_lastCaptureTime = currTime;
		_isCapturing = false;
	}
	
	//4/26/2022 - Ross T - this writes the color image to disk directly, but we aren't using this one at the moment
	void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
	{
		float currTime = Time.time;
		
		if (result.success)
		{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
			if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated())
			{
				byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				if (frameTexture.Length > 0)
				{
					for(int i = 0; i < DEPTH_HEIGHT; ++i)
					{
						for(int j = 0; j < DEPTH_WIDTH; ++j)
						{
							int idx = (DEPTH_HEIGHT-i-1) * DEPTH_WIDTH + j;
							int ourIdx = i * DEPTH_WIDTH + j;
							depthTextureBytes[ourIdx] = frameTexture[idx];
						}
					}
					
					
					string filename = string.Format(@"CapturedImageDepth{0}_n.png", currTime);
					File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(frameTexture, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 320, 288));
					
					float[] depthPos = researchMode.GetDepthToWorld();
					string depthString = depthPos[0].ToString("F4") + " " + depthPos[1].ToString("F4") + " " + depthPos[2].ToString("F4") + " " + depthPos[3].ToString("F4") + "\n";
					depthString = depthString + (depthPos[4].ToString("F4") + " " + depthPos[5].ToString("F4") + " " + depthPos[6].ToString("F4") + " " + depthPos[7].ToString("F4") + "\n");
					depthString = depthString + (depthPos[8].ToString("F4") + " " + depthPos[9].ToString("F4") + " " + depthPos[10].ToString("F4") + " " + depthPos[11].ToString("F4") + "\n");
					depthString = depthString + (depthPos[12].ToString("F4") + " " + depthPos[13].ToString("F4") + " " + depthPos[14].ToString("F4") + " " + depthPos[15].ToString("F4") + "\n");
					string filenameTxt = string.Format(@"Position{0}_n.txt", currTime);
					System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);
				}
			}
#endif
#endif
			//Debug.Log("Saved Photo to disk!");
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
		}
		else
		{
			//Debug.Log("Failed to save Photo to disk");
		}
		
		_lastCaptureTime = currTime;
		_isCapturing = false;
	}
	
	void UpdateImagePreviews()
	{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(depthPreviewPlane != null)
		{
			// update short depth map texture
			if (startRealtimePreview && researchMode.DepthMapTextureUpdated())
			{
				byte[] frameTexture = researchMode.GetDepthMapTextureBuffer();
				if (frameTexture.Length > 0)
				{
					if (depthFrameData == null)
					{
						depthFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, depthFrameData, 0, depthFrameData.Length);
					}

					depthMediaTexture.LoadRawTextureData(depthFrameData);
					depthMediaTexture.Apply();
				}
			}
		}

		if(longDepthPreviewPlane != null)
		{
			if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated())
			{
				byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				
				if (longDepthFrameData == null)
				{
					longDepthFrameData = frameTexture;
				}
				else
				{
					System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
				}
				
				if (frameTexture.Length > 0)
				{
					longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
					longDepthMediaTexture.Apply();
				}
			}
		}
		
		// update short-throw AbImage texture
        if(shortAbImagePreviewPlane != null)
		{
			if (startRealtimePreview && researchMode.ShortAbImageTextureUpdated())
			{
				byte[] frameTexture = researchMode.GetShortAbImageTextureBuffer();
				if (frameTexture.Length > 0)
				{
					if (shortAbImageFrameData == null)
					{
						shortAbImageFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, shortAbImageFrameData, 0, shortAbImageFrameData.Length);
					}

					shortAbImageMediaTexture.LoadRawTextureData(shortAbImageFrameData);
					shortAbImageMediaTexture.Apply();
				}
			}
		}
		
		if(LFPreviewPlane != null)
		{
			// update LF camera texture
			if (startRealtimePreview && researchMode.LFImageUpdated())
			{
				byte[] frameTexture = researchMode.GetLFCameraBuffer();
				if (frameTexture.Length > 0)
				{
					if (LFFrameData == null)
					{
						LFFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, LFFrameData, 0, LFFrameData.Length);
					}

					LFMediaTexture.LoadRawTextureData(LFFrameData);
					LFMediaTexture.Apply();
				}
			}
		}
		
		if(RFPreviewPlane != null)
		{
			// update RF camera texture
			if (startRealtimePreview && researchMode.RFImageUpdated())
			{
				byte[] frameTexture = researchMode.GetRFCameraBuffer();
				if (frameTexture.Length > 0)
				{
					if (RFFrameData == null)
					{
						RFFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, RFFrameData, 0, RFFrameData.Length);
					}

					RFMediaTexture.LoadRawTextureData(RFFrameData);
					RFMediaTexture.Apply();
				}
			}
		}

		if(RRPreviewPlane != null)
		{
			if (startRealtimePreview && researchMode.RRImageUpdated())
			{
				byte[] frameTexture = researchMode.GetRRCameraBuffer();
				if (frameTexture.Length > 0)
				{
					if (RRFrameData == null)
					{
						RRFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, RRFrameData, 0, RRFrameData.Length);
					}

					RRMediaTexture.LoadRawTextureData(RRFrameData);
					RRMediaTexture.Apply();
				}
			}
		}
		
		if(LRPreviewPlane != null)
		{
			if (startRealtimePreview && researchMode.LRImageUpdated())
			{
				byte[] frameTexture = researchMode.GetLRCameraBuffer();
				if (frameTexture.Length > 0)
				{
					if (LRFrameData == null)
					{
						LRFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, LRFrameData, 0, LRFrameData.Length);
					}

					LRMediaTexture.LoadRawTextureData(LRFrameData);
					LRMediaTexture.Apply();
				}
			}
		}
#endif
#endif
	}
	
    void LateUpdate()
    {
		/*Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
		foreach (Resolution resolution in PhotoCapture.SupportedResolutions)
        {
            Debug.Log(resolution);
        }*/
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		
		if(ShowImagesInView && startRealtimePreview)
		{
			UpdateImagePreviews();
		}
		
        // update long depth map texture
        if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated())
        {
			float currTime = Time.time;
			
			if(_lastCaptureTime == 0.0)
			{
				_lastCaptureTime = currTime;
			}
			
			if(currTime - _lastCaptureTime > WriteTime)
			{
				if(!_isCapturing)
				{
					_isCapturing = true;
					PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
				}
			}
        }

        // Update point cloud
        if (renderPointCloud)
        {
            float[] pointCloud = researchMode.GetPointCloudBuffer();
            if (pointCloud.Length > 0)
            {
                int pointCloudLength = pointCloud.Length / 3;
                Vector3[] pointCloudVector3 = new Vector3[pointCloudLength];
                for (int i = 0; i < pointCloudLength; i++)
                {
                    pointCloudVector3[i] = new Vector3(pointCloud[3 * i], pointCloud[3 * i + 1], pointCloud[3 * i + 2]);
                }
                //Debug.LogError("Point Cloud Size: " + pointCloudVector3.Length.ToString());
                pointCloudRenderer.Render(pointCloudVector3, pointColor);

            }
        }
		
		if (_performTSDFReconstruction)
		{
			
		}
#endif
#endif
    }


    #region Button Event Functions
    public void TogglePreviewEvent()
    {
        startRealtimePreview = !startRealtimePreview;
    }
    
    public void TogglePointCloudEvent()
    {
        renderPointCloud = !renderPointCloud;
        if (renderPointCloud)
        {
            pointCloudRendererGo.SetActive(true);
        }
        else
        {
            pointCloudRendererGo.SetActive(false);
        }
    }

    public void StopSensorsEvent()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode.StopAllSensorDevice();
#endif
#endif
        startRealtimePreview = false;
    }

    public void SaveAHATSensorDataEvent()
    {
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        var depthMap = researchMode.GetDepthMapBuffer();
        var AbImage = researchMode.GetShortAbImageBuffer();
#endif
#if WINDOWS_UWP
        //tcpClient.SendUINT16Async(depthMap, AbImage);
#endif
#endif
    }
    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }
	
	//server communication functions...

	IEnumerator UploadHeadset(Vector3 pos, Vector3 orient)
    {
		EasyVizAR.Headset h = new EasyVizAR.Headset();
		h.name = "RossTestFromHololens2";
		h.position = pos;
		h.orientation = orient;
		
        //UnityWebRequest www = UnityWebRequest.Post("http://halo05.wings.cs.wisc.edu:5000/headsets", form);
		
		UnityWebRequest www = new UnityWebRequest("http://halo05.wings.cs.wisc.edu:5000/headsets", "POST");
		www.SetRequestHeader("Content-Type", "application/json");
		
		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(JsonUtility.ToJson(h));
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
        }
		
		www.Dispose();
    }
	
	IEnumerator UploadImage(string sPathToFile, Texture2D imageData)
	{
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = COLOR_WIDTH;
		h.height = COLOR_HEIGHT;
		h.contentType = "image/png";
		//h.imagePath = h2Location;
		//h.imageUrl = null;
		
		UnityWebRequest www = new UnityWebRequest("http://halo05.wings.cs.wisc.edu:5000/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");
		
		string ourJson = JsonUtility.ToJson(h);
		//Debug.Log(ourJson);
		
		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(ourJson);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
        }
		
		string resultText = www.downloadHandler.text;
		
		Debug.Log(resultText);
		
		EasyVizAR.Hololens2PhotoPut h2 = JsonUtility.FromJson<EasyVizAR.Hololens2PhotoPut>(resultText);
		//h2.imagePath = h2Location;
		
		string photoJson = JsonUtility.ToJson(h2);
		//Debug.Log(photoJson);
		//Debug.Log(h2.imageUrl);
		
		UnityWebRequest www2 = new UnityWebRequest("http://halo05.wings.cs.wisc.edu:5000"+h2.imageUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");
		
		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(sPathToFile);//new UploadHandlerRaw(image_as_bytes2);//
        www2.downloadHandler = new DownloadHandlerBuffer();

		yield return www2.SendWebRequest();

        if (www2.result != UnityWebRequest.Result.Success)
        {
            //Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr.txt"), www2.error);
        }
        else
        {
            //Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut.txt"), "successfully sent photo");
        }
		
		www.Dispose();
		www2.Dispose();
	}
	
	/*public async Task<TResultType> PostJsonString<TResultType>(string url, string json_as_string)
    {
        try
        {
            //Setting up a non static constructor version and putting the pieces together
            using (var www = new UnityWebRequest(url, "POST"))
            {
                //Converting into "RAW" JSON? I think this is what that means. I can't use the
                //static put constructor because it only takes WWWForms as an argument
                byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(json_as_string);

                //Setting up the rest of the web request
                //Set content type to Json based on the serialization class used for JsonSerializationOption
                www.SetRequestHeader("Content-Type", _serializationOption.ContentType);

                www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
                www.downloadHandler = new DownloadHandlerBuffer();

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                Debug.Log("www.result: " + www.downloadHandler.text);
                stringID = www.downloadHandler.text;
                

                if (www.result != UnityWebRequest.Result.Success)
                    Debug.LogError($"Failed: {www.error}");

                return default;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"{nameof(Get)} failed: {ex.Message}");
            return default;
        }
    }*/
}