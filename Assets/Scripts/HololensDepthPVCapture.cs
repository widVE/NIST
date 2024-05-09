#define ENABLE_WINMD_SUPPORT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine.Windows.WebCam;
using System.Linq;

// Prevent compiler errors when developing and testing on non-Windows machines.
#if (UNITY_EDITOR_WIN || UNITY_WSA || WINDOWS_UWP)
using Microsoft.Windows.Perception.Spatial.Preview;
using Microsoft.Windows.Perception.Spatial;
#endif

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
	QRScanner _qrScanner;
	
	[SerializeField]
	EasyVizARHeadsetManager _manager;
	
	[SerializeField]
	bool _startOnQRDetection = true;
	
	[SerializeField]
	bool _uploadToServer = true;
	
	//[SerializeField]
	//float _captureTime = 1f;
	
	[SerializeField]
	bool _captureColorPointCloud = false;
	
	[SerializeField]
	bool _captureHiResColorImages = false;
	
	[SerializeField]
	bool _captureRectifiedColorImages = false;
	
	[SerializeField]
	bool _captureDepthImages = false;
	
	[SerializeField]
	bool _captureBinaryDepth = false;
	
	[SerializeField]
	bool _captureTransforms = false;
	
	[SerializeField]
	bool _captureIntensity = false;
	
	[SerializeField]
	bool _captureVideo = false;
	
	[SerializeField]
	bool _rectifyAllImages = true;
	
	[SerializeField]
	DrawCube _cubeTest;
	
	string _lastDepthBinaryName = "";
	string _lastRectColorName = "";
	string _lastTransformName = "";
	string _lastDepthImageName = "";
	string _lastIntensityImageName = "";
	string _lastHiResColorName = "";
	string _lastColorPCName = "";   //world space point cloud...

	Vector3 _lastPosition = Vector3.zero;
	Quaternion _lastOrientation = Quaternion.identity;

	static readonly float MaxRecordingTime = 5.0f;
	VideoCapture m_VideoCapture = null;
    float m_stopRecordingTimer = float.MaxValue;

	bool _isCapturing = true;
	
	float _lastCaptureTime = 0.0f;
	
	const int DEPTH_WIDTH = 320;
	const int DEPTH_HEIGHT = 288;
	
	const int DEPTH_RESOLUTION = DEPTH_WIDTH * DEPTH_HEIGHT;
	//3904 x 2196...
	//1952 x 1100...
	const int COLOR_WIDTH = 760;
	const int COLOR_HEIGHT = 428;
	
	//byte[] depthTextureBytes = new byte[DEPTH_RESOLUTION];
	//byte[] depthTextureFilteredBytes = new byte[DEPTH_RESOLUTION*2];
	
	bool _firstHeadsetSend = true;
	
	Camera _mainCamera;
	
	private string _locationId = null;

	[SerializeField]
	PointCloudRenderer _pc;
	
	Queue<string> _depthImageQueue = new Queue<string>();
	
	//float[] _pcTest = new float[6 * 320 * 288];
	
	private void RemoveOldFiles()
    {
		DirectoryInfo dir = new DirectoryInfo(Application.persistentDataPath);
		var files = Directory.GetFiles(Application.persistentDataPath, "*")
			.Where(path => path.EndsWith(".txt") || path.EndsWith(".png") || path.EndsWith(".bmp"));
		foreach (var f in files)
        {
			File.Delete(f);
        }
	}

    void Start()
    {
		_mainCamera = Camera.main;
		_lastPosition = _mainCamera.transform.position;
		_lastOrientation = _mainCamera.transform.rotation;

		RemoveOldFiles();

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		
		if(_captureVideo)
		{
			StartVideoCaptureTest();
		}
		else
		{
			researchMode = new HL2ResearchMode();

			//if(longDepthPreviewPlane != null)
			//{
			researchMode.InitializeLongDepthSensor();
			
			//researchMode.InitializePVCamera();
			if(!_startOnQRDetection)
			{
				researchMode.SetQRCodeDetected();
				RunSensors();
			}
			else
			{
				_qrScanner.QRTransformChanged += (o, ev) =>
				{
					Matrix4x4 m = ev.NewTransform;
					//m = m.transpose;
					m = m.inverse;

					researchMode.SetReferenceCoordinateSystem(ev.spatialNodeId);
					researchMode.SetQRTransform(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);					
					researchMode.SetQRCodeDetected();

                    // Wait for external signal to start sensing based on location configuration.
					//RunSensors();
				};
				

				_qrScanner.LocationChanged += (o, ev) =>
				{
					_locationId = ev.LocationID;
				};
			}
			
			_isCapturing = true;
			StartCoroutine("LookForData");
		}
		
		//PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		/*}
		else if(depthPreviewPlane && shortAbImagePreviewPlane)
		{
			researchMode.InitializeDepthSensor();
			researchMode.StartDepthSensorLoop();
		}*/

		//researchMode.InitializeSpatialCamerasFront();
		//researchMode.StartSpatialCamerasFrontLoop();
		
#endif
#endif

	}

	void StartVideoCaptureTest()
    {
        Resolution cameraResolution = VideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Debug.Log(cameraResolution);

        float cameraFramerate = VideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
        Debug.Log(cameraFramerate);

        VideoCapture.CreateAsync(false, delegate(VideoCapture videoCapture)
        {
            if (videoCapture != null)
            {
                m_VideoCapture = videoCapture;
                Debug.Log("Created VideoCapture Instance!");

                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;

                m_VideoCapture.StartVideoModeAsync(cameraParameters,
                    VideoCapture.AudioState.ApplicationAndMicAudio,
                    OnStartedVideoCaptureMode);
            }
            else
            {
                Debug.LogError("Failed to create VideoCapture Instance!");
            }
        });
    }

    void OnStartedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Video Capture Mode!");
        string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
        string filename = string.Format("TestVideo_{0}.mp4", timeStamp);
        string filepath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        filepath = filepath.Replace("/", @"\");
        m_VideoCapture.StartRecordingAsync(filepath, OnStartedRecordingVideo);
    }

    void OnStoppedVideoCaptureMode(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Video Capture Mode!");
    }

    void OnStartedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Started Recording Video!");
        m_stopRecordingTimer = Time.time + MaxRecordingTime;
    }

    void OnStoppedRecordingVideo(VideoCapture.VideoCaptureResult result)
    {
        Debug.Log("Stopped Recording Video!");
        m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
    }
	
	void OnDestroy()
	{
		_isCapturing = false;
		
		StopSensorsEvent();
		
		
	}
	
	public void TextureUploaded(string imageURL)
	{
		Debug.Log("Texture uploaded to: " + imageURL);
	}
	
	IEnumerator LookForData()
	{
		while(_isCapturing)
		{
			if(_depthImageQueue.Count > 0)
			{
				string sPC = _depthImageQueue.Dequeue();
				
				int lastIndex = sPC.LastIndexOf("_");
				string prefix = sPC.Substring(0, lastIndex);
					
				string sColor = prefix+"_color.png";
				string transFile = prefix+"_trans.txt";
				string sDepth = prefix+"_depth.bmp";
				//string sI = prefix+"_intensity.png";
				
				//FileInfo sColorInfo = null;
				//FileInfo sTransInfo = null;
				//FileInfo sIInfo = null;
				//FileInfo sDepthInfo = null;
				
				//System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, prefix+"________.txt"), prefix);
				
				
				while(!File.Exists(sColor) && !File.Exists(transFile) && !File.Exists(sDepth))// && !File.Exists(sI)) 
				{
					yield return new WaitForSeconds(0.1f);
				}
				
				//sColorInfo = new FileInfo(sColor);
				//sTransInfo = new FileInfo(transFile);
				//sIInfo = new FileInfo(sI);
				//sDepthInfo = new FileInfo(sDepth);
				
				//while(sColorInfo.Length == 0 || sTransInfo.Length == 0 || sIInfo.Length == 0 || sDepthInfo.Length == 0)
				//{
				//	yield return new WaitForSeconds(0.1f);
				//}

				string[] transLines = File.ReadAllLines(transFile);
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				Matrix4x4 depthTrans = Matrix4x4.identity;
				
				for(int i = 0; i < 4; ++i)
				{
					string[] vals = transLines[i].Split(" ");
					for(int j = 0; j < 4; ++j)
					{
						depthTrans[i*4+j] = float.Parse(vals[j]);
					}
				}
				
				pos = depthTrans.GetPosition();
				rot = depthTrans.rotation;

				var headset = _manager.LocalHeadset;
				string headsetID = "";
				if (headset != null)
				{
					var hsObject = headset.GetComponent<EasyVizARHeadset>();
					if (hsObject != null)
					{
						headsetID = hsObject._headsetID;
					} 
					else 
					{
						headsetID = _manager._local_headset_ID;
					}
				}
				
				//System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, prefix+"________.txt"), prefix);
				
				//if waiting on previous upload call... wait here as well...
				
				while(EasyVizARServer.Instance.IsUploadingImage()) {
					yield return new WaitForSeconds(0.1f);
				}
				
				//EasyVizARServer.Instance.PutImagePair("image/png", sPC, sColor, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, headsetID, "geometry", "photo");
				//EasyVizARServer.Instance.PutImageQuad("image/png", sPC, sColor, sDepth, sI, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, headsetID, "geometry", "photo", "depth", "thermal", prefix);
				EasyVizARServer.Instance.PutImageTriple("image/png", sPC, sColor, sDepth, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, headsetID, "geometry", "photo", "depth", prefix);
			}
			else
			{
				yield return new WaitForSeconds(0.2f);
			}
		}
	}
	
	void LateUpdate()
	{

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else	
		if(_uploadToServer)
		{
			if(_captureBinaryDepth && _captureRectifiedColorImages)
			{
				bool isNewPC = false;
				
				string sPC = researchMode.GetBinaryDepthName();
				if(sPC.Length > 0)
				{
					string prefix = "";
					
					bool isNewBinaryDepth = false;

					if(_lastDepthBinaryName.Length == 0)
					{
						_lastDepthBinaryName = sPC;
						isNewBinaryDepth = true;
					}
					else
					{
						if(sPC != _lastDepthBinaryName)
						{
							isNewBinaryDepth = true;		
						}
					}
					
					if(isNewBinaryDepth) 
					{
						int lastIndex = sPC.LastIndexOf("_");
						prefix = sPC.Substring(0, lastIndex);
						string sOut = "";
						if(isNewBinaryDepth)
						{
							 sOut = " 1 ";
						}
						else
						{
							sOut = " 0 ";
						}
						
						System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, prefix+"___.txt"), prefix+sOut);
						
						_depthImageQueue.Enqueue(sPC);
						//StartCoroutine(LookForData(prefix, sPC));
						_lastDepthBinaryName = sPC;
					}
				}
			}
		}	
#endif
#endif
	}
	
    void LateUpdate2()
    {

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(_uploadToServer)
		{
			if(_captureDepthImages && (_captureRectifiedColorImages || (_captureHiResColorImages && !_rectifyAllImages)) && _captureBinaryDepth && _captureIntensity)
			{
				bool isNewDepth = false;
				string sDepth = researchMode.GetDepthImageName();
				if(sDepth.Length > 0)
				{
					if(_lastDepthImageName.Length == 0)
					{
						_lastDepthImageName = sDepth;
						isNewDepth = true;
					}
					else
					{
						if(sDepth != _lastDepthImageName)
						{
							isNewDepth = true;		
						}
					}
				}
				
				bool isNewColor = false;
				string sColor = "";
				
				if(_captureRectifiedColorImages)
				{
					sColor = researchMode.GetRectColorName();
					if(sColor.Length > 0)
					{
						if(_lastRectColorName.Length == 0)
						{
							_lastRectColorName = sColor;
							isNewColor = true;
						}
						else
						{
							if(sColor != _lastRectColorName)
							{
								isNewColor = true;		
							}
						}
					}
				}
				else if(_captureHiResColorImages)
				{
					sColor = researchMode.GetHiColorName();
					if(sColor.Length > 0)
					{
						if(_lastHiResColorName.Length == 0)
						{
							_lastHiResColorName = sColor;
							isNewColor = true;
						}
						else
						{
							if(sColor != _lastHiResColorName)
							{
								isNewColor = true;		
							}
						}
					}
				}
				
				bool isNewBinaryDepth = false;
				string sPC = researchMode.GetBinaryDepthName();
				if(sPC.Length > 0)
				{
					//int lastIndex = sPC.LastIndexOf("_");
					//string prefix = sPC.SubString(lastIndex);
					
					if(_lastDepthBinaryName.Length == 0)
					{
						_lastDepthBinaryName = sPC;
						isNewBinaryDepth = true;
					}
					else
					{
						if(sPC != _lastDepthBinaryName)
						{
							isNewBinaryDepth = true;		
						}
					}
				}
				
				bool isNewIntensity = false;
				string sI = researchMode.GetIntensityImageName();
				if(sI.Length > 0)
				{
					if(_lastIntensityImageName.Length == 0)
					{
						_lastIntensityImageName = sI;
						isNewIntensity = true;
					}
					else
					{
						if(sI != _lastIntensityImageName)
						{
							isNewIntensity = true;		
						}
					}
				}
				
				if(isNewColor && isNewDepth && isNewBinaryDepth && isNewIntensity)
				{
					if(_manager != null)
					{
						var headset = _manager.LocalHeadset;
						string headsetID = "";
						if (headset != null)
						{
							var hsObject = headset.GetComponent<EasyVizARHeadset>();
							if (hsObject != null)
							{
								headsetID = hsObject._headsetID;
							} 
							else 
							{
								headsetID = _manager._local_headset_ID;
							}
						}
							
						
						Matrix4x4 depthTrans = Matrix4x4.identity;
						string sTransform = researchMode.GetTransformName();
						//load the transform... decompose to the position and rotation...
						string[] transLines = File.ReadAllLines(sTransform);
						Vector3 pos = Vector3.zero;
						Quaternion rot = Quaternion.identity;
						
						for(int i = 0; i < 4; ++i)
						{
							string[] vals = transLines[i].Split(" ");
							for(int j = 0; j < 4; ++j)
							{
								depthTrans[i*4+j] = float.Parse(vals[j]);
							}
						}
						
						pos = depthTrans.GetPosition();
						rot = depthTrans.rotation;

						// Important - send the geometry image before the color image
						// so that the image processing begins after both have been received.
						// The server marks the photo as ready after the color image has been received.
						if(EasyVizARServer.Instance.PutImageQuad("image/png", sPC, sColor, sDepth, sI, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, headsetID, "geometry", "photo", "depth", "thermal"))
						{
							_lastDepthImageName = sDepth;
							if(_captureRectifiedColorImages)
							{
								_lastRectColorName = sColor;
							}
							else if(_captureHiResColorImages)
							{
								_lastHiResColorName = sColor;
							}
							_lastDepthBinaryName = sPC;
							_lastIntensityImageName = sI;
						}
						
						
					}	
				}
			}
			else if(_captureDepthImages && _captureRectifiedColorImages && _captureBinaryDepth)
			{
				bool isNewDepth = false;
				string sDepth = researchMode.GetDepthImageName();
				if(sDepth.Length > 0)
				{
					if(_lastDepthImageName.Length == 0)
					{
						_lastDepthImageName = sDepth;
						isNewDepth = true;
					}
					else
					{
						if(sDepth != _lastDepthImageName)
						{
							isNewDepth = true;		
						}
					}
				}
				
				bool isNewColor = false;
				string sColor = researchMode.GetRectColorName();
				if(sColor.Length > 0)
				{
					if(_lastRectColorName.Length == 0)
					{
						_lastRectColorName = sColor;
						isNewColor = true;
					}
					else
					{
						if(sColor != _lastRectColorName)
						{
							isNewColor = true;		
						}
					}
				}
				
				bool isNewBinaryDepth = false;
				string sPC = researchMode.GetBinaryDepthName();
				if(sPC.Length > 0)
				{
					if(_lastDepthBinaryName.Length == 0)
					{
						_lastDepthBinaryName = sPC;
						isNewBinaryDepth = true;
					}
					else
					{
						if(sPC != _lastDepthBinaryName)
						{
							isNewBinaryDepth = true;		
						}
					}
				}
						
				if(isNewColor && isNewDepth && isNewBinaryDepth)
				{
					if(_manager != null)
					{
						var headset = _manager.LocalHeadset;
						if (headset != null)
						{
							var hsObject = headset.GetComponent<EasyVizARHeadset>();
							if (hsObject != null)
							{
								Matrix4x4 depthTrans = Matrix4x4.identity;
								string sTransform = researchMode.GetTransformName();
								//load the transform... decompose to the position and rotation...
								string[] transLines = File.ReadAllLines(sTransform);
								Vector3 pos = Vector3.zero;
								Quaternion rot = Quaternion.identity;
								
								for(int i = 0; i < 4; ++i)
								{
									string[] vals = transLines[i].Split(" ");
									for(int j = 0; j < 4; ++j)
									{
										depthTrans[i*4+j] = float.Parse(vals[j]);
									}
								}

								pos = depthTrans.GetPosition();
								rot = depthTrans.rotation;
								
								if(EasyVizARServer.Instance.PutImageTriple("image/png", sColor, sDepth, sPC, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "photo", "depth", "geometry"))
								{
									_lastDepthImageName = sDepth;
									_lastRectColorName = sColor;
									_lastDepthBinaryName = sPC;
								}
							}
						}
					}	
				}
			}
			else if(_captureDepthImages && _captureRectifiedColorImages)
			{
				bool isNewDepth = false;
				string sDepth = researchMode.GetDepthImageName();
				if(sDepth.Length > 0)
				{
					if(_lastDepthImageName.Length == 0)
					{
						_lastDepthImageName = sDepth;
						isNewDepth = true;
					}
					else
					{
						if(sDepth != _lastDepthImageName)
						{
							isNewDepth = true;		
						}
					}
				}
				
				bool isNewColor = false;
				string sColor = researchMode.GetRectColorName();
				if(sColor.Length > 0)
				{
					if(_lastRectColorName.Length == 0)
					{
						_lastRectColorName = sColor;
						isNewColor = true;
					}
					else
					{
						if(sColor != _lastRectColorName)
						{
							isNewColor = true;		
						}
					}
				}
						
				if(isNewColor && isNewDepth)
				{
					if(_manager != null)
					{
						var headset = _manager.LocalHeadset;
						if (headset != null)
						{
							var hsObject = headset.GetComponent<EasyVizARHeadset>();
							if (hsObject != null)
							{
								Matrix4x4 depthTrans = Matrix4x4.identity;
								string sTransform = researchMode.GetTransformName();
								//load the transform... decompose to the position and rotation...
								string[] transLines = File.ReadAllLines(sTransform);
								Vector3 pos = Vector3.zero;
								Quaternion rot = Quaternion.identity;
								
								for(int i = 0; i < 4; ++i)
								{
									string[] vals = transLines[i].Split(" ");
									for(int j = 0; j < 4; ++j)
									{
										depthTrans[i*4+j] = float.Parse(vals[j]);
									}
								}
								
								pos = depthTrans.GetPosition();
								rot = depthTrans.rotation;
								
								if(EasyVizARServer.Instance.PutImagePair("image/png", sColor, sDepth, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "photo", "depth"))
								{
									_lastDepthImageName = sDepth;
									_lastRectColorName = sColor;
								}
							}
						}
					}	
				}
			}
			else if(_captureBinaryDepth && _captureRectifiedColorImages)
			{
				bool isNewBinaryDepth = false;
				string sPC = researchMode.GetBinaryDepthName();
				if(sPC.Length > 0)
				{
					if(_lastDepthBinaryName.Length == 0)
					{
						_lastDepthBinaryName = sPC;
						isNewBinaryDepth = true;
					}
					else
					{
						if(sPC != _lastDepthBinaryName)
						{
							isNewBinaryDepth = true;		
						}
					}
				}
					
				bool isNewColor = false;
				string sColor = researchMode.GetRectColorName();
				if(sColor.Length > 0)
				{
					if(_lastRectColorName.Length == 0)
					{
						_lastRectColorName = sColor;
						isNewColor = true;
					}
					else
					{
						if(sColor != _lastRectColorName)
						{
							isNewColor = true;		
						}
					}
				}
						
				if(isNewColor && isNewBinaryDepth)
				{
					if(_manager != null)
					{
						var headset = _manager.LocalHeadset;
						if (headset != null)
						{
							var hsObject = headset.GetComponent<EasyVizARHeadset>();
							if (hsObject != null)
							{
								Matrix4x4 depthTrans = Matrix4x4.identity;
								string sTransform = researchMode.GetTransformName();
								//load the transform... decompose to the position and rotation...
								string[] transLines = File.ReadAllLines(sTransform);
								Vector3 pos = Vector3.zero;
								Quaternion rot = Quaternion.identity;
								
								for(int i = 0; i < 4; ++i)
								{
									string[] vals = transLines[i].Split(" ");
									for(int j = 0; j < 4; ++j)
									{
										depthTrans[i*4+j] = float.Parse(vals[j]);
									}
								}
								
								pos = depthTrans.GetPosition();
								rot = depthTrans.rotation;
								
								if(EasyVizARServer.Instance.PutImagePair("image/png", sColor, sPC, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "photo", "geometry"))
								{
									_lastDepthBinaryName = sPC;
									_lastRectColorName = sColor;
								}
							}
						}
					}	
				}
			}
			else
			{
				if(_captureDepthImages)
				{
					bool isNewDepth = false;
					string sDepth = researchMode.GetDepthImageName();
					if(sDepth.Length > 0)
					{
						if(_lastDepthImageName.Length == 0)
						{
							_lastDepthImageName = sDepth;
							isNewDepth = true;
						}
						else
						{
							if(sDepth != _lastDepthImageName)
							{
								isNewDepth = true;		
							}
						}
						
						if(isNewDepth)
						{
							if(_manager != null)
							{
								var headset = _manager.LocalHeadset;
								if (headset != null)
								{
									var hsObject = headset.GetComponent<EasyVizARHeadset>();
									if (hsObject != null)
									{
										Matrix4x4 depthTrans = Matrix4x4.identity;
										string sTransform = researchMode.GetTransformName();
										//load the transform... decompose to the position and rotation...
										string[] transLines = File.ReadAllLines(sTransform);
										Vector3 pos = Vector3.zero;
										Quaternion rot = Quaternion.identity;
										
										for(int i = 0; i < 4; ++i)
										{
											string[] vals = transLines[i].Split(" ");
											for(int j = 0; j < 4; ++j)
											{
												depthTrans[i*4+j] = float.Parse(vals[j]);
											}
										}
										
										pos = depthTrans.GetPosition();
										rot = depthTrans.rotation;
										
										if(EasyVizARServer.Instance.PutImage("image/png", sDepth, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "depth"))
										{
											_lastDepthImageName = sDepth;
										}
									}
								}
							}
						}
					}
				}
				
				/*
				if(_lastDepthBinaryName.Length == 0)
				{
					_lastDepthBinaryName = researchMode.GetBinaryDepthName();
					isNewDepth = true;
				}
				else
				{
					string s = researchMode.GetBinaryDepthName();
					if(s != _lastDepthBinaryName)
					{
						isNewDepth = true;
						_lastDepthBinaryName = s;
					}
				}
				
				if(isNewDepth)
				{
					if(_manager != null)
					{
						//Debug.Log(_lastDepthBinaryName);
						//EasyVizARServer.Instance.PutImage("image/png", _lastRectColorName, _manager.LocationID, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, hsObject.transform.position, hsObject.transform.rotation, hsObject._headsetID);
							//}
					}
				}*/
				
				if(_captureRectifiedColorImages)
				{
					string sColor = researchMode.GetRectColorName();
					if(sColor.Length > 0)
					{
						bool isNewColor = false;
						if(_lastRectColorName.Length == 0)
						{
							_lastRectColorName = sColor;
							isNewColor = true;
						}
						else
						{
							if(sColor != _lastRectColorName)
							{
								isNewColor = true;		
							}
						}
						
						if(isNewColor)
						{
							if(_manager != null)
							{
								var headset = _manager.LocalHeadset;
								if (headset != null)
								{
									var hsObject = headset.GetComponent<EasyVizARHeadset>();
									if (hsObject != null)
									{
										Matrix4x4 depthTrans = Matrix4x4.identity;
										string sTransform = researchMode.GetTransformName();
										//load the transform... decompose to the position and rotation...
										string[] transLines = File.ReadAllLines(sTransform);
										Vector3 pos = Vector3.zero;
										Quaternion rot = Quaternion.identity;
										
										for(int i = 0; i < 4; ++i)
										{
											string[] vals = transLines[i].Split(" ");
											for(int j = 0; j < 4; ++j)
											{
												depthTrans[i*4+j] = float.Parse(vals[j]);
											}
										}
										
										pos = depthTrans.GetPosition();
										rot = depthTrans.rotation;
										
										if(EasyVizARServer.Instance.PutImage("image/png", sColor, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID))
										{
											_lastRectColorName = sColor;
										}
									}
								}
								
								//Debug.Log(_lastRectColorName);
								
							}
						}
					}
				}
				
				if(_captureIntensity)
				{
					string sIntensity = researchMode.GetIntensityImageName();
					if(sIntensity.Length > 0)
					{
						bool isNewIntensity = false;
						if(_lastIntensityImageName.Length == 0)
						{
							_lastIntensityImageName = sIntensity;
							isNewIntensity = true;
						}
						else
						{
							if(sIntensity != _lastIntensityImageName)
							{
								isNewIntensity = true;		
							}
						}
						
						if(isNewIntensity)
						{
							if(_manager != null)
							{
								var headset = _manager.LocalHeadset;
								if (headset != null)
								{
									var hsObject = headset.GetComponent<EasyVizARHeadset>();
									if (hsObject != null)
									{
										Matrix4x4 depthTrans = Matrix4x4.identity;
										string sTransform = researchMode.GetTransformName();
										//load the transform... decompose to the position and rotation...
										string[] transLines = File.ReadAllLines(sTransform);
										Vector3 pos = Vector3.zero;
										Quaternion rot = Quaternion.identity;
										
										for(int i = 0; i < 4; ++i)
										{
											string[] vals = transLines[i].Split(" ");
											for(int j = 0; j < 4; ++j)
											{
												depthTrans[i*4+j] = float.Parse(vals[j]);
											}
										}
										
										pos = depthTrans.GetPosition();
										rot = depthTrans.rotation;
										
										if(EasyVizARServer.Instance.PutImage("image/png", sIntensity, _locationId, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "thermal"))
										{
											_lastIntensityImageName = sIntensity;
										}
									}
								}
								
								//Debug.Log(_lastRectColorName);
								
							}
						}
					}
				}
			}
		}
		
		if(_captureColorPointCloud)
		{
			string sPC = researchMode.GetPointCloudName();
			if(sPC.Length > 0)
			{
				bool isNewPC = false;
				if(_lastColorPCName.Length == 0)
				{
					_lastColorPCName = sPC;
					isNewPC = true;

					// Store pose at time the point cloud was aquired. Maybe this will fix staleness?
					_lastPosition = _mainCamera.transform.position;
					_lastOrientation = _mainCamera.transform.rotation;
				}
				else
				{
					if(sPC != _lastColorPCName)
					{
						isNewPC = true;

						// Store pose at time the point cloud was aquired. Maybe this will fix staleness?
						_lastPosition = _mainCamera.transform.position;
						_lastOrientation = _mainCamera.transform.rotation;
					}
				}
				
				if(isNewPC)
				{
					//as a proof of concept try using the point cloud renderer stuff that came with the research mode plugin?
					if(_manager != null)
					{
						var headset = _manager.LocalHeadset;
						if (headset != null)
						{
							var hsObject = headset.GetComponent<EasyVizARHeadset>();
							if (hsObject != null)
							{
								Matrix4x4 depthTrans = Matrix4x4.identity;
								string sTransform = researchMode.GetTransformName();
								//load the transform... decompose to the position and rotation...
								string[] transLines = File.ReadAllLines(sTransform);
								Vector3 pos = Vector3.zero;
								Quaternion rot = Quaternion.identity;
								
								for(int i = 0; i < 4; ++i)
								{
									string[] vals = transLines[i].Split(" ");
									for(int j = 0; j < 4; ++j)
									{
										depthTrans[i*4+j] = float.Parse(vals[j]);
									}
								}
								
								pos = depthTrans.GetPosition();
								rot = depthTrans.rotation;
								
								_lastColorPCName = sPC;
								
								//uncomment to test cube rendering.
								/*if(_cubeTest != null)
								{
									_cubeTest._pcFileName = _lastColorPCName;
									
								}*/
								
								if(_pc != null)
								{
									_pc._lastPCFileName = _lastColorPCName;
									_pc.UpdateMesh();
								}
								
								/*if(EasyVizARServer.Instance.PutImage("image/png", sIntensity, _manager.LocationID, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID, "thermal"))
								{
									_lastColorPCName = sPC;
								}*/
							}
						}
						
						//Debug.Log(_lastRectColorName);
						
					}
				}
			}
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
	
	public void RunSensors()
	{
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(_captureHiResColorImages)
		{
			researchMode.SetCaptureHiResColorImage();
		}
		
		if(_captureColorPointCloud)
		{
			researchMode.SetCaptureColoredPointCloud();
		}

		if(_captureRectifiedColorImages)
		{
			researchMode.SetCaptureRectColorImage();
		}

		if(_captureDepthImages)
		{
			researchMode.SetCaptureDepthImages();
		}
		
		if(_captureBinaryDepth)
		{
			researchMode.SetCaptureBinaryDepth();
		}
		
		if(_captureTransforms)
		{
			researchMode.SetCaptureTransforms();
		}
		
		if(_captureIntensity)
		{
			researchMode.SetCaptureIntensity();
		}
		
		if(_rectifyAllImages)
		{
			researchMode.SetUsingRectifiedImages();
		}
		
       	researchMode.StartPVCameraLoop();

		researchMode.StartLongDepthSensorLoop();
		
#endif
#endif
	}
	
    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }
}
