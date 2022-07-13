#define INCLUDE_TSDF

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
	
	Texture2D _ourColor = null;
	Texture2D _ourDepth = null;
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

#if INCLUDE_TSDF
	[SerializeField]
	bool _performTSDFReconstruction = false;
	public bool PerformTSDF => _performTSDFReconstruction;
	
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
	
	Vector4 volumeBounds = new Vector4(16f, 8f, 16f, 0f);
	Vector4 volumeOrigin = new Vector4(0f, 0f, 0f, 0f);		
	Vector4 cellDimensions = new Vector4(32f, 16f, 32f, 0f);

	int processID = -1;
	int clearID = -1;
	int clearTextureID = -1;
	int clearBufferID = -1;
	int octantComputeID = -1;
	int renderID = -1;
	int textureID = -1;
	int renderIDAll = -1;

	uint _totalRes = 0;
	uint _currWidth = 0;
	uint _currHeight = 0;

	byte[] bigVolumeData = new byte[NUM_GRIDS*TOTAL_CELLS * sizeof(ushort)];
	byte[] bigColorData = new byte[NUM_GRIDS*TOTAL_CELLS * sizeof(uint)];
	byte[] bigVolumeCPUData;// = new byte[NUM_GRIDS_CPU * TOTAL_CELLS * sizeof(ushort)];
	byte[] bigColorCPUData;// = new byte[NUM_GRIDS_CPU * TOTAL_CELLS * sizeof(uint)];

	int[] octantLookupData = new int[NUM_GRIDS];
	int[] volumeLookupData = new int[NUM_GRIDS];

	ComputeBuffer octantLookup = null;
	ComputeBuffer volumeLookup = null;

	ComputeBuffer bigVolumeBuffer=null;
	ComputeBuffer bigColorBuffer=null;

	int _lastCopyCount = 0;

	Queue<uint> _gpuIndices = new Queue<uint>((int)NUM_GRIDS);
	Queue<uint> _gpuColorIndices = new Queue<uint>((int)NUM_GRIDS);
	Queue<uint> _cpuIndices = new Queue<uint>((int)NUM_GRIDS_CPU);
	Queue<uint> _cpuColorIndices = new Queue<uint>((int)NUM_GRIDS_CPU);
	
	bool _updateImages = true;
	bool _waitingForGrids = false;
	bool _gridsFound = false;
	
	const float SCREEN_MULTIPLIER = 1.0f;

	public RenderTexture _pointRenderTexture;
	public ComputeBuffer _pointRenderBuffer;

	Matrix4x4 _lastProjMatrix = Matrix4x4.identity;
	Matrix4x4 _camIntrinsicsInv = Matrix4x4.identity;

	Camera _arCamera;
#endif

	[SerializeField]
	bool _writeImagesToDisk = false;
	public bool WriteImagesToDisk => _writeImagesToDisk;
	
	[SerializeField]
	bool _showImagesInView = false;
	public bool ShowImagesInView => _showImagesInView;
	
	bool _firstHeadsetSend = true;
	
	//[SerializeField]
	//Texture2D _testTexture;

	[ContextMenu("TestJSON")]
	public void TestJSON()
	{
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
		_arCamera = Camera.main;

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
		
		_ourColor = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RGBA32, false);
		_ourDepth = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.R8, false);
		
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

#if INCLUDE_TSDF
		InitializeTSDF();
#endif
    }

#if INCLUDE_TSDF
	void InitializeTSDF()
	{
		Debug.Log("Initializing TSDF");
		
		if(_tsdfShader != null)
		{
			if(processID == -1)
			{
				processID = _tsdfShader.FindKernel("CSTsdfGrid");
				if(processID != -1)
				{
					Debug.Log("Found Processing shader");
				}
			}

			if(renderID == -1)
			{
				renderID = _tsdfShader.FindKernel("CSRender");
				if(renderID != -1)
				{
					Debug.Log("Found render shader");
				}
			}

			if(renderIDAll == -1)
			{
				renderIDAll = _tsdfShader.FindKernel("CSRenderAll");
				if(renderIDAll != -1)
				{
					Debug.Log("Found render all shader");
				}
			}

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
			
			if(clearBufferID == -1)
			{
				clearBufferID = _tsdfShader.FindKernel("CSClearBuffer");
				if(clearBufferID != -1)
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

			if(textureID == -1)
			{
				textureID = _tsdfShader.FindKernel("CSTexture");
				if(textureID != -1)
				{
					Debug.Log("Found CSTexture shader");
				}
			}

			/*if(depthRangeID == -1)
			{
				depthRangeID = _tsdfShader.FindKernel("CSDepthRange");
				if(depthRangeID != -1)
				{
					Debug.Log("Found CSDepthRange shader");
				}
			}*/
		}

		if(octantBuffer == null)
		{
			octantBuffer = new ComputeBuffer(_depthRT.width * _depthRT.height, sizeof(int));
			octantData = new int[_depthRT.width * _depthRT.height];
			//Debug.Log("Image depth: " + imageDepth.width + " " + imageDepth.height);
			for(int i = 0; i < _depthRT.width * _depthRT.height; ++i)
			{
				octantData[i] = -1;
			}
		}

		octantBuffer.SetData(octantData);

		if(cellBuffer == null)
		{
			cellBuffer = new ComputeBuffer(_depthRT.width * _depthRT.height, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
			cellData = new int[_depthRT.width * _depthRT.height];
			for(int i = 0; i < _depthRT.width * _depthRT.height; ++i)
			{
				cellData[i] = -1;
			}
		}

		cellBuffer.SetData(cellData);
		ushort posOne = UnityEngine.Mathf.FloatToHalf(1.0f);
		byte[] posOneBytes = System.BitConverter.GetBytes(posOne);

		uint totalGPUMem = NUM_GRIDS * TOTAL_CELLS * sizeof(ushort);
		for(uint i = 0; i < totalGPUMem; i+=2)
		{
			System.Buffer.BlockCopy(posOneBytes, 0, bigVolumeData, (int)i, sizeof(ushort));
		}

		uint totalGPUColorMem = NUM_GRIDS * TOTAL_CELLS * sizeof(uint);
		for(uint i = 0; i < totalGPUColorMem; ++i)
		{
			bigColorData[i] = 0;
		}

		for(uint i = 0; i < NUM_GRIDS_CPU; ++i)
		{
			_cpuIndices.Enqueue(i * TOTAL_CELLS * sizeof(ushort));
			_cpuColorIndices.Enqueue(i * TOTAL_CELLS * sizeof(uint));
		}

		for(uint i = 0; i < NUM_GRIDS; ++i)
		{
			_gpuIndices.Enqueue(i * TOTAL_CELLS * sizeof(ushort));
			_gpuColorIndices.Enqueue(i * TOTAL_CELLS * sizeof(uint));
		}

		int screenWidthMult = (int)((float)Screen.width * SCREEN_MULTIPLIER);
		int screenHeightMult = (int)((float)Screen.height * SCREEN_MULTIPLIER);

		_pointRenderTexture = new RenderTexture(screenWidthMult, screenHeightMult, 0);
		_pointRenderTexture.name = name + "_Color";

		_pointRenderTexture.enableRandomWrite = true;
		_pointRenderTexture.filterMode = FilterMode.Point;
		_pointRenderTexture.format = RenderTextureFormat.ARGB32;
		_pointRenderTexture.useMipMap = false;
		_pointRenderTexture.autoGenerateMips = false;
		_pointRenderTexture.Create();

		_pointRenderBuffer = new ComputeBuffer(screenWidthMult*screenHeightMult*4, sizeof(uint));
		
		octantLookup = new ComputeBuffer((int)NUM_GRIDS, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
		volumeLookup = new ComputeBuffer((int)NUM_GRIDS, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
		for(int i = 0; i < NUM_GRIDS; ++i)
		{
			octantLookupData[i] = -1;
			volumeLookupData[i] = -1;
		}

		octantLookup.SetData(octantLookupData);
		volumeLookup.SetData(volumeLookupData);

		_totalRes = (uint)_depthRT.width * (uint)_depthRT.height;

		_currWidth = (uint)_depthRT.width;
		_currHeight = (uint)_depthRT.height;

		for(int i = 0; i < TOTAL_NUM_OCTANTS; ++i)
		{
			octantToBufferMapGPU[i] = -1;
		}
		//Debug.Log(_currWidth + " " +_currHeight);

		/*if(_allOctantBuffer == null)
		{
			_allOctantBuffer = new ComputeBuffer((int)TOTAL_NUM_OCTANTS, sizeof(int));
			_allOctantBuffer.SetData(octantToBufferMapGPU);
		}*/

		if(bigVolumeBuffer == null)
		{
			bigVolumeBuffer = new ComputeBuffer((int)NUM_GRIDS*(int)TOTAL_CELLS/2, sizeof(uint));
			bigVolumeBuffer.SetData(bigVolumeData);
		}

		if(bigColorBuffer == null)
		{
			bigColorBuffer = new ComputeBuffer((int)NUM_GRIDS*(int)TOTAL_CELLS, sizeof(uint));
			bigColorBuffer.SetData(bigColorData);
		}


		_tsdfShader.SetBuffer(clearID, "volumeBuffer", bigVolumeBuffer);
		_tsdfShader.SetBuffer(clearID, "volumeColorBuffer", bigColorBuffer);
		

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

		bigColorBuffer.GetData(bigColorData);
		bigVolumeBuffer.GetData(bigVolumeData);

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

		_tsdfShader.SetBuffer(clearBufferID, "renderBuffer", _pointRenderBuffer);
		
		_tsdfShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		_tsdfShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//_tsdfShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);

		_tsdfShader.SetTexture(processID, "depthTexture", _depthRT);
		//_tsdfShader.SetTexture(processID, "confTexture", _renderTargetConfV);
		_tsdfShader.SetTexture(processID, "colorTexture", _colorRT);

		_tsdfShader.SetBuffer(processID, "volumeBuffer", bigVolumeBuffer);
		_tsdfShader.SetBuffer(processID, "volumeColorBuffer", bigColorBuffer);
		_tsdfShader.SetBuffer(processID, "cellBuffer", cellBuffer);
		_tsdfShader.SetBuffer(processID, "octantBuffer", octantBuffer);

		_tsdfShader.SetTexture(clearTextureID, "renderTexture", _pointRenderTexture);

		_tsdfShader.SetTexture(textureID, "renderTexture", _pointRenderTexture);
		_tsdfShader.SetBuffer(textureID, "renderBuffer", _pointRenderBuffer);
		
		_tsdfShader.SetBuffer(renderID, "volumeBuffer", bigVolumeBuffer);
		_tsdfShader.SetBuffer(renderID, "volumeColorBuffer", bigColorBuffer);
		_tsdfShader.SetBuffer(renderID, "octantLookup", octantLookup);
		_tsdfShader.SetBuffer(renderID, "volumeLookup", volumeLookup);
		_tsdfShader.SetBuffer(renderID, "renderBuffer", _pointRenderBuffer);
		
		_camIntrinsicsInv[0] = 200.0f;//587.189f/2.375f;//(float)int.Parse(camIntrinsicData[0]); // 7.5f;
		_camIntrinsicsInv[5] = 200.0f;//585.766f/1.4861f;//(float)int.Parse(camIntrinsicData[1]); // 7.5f;
		_camIntrinsicsInv[8] = 160.0f;//373.018f/2.375f;//(float)int.Parse(camIntrinsicData[2]); // 7.5f;
		_camIntrinsicsInv[9] = 144.0f;//200.805f/1.4861f;//(float)int.Parse(camIntrinsicData[3]); // 7.5f;
		_camIntrinsicsInv = _camIntrinsicsInv.inverse;

		_tsdfShader.SetMatrix("camIntrinsicsInverse", _camIntrinsicsInv);

	}

	/*void UpdateCameraParams()
	{
		var cameraParams = new XRCameraParams {
			zNear = _arCamera.nearClipPlane,
			zFar = _arCamera.farClipPlane,
			screenWidth = Screen.width,//_currWidth,//
			screenHeight = Screen.height,//_currHeight,//
			screenOrientation = Screen.orientation
		};

		//Debug.Log(lastDisplayMatrix.ToString("F4"));

		Matrix4x4 viewMatrix = Matrix4x4.identity;//_arCamera.viewMatrix;
		Matrix4x4 projMatrix = _lastProjMatrix;
		Matrix4x4 viewInverse = Matrix4x4.identity;

		if (m_CameraManager.subsystem.TryGetLatestFrame(cameraParams, out var cameraFrame)) {
			viewMatrix = Matrix4x4.TRS(_arCamera.transform.position, _arCamera.transform.rotation, Vector3.one).inverse;
			if (SystemInfo.usesReversedZBuffer)
			{
				viewMatrix.m20 = -viewMatrix.m20;
				viewMatrix.m21 = -viewMatrix.m21;
				viewMatrix.m22 = -viewMatrix.m22;
				viewMatrix.m23 = -viewMatrix.m23;
			}
			projMatrix = cameraFrame.projectionMatrix;
			viewInverse = viewMatrix.inverse;
		}
		
		
		Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;//Matrix4x4.TRS(_arCamera.transform.position, _arCamera.transform.rotation, Vector3.one);//_arCamera.worldToCameraMatrix;//
		//Debug.Log(viewProjMatrix.ToString());
		
		if (!m_CameraManager.subsystem.TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics))
		{
			SetLastValues();
			return;
		}
		
		Matrix4x4 flipYZ = new Matrix4x4();
		flipYZ.SetRow(0, new Vector4(1f,0f,0f,0f));
		flipYZ.SetRow(1, new Vector4(0f,1f,0f,0f));
		flipYZ.SetRow(2, new Vector4(0f,0f,-1f,0f));
		flipYZ.SetRow(3, new Vector4(0f,0f,0f,1f));

		//the way we are making this quaternion is impacting the correctness (i.e. we need to do it eventhough the angle is zero, for things to work)
		
		//Debug.Log(viewMatrix.ToString("F6"));
		
		//Matrix4x4 cTransform = GetCamTransform();

		Matrix4x4 rotateToARCamera = flipYZ;

		Matrix4x4 theMatrix = viewInverse * rotateToARCamera;

		Matrix4x4 camIntrinsics = Matrix4x4.identity;

		//we want to pass in the data to compute buffers and calculate that way..
		//viewProjMatrix = projMatrix * theMatrix.inverse;

		_tsdfShader.SetMatrix("localToWorld", theMatrix);
		//octreeShader.SetMatrix("displayMatrix", _lastDisplayMatrix);
		_tsdfShader.SetMatrix("viewProjMatrix", viewProjMatrix);

		//Debug.Log(cameraIntrinsics.focalLength.x + " " + cameraIntrinsics.focalLength.y + " " + cameraIntrinsics.principalPoint.x + " " + cameraIntrinsics.principalPoint.y);
		//these could be set on re-orientation...
		//focal length values are equal
		camIntrinsics.SetColumn(0, new Vector4(cameraIntrinsics.focalLength.y, 0f, 0f, 0f));
		camIntrinsics.SetColumn(1, new Vector4(0f, cameraIntrinsics.focalLength.x, 0f, 0f));
		camIntrinsics.SetColumn(2, new Vector4(cameraIntrinsics.principalPoint.y, cameraIntrinsics.principalPoint.x, 1f, 0f));

		Matrix4x4 camInv = camIntrinsics.inverse;
		_tsdfShader.SetMatrix("camIntrinsicsInverse", camInv);
		_arCamera.GetComponent<ARCameraBackground>().customMaterial.SetMatrix("_camIntrinsicsInverse", camInv);
	}*/

	int ManageMemory()
	{
		int copyCount = 0;

		//UnityEngine.Profiling.Profiler.BeginSample("ManageMemory");
		
		uint numNew = 0;

		Dictionary<int, int> pixelToOctant = new Dictionary<int, int>();

		for(int k = 0; k < _totalRes; ++k)
		{
			//if(copyCount < NUM_GRIDS)
			{
				//Debug.Log(k + " " + octantData[k]);
				//for each pixel from the depth map, check which octant it hit...
				if(octantData[k] != -1)
				{
					//can we remember what was present last frame?  and re-use without copying?
					//i.e. don't clear octantData?
					uint gpuIndex = 0;
					if(octantToBufferMapGPU[octantData[k]] == -1)
					{
						if(_gpuIndices.Count > 0)
						{
							numNew++;
							gpuIndex = _gpuIndices.Dequeue();
							octantToBufferMapGPU[octantData[k]] = (int)gpuIndex;

							uint octID = (uint)octantData[k];
							octantToBufferMap.Add(octID, gpuIndex);
							colorToBufferMap.Add(octID, _gpuColorIndices.Dequeue());
						
							if(!octantToBufferMapCPU.ContainsKey(octID))
							{
								octantToBufferMapCPU.Add(octID, _cpuIndices.Dequeue());
								colorToBufferMapCPU.Add(octID, _cpuColorIndices.Dequeue());
							}

							//if(pixelToOctant[octantData[k]] == -1)
							//{
							//copy to big buffer...
							/*if(!_firstFrame)
							{
								System.Buffer.BlockCopy(bigVolumeCPUData, (int)octantToBufferMapCPU[octID], bigVolumeData, (int)gpuIndex, (int)GRID_BYTE_COUNT);
								System.Buffer.BlockCopy(bigColorCPUData, (int)colorToBufferMapCPU[octID], bigColorData, (int)colorToBufferMap[octID], (int)COLOR_BYTE_COUNT);
							}*/

							//this stores GPU index...
							//pixelToOctant[octantData[k]] = copyCount;
							//newCount++;
							//copy octantID to cell buffer...
							cellData[k] = (int)(gpuIndex/GRID_BYTE_COUNT);
							octantLookupData[copyCount] = (int)octID;
							volumeLookupData[copyCount] = cellData[k];

							copyCount++;
						}
						else
						{
							Debug.Log("NO MORE INDICES!");
						}
					}
					else
					{
						cellData[k] = (int)(octantToBufferMapGPU[octantData[k]]/GRID_BYTE_COUNT);

						int outTest = 0;
						if(!pixelToOctant.TryGetValue(octantData[k], out outTest))
						{
							pixelToOctant.Add(octantData[k], -1);
							octantLookupData[copyCount] = octantData[k];
							volumeLookupData[copyCount] = cellData[k];
							copyCount++;
						}
					}
				}
				else
				{
					cellData[k] = -1;
				}
			}
		}

		for(int i = copyCount; i < NUM_GRIDS; ++i)
		{
			octantLookupData[i] = -1;
			volumeLookupData[i] = -1;
		}

		Unity.Collections.NativeArray<int> cellBuffData = cellBuffer.BeginWrite<int>(0, (int)_totalRes);

		Unity.Collections.NativeArray<int>.Copy(cellData, cellBuffData);

		cellBuffer.EndWrite<int>((int)_totalRes);

		Unity.Collections.NativeArray<int> octantLookupBuffData = octantLookup.BeginWrite<int>(0, (int)NUM_GRIDS);

		Unity.Collections.NativeArray<int>.Copy(octantLookupData, octantLookupBuffData);
		
		octantLookup.EndWrite<int>((int)NUM_GRIDS);

		Unity.Collections.NativeArray<int> volumeLookupBuffData = volumeLookup.BeginWrite<int>(0, (int)NUM_GRIDS);

		Unity.Collections.NativeArray<int>.Copy(volumeLookupData, volumeLookupBuffData);
		
		volumeLookup.EndWrite<int>((int)NUM_GRIDS);

		/*if(numNew > 0)
		{
			//Debug.Log("New count: " + numNew);
			//this call is over-writing previous scan data on the GPU...
			//unless we read it back first
			bigVolumeBuffer.SetData(bigVolumeData);
			bigColorBuffer.SetData(bigColorData);
		}*/

		//cellBuffer.SetData(cellData);
		//UnityEngine.Profiling.Profiler.EndSample();

		//_firstFrame = false;

		return copyCount;
	}

	void FindSuboctants()
	{
		//this doesn't use a co-routine + AsyncGPUReadback, so the GetData call is slow
		_waitingForGrids = true;
		_updateImages = false;
		
		//UpdateCameraParams();

		//octreeShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//octreeShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//octreeShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);
		_tsdfShader.Dispatch(octantComputeID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);

		octantBuffer.GetData(octantData);

		_gridsFound = true;
	}
#endif

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

					float[] depthPos = researchMode.GetDepthToWorld();
					
					if(photoCaptureFrame.hasLocationData)
					{
						//Matrix4x4 cameraToWorldMatrix = Matrix4x4.identity;
						//Matrix4x4 projectionMatrix = Matrix4x4.identity;
						
						photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

						//in the manual processing, the cameraToWorldMatrix here corresponds to the color camera's extrinsic matrix
						//its 3rd column is negated, and we take the transpose of it
						Matrix4x4 scanTransPV = cameraToWorldMatrix;//.transpose;
						Matrix4x4 zScale2 = Matrix4x4.identity;
						Vector4 col3 = zScale2.GetColumn(2);
						col3 = -col3;
						zScale2.SetColumn(2, col3);
						scanTransPV = zScale2 * scanTransPV;
						scanTransPV = scanTransPV.transpose;
						
						//Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
						//Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

						photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);
						
						scanTransPV = projectionMatrix * scanTransPV;
						
						//scanTransPV is now the MVP matrix of the color camera, this is used to project back unprojected depth image data to the color image
						//to look up what corresponding color matches the depth, if any

						Matrix4x4 scanTrans = Matrix4x4.identity;
						for(int i = 0; i < 16; ++i)
						{
							scanTrans[i] = depthPos[i];
						}
						
						//scanTrans = scanTrans.transpose;

						var commandBuffer = new UnityEngine.Rendering.CommandBuffer();
						commandBuffer.name = "Color Blit Pass";
						
						_colorCopyMaterial.SetTexture("_MainTex", targetTexture);
						_colorCopyMaterial.SetTexture("_DepthTex", longDepthMediaTexture);
						_colorCopyMaterial.SetFloat("_depthWidth", (float)DEPTH_WIDTH);
						_colorCopyMaterial.SetFloat("_depthHeight", (float)DEPTH_HEIGHT);
						_colorCopyMaterial.SetMatrix("_camIntrinsicsInv", _camIntrinsicsInv);
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
						
						//at this point we have the depth mvp matrix, and the color proj / view matrix.
						//need to project world space depth value into color image that matches depth image size...
						
						//this should happen in the BLIT shader above...
						/*for(int n = 0; n < DEPTH_WIDTH*DEPTH_HEIGHT; ++n)
						{
							float depth = (float)((float)depthTextureBytes[n] / 255f) * 4000f;
							//Debug.Log("Depth: " + depth);

							int wIndex = (int)(n % DEPTH_WIDTH);
							int hIndex = (int)DEPTH_HEIGHT - (int)(n / DEPTH_WIDTH);
							
							//uint numRows = numPoints / numCols;

							int colorWidth = (int)760;
							int colorHeight = (int)428;
				
							uint idx = (uint)(hIndex * (int)DEPTH_WIDTH + wIndex);
							//byte cData = confData[idx];
							
							pos.x = 0f;
							pos.y = 0f;
							pos.z = 0f;
							pos.w = 1.0f;*/
							
							//if(cData >= ipadConfidence)
							/*{
								Vector3 cameraPoint = new Vector3(wIndex + 0.5f, hIndex + 0.5f, 1f);
								cameraPoint = _camIntrinsicsInv.MultiplyVector(cameraPoint);
								cameraPoint *= depth;
								//cameraPoint.z = -cameraPoint.z;
								Vector4 newCamPoint = new Vector4(cameraPoint.x, cameraPoint.y, cameraPoint.z, 1f);
								//Debug.Log(newCamPoint.ToString("F3"));
								
								Vector4 projectedPoint = scanTrans * newCamPoint;
								
								pos.x = projectedPoint.x / projectedPoint.w;
								pos.y = projectedPoint.y / projectedPoint.w;
								pos.z = projectedPoint.z / projectedPoint.w;

							}
							
							//Debug.Log(pos.ToString("F4"));
							//we now want to project pos into the color image to look up the color value for the new color image
							//this will replace the below colorIdx calculation...
							//need full view projection...
							//do we have this?
							
							Vector4 projPos = scanTransPV * pos;
							//projPos = camIntrinsics2 * projPos;
							//Debug.Log(projPos.ToString("F4"));
							projPos.x /= projPos.w;
							projPos.y /= projPos.w;
							projPos.z /= projPos.w;
							projPos.x = projPos.x * 0.5f + 0.5f;
							projPos.y = projPos.y * 0.5f + 0.5f;
							//projPos.x = 1f - projPos.x;
							//projPos.y = 1f - projPos.y;

							//Debug.Log(projPos.x);
							//Debug.Log(projPos.y);
							int hIdx = (int)((float)colorHeight * projPos.y);
							int wIdx = (int)((float)colorWidth * projPos.x);*/
						//}

						if(_gpuIndices.Count > 0)
						{
							if(!_waitingForGrids)
							{
								FindSuboctants();
								//StartCoroutine(StartNewSubgridFind());
							}

							if(_gridsFound)
							{	
								_lastCopyCount = ManageMemory();
							
								UnprojectPoints(_lastCopyCount);

								_updateImages = true;
								_gridsFound = false;
								_waitingForGrids = false;
							}
						}

						//RenderPoints(_lastCopyCount);

						//write pv / projection matrices...
						if(WriteImagesToDisk)
						{
							string colorString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
							colorString = colorString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + cameraToWorldMatrix[7].ToString("F4") + "\n");
							colorString = colorString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + cameraToWorldMatrix[11].ToString("F4") + "\n");
							colorString = colorString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");
							
							colorString = colorString + (projectionMatrix[0].ToString("F4") + " " + projectionMatrix[1].ToString("F4") + " " + projectionMatrix[2].ToString("F4") + " " + projectionMatrix[3].ToString("F4") + "\n");
							colorString = colorString + (projectionMatrix[4].ToString("F4") + " " + projectionMatrix[5].ToString("F4") + " " + projectionMatrix[6].ToString("F4") + " " + projectionMatrix[7].ToString("F4") + "\n");
							colorString = colorString + (projectionMatrix[8].ToString("F4") + " " + projectionMatrix[9].ToString("F4") + " " + projectionMatrix[10].ToString("F4") + " " + projectionMatrix[11].ToString("F4") + "\n");
							colorString = colorString + (projectionMatrix[12].ToString("F4") + " " + projectionMatrix[13].ToString("F4") + " " + projectionMatrix[14].ToString("F4") + " " + projectionMatrix[15].ToString("F4") + "\n");
							
							
							string filenameTxtC = string.Format(@"CapturedImage{0}_n.txt", currTime);
							System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxtC), colorString);
						}
						
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
						//	StartCoroutine(UploadImage(outPathColorImage, _ourColor));
						//	_firstHeadsetSend = false;
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
						string depthString = depthPos[0].ToString("F4") + " " + depthPos[1].ToString("F4") + " " + depthPos[2].ToString("F4") + " " + depthPos[3].ToString("F4") + "\n";
						depthString = depthString + (depthPos[4].ToString("F4") + " " + depthPos[5].ToString("F4") + " " + depthPos[6].ToString("F4") + " " + depthPos[7].ToString("F4") + "\n");
						depthString = depthString + (depthPos[8].ToString("F4") + " " + depthPos[9].ToString("F4") + " " + depthPos[10].ToString("F4") + " " + depthPos[11].ToString("F4") + "\n");
						depthString = depthString + (depthPos[12].ToString("F4") + " " + depthPos[13].ToString("F4") + " " + depthPos[14].ToString("F4") + " " + depthPos[15].ToString("F4") + "\n");
						
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
	
	void UnprojectPoints(int copyCount)
	{
		_tsdfShader.SetInt("numVolumes", copyCount);
		_tsdfShader.Dispatch(processID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);
	}

	void WriteXYZ()
	{
		
		string debugOut = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("M_dd_yyyy_hh_mm_ss")+".txt");
		Debug.Log("Writing data to: " + debugOut);
		
		StreamWriter s = new StreamWriter(File.Open(debugOut, FileMode.Create));
		if(s != null)
		{
			float gridSizeDiag = (float)1.1f * (float)(volumeBounds.magnitude / volumeGridSize.magnitude);
			
			Vector3Int octantDim = new Vector3Int((int)(volumeGridSize.x / cellDimensions.x), (int)(volumeGridSize.y / cellDimensions.y), (int)(volumeGridSize.z / cellDimensions.z));
   
			Vector3 octantLength = new Vector3((volumeBounds.x / volumeGridSize.x) * cellDimensions.x, (volumeBounds.y / volumeGridSize.y) * cellDimensions.y, (volumeBounds.z / volumeGridSize.z) * cellDimensions.z);
			
			Vector3 gridCellSize = new Vector3((volumeBounds.x / volumeGridSize.x), (volumeBounds.y / volumeGridSize.y), (volumeBounds.z / volumeGridSize.z));
			
			uint gridXY = (uint)(octantDim.x * octantDim.y);
			uint gridYZ = (uint)(octantDim.y * octantDim.z);

			if(gridXY == 0)
			{
				gridXY = 1;
			}
			
			uint gridX = (uint)octantDim.x;
			uint gridZ = (uint)octantDim.z;
			
			if(gridX == 0)
			{
				gridX = 1;
			}

			bigVolumeBuffer.GetData(bigVolumeData);
			bigColorBuffer.GetData(bigColorData);

			uint totalOs = 0;
			for(uint i = 0; i < TOTAL_NUM_OCTANTS; ++i)
			{
				if(octantToBufferMapGPU[i] != -1)
				{
					totalOs++;
				}
			}
			
			ushort posOne = UnityEngine.Mathf.FloatToHalf(1.0f);
			byte[] posOneBytes = System.BitConverter.GetBytes(posOne);

			byte[] bigVolumeCPUData2 = new byte[totalOs * TOTAL_CELLS * sizeof(ushort)];
			byte[] bigColorCPUData2 = new byte[totalOs * TOTAL_CELLS * sizeof(uint)];

			uint totalCPUMem = totalOs * TOTAL_CELLS * sizeof(ushort);
			for(uint i = 0; i < totalCPUMem; i+=2)
			{
				System.Buffer.BlockCopy(posOneBytes, 0, bigVolumeCPUData2, (int)i, sizeof(ushort));
			}

			uint totalCPUColorMem = totalOs * TOTAL_CELLS * sizeof(uint);
			for(uint i = 0; i < totalCPUColorMem; ++i)
			{
				bigColorCPUData2[i] = 0;
			}

			totalOs = 0;
			//List<uint> keysGPU = new List<uint>(octantToBufferMap.Keys);
			for(uint i = 0; i < TOTAL_NUM_OCTANTS; ++i)
			//for(int k = 0; k < keysGPU.Count; ++k)
			{			
				if(octantToBufferMapGPU[i] != -1)
				{
					//System.Buffer.BlockCopy(bigVolumeData, (int)octantToBufferMapGPU[i], bigVolumeCPUData, (int)octantToBufferMapCPU[i], (int)GRID_BYTE_COUNT);
					//System.Buffer.BlockCopy(bigColorData, (int)colorToBufferMap[i], bigColorCPUData, (int)colorToBufferMapCPU[i], (int)COLOR_BYTE_COUNT);

					System.Buffer.BlockCopy(bigVolumeData, (int)octantToBufferMapGPU[i], bigVolumeCPUData2, (int)totalOs * (int)GRID_BYTE_COUNT, (int)GRID_BYTE_COUNT);
					System.Buffer.BlockCopy(bigColorData, (int)colorToBufferMap[i], bigColorCPUData2, (int)totalOs * (int)COLOR_BYTE_COUNT, (int)COLOR_BYTE_COUNT);

					totalOs++;
				}
			}
			//List<uint> keys = new List<uint>(octantToBufferMapCPU.Keys);
			//for(int i = 0; i < keys.Count; ++i)
			totalOs = 0;
			for(int i = 0; i < TOTAL_NUM_OCTANTS; ++i)
			{
				if(octantToBufferMapGPU[i] != -1)
				{
					//uint z = (uint)keys[i] / gridXY;
					//uint val = (uint)keys[i] - (z * gridXY);
					uint z = (uint)i / gridXY;
					uint val = (uint)i - (z * gridXY);
					uint y = val / gridX;
					uint x = val - (y * gridX);
					
					Vector3 vXYZ = new Vector3((float)x, (float)y, (float)z);
					Vector3 offset = Vector3.zero;
					offset.x = volumeOrigin.x - (volumeBounds.x * 0.5f) + vXYZ.x * octantLength.x;
					offset.y = volumeOrigin.y - (volumeBounds.y * 0.5f) + vXYZ.y * octantLength.y;
					offset.z = volumeOrigin.z - (volumeBounds.z * 0.5f) + vXYZ.z * octantLength.z;

					int cdX = (int)cellDimensions.x;
					int cdY = (int)cellDimensions.y;
					int cdZ = (int)cellDimensions.z;
					int cellSizeXY = cdX * cdY;
					for(int j = 0; j < cdZ; ++j)
					{
						for(int k = 0; k < cdY; ++k)
						{
							for(int m = 0; m < cdX; ++m)
							{
								int bufIdx = m + k * cdX + j * cellSizeXY;

								ushort halfUShort = System.BitConverter.ToUInt16(bigVolumeCPUData2, (int)totalOs * (int)GRID_BYTE_COUNT+bufIdx*2);
								//ushort halfUShort = System.BitConverter.ToUInt16(bigVolumeCPUData, (int)octantToBufferMapCPU[keys[i]]+bufIdx*2);
								float tsdf = UnityEngine.Mathf.HalfToFloat(halfUShort);
								//s.Write(octantToBufferMapCPU[keys[i]]+bufIdx + " : " + tsdf + "\n");
								//if(tsdf != 1.0)
								if(tsdf >= -gridSizeDiag && tsdf <= gridSizeDiag)
								{
									byte wCount = bigColorCPUData2[totalOs * (int)COLOR_BYTE_COUNT+bufIdx*4+3];

									if(wCount > 10)
									{
										float xPos = offset.x + (float)m * gridCellSize.x + 0.5f * gridCellSize.x;
										float yPos = offset.y + (float)k * gridCellSize.y + 0.5f * gridCellSize.y;
										float zPos = offset.z + (float)j * gridCellSize.z + 0.5f * gridCellSize.z;

										s.Write(xPos.ToString("F4") + " " + yPos.ToString("F4") + " " + (-zPos).ToString("F4") + " ");

										//float red = (float)bigColorCPUData[colorToBufferMapCPU[keys[i]]+bufIdx*4] / 255.0f;
										//float green = (float)bigColorCPUData[colorToBufferMapCPU[keys[i]]+bufIdx*4+1] / 255.0f;
										//float blue = (float)bigColorCPUData[colorToBufferMapCPU[keys[i]]+bufIdx*4+2] / 255.0f;
										
										float red = (float)bigColorCPUData2[totalOs * (int)COLOR_BYTE_COUNT+bufIdx*4] / 255.0f;
										float green = (float)bigColorCPUData2[totalOs * (int)COLOR_BYTE_COUNT+bufIdx*4+1] / 255.0f;
										float blue = (float)bigColorCPUData2[totalOs * (int)COLOR_BYTE_COUNT+bufIdx*4+2] / 255.0f;
										
										s.Write(red.ToString() + " " + green.ToString() + " " + blue.ToString() + "\n");
									}
								}
							}
						}
					}

					totalOs++;
				}
			}
			s.Close();
		}
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

        // Update point cloud - not currently used...
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
			if(pointCloudRendererGo != null)
			{
            	pointCloudRendererGo.SetActive(true);
			}
        }
        else
        {
			if(pointCloudRendererGo != null)
			{
            	pointCloudRendererGo.SetActive(false);
			}
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
	
	//proof of concept of uploading an image to the easyvizar server
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
	
	IEnumerator StartNewSubgridFind()
	{
		_waitingForGrids = true;
		_updateImages = false;
		
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
		//UpdateCameraParams();

		//octreeShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//octreeShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//octreeShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);
		_tsdfShader.Dispatch(octantComputeID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);
	}
}