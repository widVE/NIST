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

	private Texture2D _depthTexFromHololensX = null;
	private Texture2D _depthTexFromHololensY = null;
	private Texture2D _depthTexFromHololensZ = null;
	
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
	
	public RenderTexture _colorRT;
	//public RenderTexture _depthRT;
	
	public Material _colorCopyMaterial;
	public Material _depthCopyMaterial;
	
	Texture2D _ourColor = null;
	//Texture2D _ourDepth = null;
	Texture2D targetTexture = null;
	
	bool startRealtimePreview = true;

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

#endif

	[SerializeField]
	bool _writeImagesToDisk = false;
	public bool WriteImagesToDisk => _writeImagesToDisk;
	
	[SerializeField]
	bool _showImagesInView = false;
	public bool ShowImagesInView => _showImagesInView;
	
	bool _firstHeadsetSend = true;
	
	float _startTimer = 0f;
	
	int _fileOutNumber = 0;
	
	bool _firstLoop = true;
	
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

        //tcpClient = GetComponent<TCPClient>();

		// Depth sensor should be initialized in only one mode
        targetTexture = new Texture2D(COLOR_WIDTH, COLOR_HEIGHT, TextureFormat.RGBA32, false);
		
		_ourColor = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RGBA32, false);
		//_ourDepth = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.R8, false);
		
		_startTimer = UnityEngine.Time.time;
		
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode = new HL2ResearchMode();
	
        researchMode.SetPointCloudDepthOffset(0);
		
		
		//if(longDepthPreviewPlane != null)
		//{
			researchMode.InitializeLongDepthSensor();
			researchMode.StartLongDepthSensorLoop();
			researchMode.SetShouldGetDepth();
			//PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
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

	void OnDestroy()
	{
		_isCapturing = false;
#if INCLUDE_TSDF
		if(bigColorBuffer != null)
		{
			bigColorBuffer.Release();
			bigColorBuffer = null;
		}
		
		if(bigVolumeBuffer != null)
		{
			bigVolumeBuffer.Release();
			bigVolumeBuffer = null;
		}
		
		if(octantBuffer != null)
		{
			octantBuffer.Release();
			octantBuffer = null;
		}
		
		if(cellBuffer != null)
		{
			cellBuffer.Release();
			cellBuffer = null;
		}
		
		if(octantLookup != null)
		{
			octantLookup.Release();
			octantLookup = null;
		}
		
		if(volumeLookup != null)
		{
			volumeLookup.Release();
			volumeLookup = null;
		}
#endif
		photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);	
	}
	
#if INCLUDE_TSDF
	void InitializeTSDF()
	{
		Debug.Log("Initializing TSDF");
		
		_depthTexFromHololensX = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		_depthTexFromHololensY = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		_depthTexFromHololensZ = new Texture2D(DEPTH_WIDTH, DEPTH_HEIGHT, TextureFormat.RFloat, false);
		
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
			octantBuffer = new ComputeBuffer(DEPTH_RESOLUTION, sizeof(int));
			octantData = new int[DEPTH_RESOLUTION];
			//Debug.Log("Image depth: " + imageDepth.width + " " + imageDepth.height);
			for(int i = 0; i < DEPTH_RESOLUTION; ++i)
			{
				octantData[i] = -1;
			}
		}

		octantBuffer.SetData(octantData);

		if(cellBuffer == null)
		{
			cellBuffer = new ComputeBuffer(DEPTH_RESOLUTION, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
			cellData = new int[DEPTH_RESOLUTION];
			for(int i = 0; i < DEPTH_RESOLUTION; ++i)
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

		_totalRes = DEPTH_RESOLUTION;

		_currWidth = (uint)DEPTH_WIDTH;
		_currHeight = (uint)DEPTH_HEIGHT;

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
		_tsdfShader.SetTexture(octantComputeID, "depthTextureX", _depthTexFromHololensX);
		_tsdfShader.SetTexture(octantComputeID, "depthTextureY", _depthTexFromHololensY);
		_tsdfShader.SetTexture(octantComputeID, "depthTextureZ", _depthTexFromHololensZ);
		//_tsdfShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);

		_tsdfShader.SetTexture(processID, "depthTextureX", _depthTexFromHololensX);
		_tsdfShader.SetTexture(processID, "depthTextureY", _depthTexFromHololensY);
		_tsdfShader.SetTexture(processID, "depthTextureZ", _depthTexFromHololensZ);
		//_tsdfShader.SetTexture(processID, "confTexture", _renderTargetConfV);
		_tsdfShader.SetTexture(processID, "colorTexture", _ourColor);

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
		
	}


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
			/*_isCapturing = true;
			while(_isCapturing)
			{
				float currTime = Time.time;
				
				if(_lastCaptureTime == 0.0)
				{
					_lastCaptureTime = currTime;
				}
				
				if((currTime - _lastCaptureTime) > WriteTime)
				{*/
					//photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
					photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
			//		_lastCaptureTime = currTime;
			//	}
			//}
		});
	}
	
	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
	}
	
	/*private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
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
	}*/
	
	//this the function we're using for image capture (color and depth), goto memory first, so we can write additional synchronized info (transforms)
	void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
	{
		float currTime = Time.time;
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		
		if(result.success)
		{
			Debug.Log("Captured new photo");
			if (startRealtimePreview)
			{
				byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				
				if (frameTexture.Length > 0)
				{
					if(photoCaptureFrame.hasLocationData)
					{		
						bool res = photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);
						if(res != false)
						{
							ushort[] frameTextureFiltered = researchMode.GetDepthMapBufferFiltered();
							float[] pointCloudBuffer = researchMode.GetPointCloudBuffer();
							float[] depthPos = researchMode.GetDepthToWorld();
				
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

							Matrix4x4 scanTrans = Matrix4x4.identity;
							//Matrix4x4 currPosMat = Matrix4x4.identity;
							//Matrix4x4 currRotMat = Matrix4x4.identity;
							
							//float[] currRot = researchMode.GetCurrRotation();
							//float[] currPos = researchMode.GetCurrPosition();
							
							for(int i = 0; i < 16; ++i)
							{
								scanTrans[i] = depthPos[i];
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
							
							//scanTransPV = scanTransPV;
							scanTransPV = zScale2 * scanTransPV;
							
							/*Matrix4x4 cameraToWorld = cameraToWorldMatrix;// * zScale2;
							
							cameraToWorld[12] = -cameraToWorld[3];
							cameraToWorld[13] = -cameraToWorld[7];
							cameraToWorld[14] = -cameraToWorld[11];*/
							
							Matrix4x4 worldToCamera = scanTransPV.inverse;
							
							worldToCamera[3] = worldToCamera[12];
							worldToCamera[7] = worldToCamera[13];
							worldToCamera[11] = worldToCamera[14];
							//worldToCamera[12] = 0f;
							//worldToCamera[13] = 0f;
							//worldToCamera[14] = 0f;
							
							photoCaptureFrame.TryGetProjectionMatrix( out Matrix4x4 projectionMatrix);// out Matrix4x4 projectionMatrix);
							

							//scanTransPV is now the MVP matrix of the color camera, this is used to project back unprojected depth image data to the color image
							//to look up what corresponding color matches the depth, if any

							scanTransPV = projectionMatrix * worldToCamera;
							
							//scanTrans = zScale2 * scanTrans;	//don't want this here as this translates incorrectly...
							scanTrans[3] = scanTrans[12];
							scanTrans[7] = -scanTrans[13];
							scanTrans[11] = scanTrans[14];
							//scanTrans[12] = 0f;
							//scanTrans[13] = 0f;
							//scanTrans[14] = 0f;
							
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
									
									//System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "test.txt"), _lastCopyCount.ToString());
									
									UnprojectPoints(_lastCopyCount);

									_updateImages = true;
									_gridsFound = false;
									_waitingForGrids = false;
								}
							}

							//write pv / projection matrices...
							/*if(WriteImagesToDisk)
							{
								string colorString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
								colorString = colorString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + cameraToWorldMatrix[7].ToString("F4") + "\n");
								colorString = colorString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + cameraToWorldMatrix[11].ToString("F4") + "\n");
								colorString = colorString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");
								
								colorString = colorString + "\n";
								
								colorString = colorString + (worldToCamera[0].ToString("F4") + " " + worldToCamera[1].ToString("F4") + " " + worldToCamera[2].ToString("F4") + " " + worldToCamera[3].ToString("F4") + "\n");
								colorString = colorString + (worldToCamera[4].ToString("F4") + " " + worldToCamera[5].ToString("F4") + " " + worldToCamera[6].ToString("F4") + " " + worldToCamera[7].ToString("F4") + "\n");
								colorString = colorString + (worldToCamera[8].ToString("F4") + " " + worldToCamera[9].ToString("F4") + " " + worldToCamera[10].ToString("F4") + " " + worldToCamera[11].ToString("F4") + "\n");
								colorString = colorString + (worldToCamera[12].ToString("F4") + " " + worldToCamera[13].ToString("F4") + " " + worldToCamera[14].ToString("F4") + " " + worldToCamera[15].ToString("F4") + "\n");
								
								colorString = colorString + "\n";
								
								colorString = colorString + (flippedWtC[0].ToString("F4") + " " + flippedWtC[1].ToString("F4") + " " + flippedWtC[2].ToString("F4") + " " + flippedWtC[3].ToString("F4") + "\n");
								colorString = colorString + (flippedWtC[4].ToString("F4") + " " + flippedWtC[5].ToString("F4") + " " + flippedWtC[6].ToString("F4") + " " + flippedWtC[7].ToString("F4") + "\n");
								colorString = colorString + (flippedWtC[8].ToString("F4") + " " + flippedWtC[9].ToString("F4") + " " + flippedWtC[10].ToString("F4") + " " + flippedWtC[11].ToString("F4") + "\n");
								colorString = colorString + (flippedWtC[12].ToString("F4") + " " + flippedWtC[13].ToString("F4") + " " + flippedWtC[14].ToString("F4") + " " + flippedWtC[15].ToString("F4") + "\n");
								
								string filenameTxtC = string.Format(@"CapturedImage{0}_n.txt", currTime);
								System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxtC), colorString);
							}*/
							
							
							
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
							
							_depthCopyMaterial.SetTexture("_MainTex", _depthTexFromHololens);
							
							RenderTexture currentActiveRT2 = RenderTexture.active;
							
							Graphics.SetRenderTarget(_depthRT.colorBuffer,_depthRT.depthBuffer);
							commandBufferDepth.ClearRenderTarget(false, true, Color.black);
							commandBufferDepth.Blit(_depthTexFromHololens, UnityEngine.Rendering.BuiltinRenderTextureType.CurrentActive, _depthCopyMaterial);
							Graphics.ExecuteCommandBuffer(commandBufferDepth);
							
							_ourDepth.ReadPixels(new Rect(0, 0, _ourDepth.width, _ourDepth.height), 0, 0, false);
							_ourDepth.Apply();
							
							if(currentActiveRT2 != null)
							{
								Graphics.SetRenderTarget(currentActiveRT2.colorBuffer, currentActiveRT2.depthBuffer);
							}
							else
							{
								RenderTexture.active = null;
							}*/
							
							//not using 16 bit depth as the raw depth buffer from the research mode plugin doesn't handle the sigma buffer within it ahead of time..
							if(WriteImagesToDisk)
							{
								string filename = string.Format(@"CapturedImageDepth{0}_n.png", currTime);
								//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(depthTextureBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 320, 288));
								File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(depthTextureFilteredBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, DEPTH_WIDTH, DEPTH_HEIGHT));
								//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(frameTexture, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, 320, 288));
								//File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(_ourDepth.GetRawTextureData(), UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, 320, 288));
							}
							
							/*if(WriteImagesToDisk)
							{
								string depthString = depthPos[0].ToString("F4") + " " + depthPos[1].ToString("F4") + " " + depthPos[2].ToString("F4") + " " + depthPos[3].ToString("F4") + "\n";
								depthString = depthString + (depthPos[4].ToString("F4") + " " + depthPos[5].ToString("F4") + " " + depthPos[6].ToString("F4") + " " + depthPos[7].ToString("F4") + "\n");
								depthString = depthString + (depthPos[8].ToString("F4") + " " + depthPos[9].ToString("F4") + " " + depthPos[10].ToString("F4") + " " + depthPos[11].ToString("F4") + "\n");
								depthString = depthString + (depthPos[12].ToString("F4") + " " + depthPos[13].ToString("F4") + " " + depthPos[14].ToString("F4") + " " + depthPos[15].ToString("F4") + "\n");
								
								string filenameTxt = string.Format(@"CapturedImageDepth{0}_n.txt", currTime);
								System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);
							}*/
						}
						else
						{
							System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "test2.txt"), "wha");
						}
					}
				}
			}
			
			Debug.Log("Stopping photo mode async");
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
			researchMode.SetShouldGetDepth();
		}
		else
		{
			Debug.Log("Stopping photo mode async 2");
			researchMode.SetShouldGetDepth();
		}
#endif
#endif	
		_lastCaptureTime = currTime;
		_isCapturing = false;
	}
	
	
	void RenderPoints(int copyCount)
	{
		int screenWidthMult = (int)((float)Screen.width * SCREEN_MULTIPLIER);
		int screenHeightMult = (int)((float)Screen.height * SCREEN_MULTIPLIER);

		if(copyCount > 0)
		{
			
			_tsdfShader.Dispatch(clearTextureID, (screenWidthMult + 31) / 32, (screenHeightMult + 31) / 32, 1);
			
			_tsdfShader.Dispatch(clearBufferID, ((screenWidthMult * screenHeightMult * 4) + 1023) / 1024, 1, 1);
			
			if((copyCount*TOTAL_CELLS+1023)/1024 > 65535)
			{
				int numCalls = (int)((copyCount*TOTAL_CELLS) / (1024*65535)) + 1;
				int totalCount = 0;
				int singleCallMax = (65535 * 1024);
				for(int j = 0; j < numCalls; ++j)
				{
					_tsdfShader.SetInt("volumeOffset", j * singleCallMax);
					if((j+1) * singleCallMax > copyCount*TOTAL_CELLS)
					{
						_tsdfShader.Dispatch(renderID, (int)((copyCount*TOTAL_CELLS-totalCount) + 1023) / 1024, 1, 1);
					}
					else
					{
						_tsdfShader.Dispatch(renderID, singleCallMax / 1024, 1, 1);
					}
					totalCount += singleCallMax;
				}
			}
			else
			{
				_tsdfShader.Dispatch(renderID, (int)(copyCount * TOTAL_CELLS + 1023) / 1024, 1, 1);
			}
		}

		//"blit" the render buffer to the render texture...
		_tsdfShader.Dispatch(textureID, (screenWidthMult + 31) / 32, (screenHeightMult + 31) / 32, 1);
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
			float gridSizeDiag = (float)1.05f * (float)(volumeBounds.magnitude / volumeGridSize.magnitude);
			
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

									if(wCount > 0)
									{
										float xPos = offset.x + (float)m * gridCellSize.x + 0.5f * gridCellSize.x;
										float yPos = offset.y + (float)k * gridCellSize.y + 0.5f * gridCellSize.y;
										float zPos = offset.z + (float)j * gridCellSize.z + 0.5f * gridCellSize.z;

										s.Write((xPos).ToString("F4") + " " + (yPos).ToString("F4") + " " + (zPos).ToString("F4") + " ");

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
			s.Flush();
			s.Close();
		}
	}

	//4/26/2022 - Ross T - this writes the color image to disk directly, but we aren't using this one at the moment
	/*void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
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
					File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), ImageConversion.EncodeArrayToPNG(frameTexture, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, DEPTH_WIDTH, DEPTH_HEIGHT));
					
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
		//_isCapturing = false;
	}*/
	
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
		
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(_firstLoop)
		{
			researchMode.SetShouldGetDepth();
			_firstLoop = false;
		}
		
		 // update long depth map texture
        if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated() && !_isCapturing)
        {
			researchMode.SetLongDepthMapTextureUpdatedOff();
			//Debug.Log("Starting Photo Capture");
			/*float currTime = Time.time;
			
			if(_lastCaptureTime == 0.0)
			{
				_lastCaptureTime = currTime;
			}
			
			if(currTime - _lastCaptureTime > WriteTime)
			{
				if(!_isCapturing)
				{*/
					_isCapturing = true;
					PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
					
				//}
			//}
        }
		
		if(UnityEngine.Time.time - _startTimer > 30f)
		{
			WriteXYZ();
			_startTimer = UnityEngine.Time.time;
		}
		
		if(ShowImagesInView && startRealtimePreview)
		{
			UpdateImagePreviews();
		}
		
		//RenderPoints(_lastCopyCount);
		
#endif
#endif
    }


    #region Button Event Functions
    public void TogglePreviewEvent()
    {
        startRealtimePreview = !startRealtimePreview;
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
		//octreeShader.SetBuffer(octantComputeID, "octantBuffer", octantBuffer);
		//octreeShader.SetTexture(octantComputeID, "depthTexture", _depthRT);
		//octreeShader.SetTexture(octantComputeID, "confTexture", _renderTargetConfV);
		_tsdfShader.Dispatch(octantComputeID, ((int)_currWidth + 31) / 32, ((int)_currHeight + 31) / 32, 1);
	}
}