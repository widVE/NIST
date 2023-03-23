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
	bool _captureVideo = false;
	
	string _lastDepthBinaryName = "";
	string _lastRectColorName = "";
	string _lastTransformName = "";
	string _lastDepthImageName = "";
	
	static readonly float MaxRecordingTime = 5.0f;
	VideoCapture m_VideoCapture = null;
    float m_stopRecordingTimer = float.MaxValue;

	bool _isCapturing = false;
	
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
	
	int _fileOutNumber = 0;
	
	//float[] _pcTest = new float[6 * 320 * 288];
	
    void Start()
    {
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
					m = m.transpose;
					//m = m.inverse;
					researchMode.SetQRTransform(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);					
					researchMode.SetQRCodeDetected();
					RunSensors();
				};
			}
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
	
    void LateUpdate()
    {
		
#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
		if(_uploadToServer)
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
								
								if(EasyVizARServer.Instance.PutImage("image/png", sDepth, _manager.LocationID, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID))
								{
									_lastDepthImageName = sDepth;
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
								
								if(EasyVizARServer.Instance.PutImage("image/png", sColor, _manager.LocationID, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, pos, rot, hsObject._headsetID))
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
		
		/*string sTransform = researchMode.GetTransformName();
		if(sTransform.Length > 0)
		{
			bool isNewTransform = false;
			if(_lastTransformName.Length == 0)
			{
				_lastTransformName = sTransform;
				isNewTransform = true;
			}
			else
			{
				if(sTransform != _lastTransformName)
				{
					isNewTransform = true;		
				}
			}
			
			if(isNewTransform)
			{
				if(_manager != null)
				{
					var headset = _manager.LocalHeadset;
					if (headset != null)
					{
						var hsObject = headset.GetComponent<EasyVizARHeadset>();
						if (hsObject != null)
						{
							//this is working, but floods the server at the moment...
							//if(EasyVizARServer.Instance.PutImage("image/png", sColor, _manager.LocationID, DEPTH_WIDTH, DEPTH_HEIGHT, TextureUploaded, hsObject.transform.position, hsObject.transform.rotation, hsObject._headsetID))
							{
								_lastTransformName = sTransform;
								//read this file and patch the photo with this transform?
								
								//
							}
						}
					}
					
					//Debug.Log(_lastTransformName);
					
				}
			}	
		}*/
		
		 // update long depth map texture

         /****float currTime = Time.time;
        
        if(_lastCaptureTime == 0.0)
        {
            _lastCaptureTime = currTime;
        }
        
        if(currTime - _lastCaptureTime > _captureTime)
        {
           _lastCaptureTime = currTime;

			string debugOut = Path.Combine(Application.persistentDataPath, DateTime.Now.ToString("M_dd_yyyy_hh_mm_ss_")+_fileOutNumber.ToString());//+".xyz");
			_fileOutNumber++;
			
			if(_captureDepthImages)
			{
				//byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				ushort[] frameTextureFiltered = researchMode.GetDepthMapBufferFiltered();
				for(int i = 0; i < DEPTH_HEIGHT; ++i)
				{
					int b2Row = i * 2 * DEPTH_WIDTH;
					for(int j = 0; j < DEPTH_WIDTH; ++j)
					{
						int idx = i * DEPTH_WIDTH + j;
						int otherIndex = (DEPTH_HEIGHT - i - 1) * (DEPTH_WIDTH) + j;
						byte[] bd = BitConverter.GetBytes(frameTextureFiltered[otherIndex]);
						depthTextureFilteredBytes[b2Row + j * 2] = bd[0];
						depthTextureFilteredBytes[b2Row + j * 2 + 1] = bd[1];
					}
				}
				
				File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, debugOut+"_depth.png"), ImageConversion.EncodeArrayToPNG(depthTextureFilteredBytes, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, DEPTH_WIDTH, DEPTH_HEIGHT));
			}
			
			if(_captureBinaryDepth)
			{
				float[] localDepth = researchMode.GetLocalDepthBuffer();
				
				int pcLen = localDepth.Length;
				
				if (pcLen > 0)
				{	
					BinaryWriter s = new BinaryWriter(File.Open(debugOut+".bin", FileMode.Create));
					
					int stride = 4;
					int rowStride = stride * DEPTH_WIDTH;
					
					for (int i = 0; i < pcLen; i+=4)
					{
						int colIdx = (i % rowStride) / stride;
						int rowIdx = i / rowStride;
						
						int idx = (rowStride * (DEPTH_HEIGHT - rowIdx - 1)) + (colIdx*stride);
						//pointCloudVector3[i] = new Vector3(pointCloudBuffer[3 * i], pointCloudBuffer[3 * i + 1], pointCloudBuffer[3 * i + 2]);
						//if(localDepth[i] != 0f && localDepth[i+1] != 0f && localDepth[i+2] != 0f && localDepth[i+3] != 0f)
						{
							s.Write(localDepth[idx]);
							s.Write(localDepth[idx+1]);
							s.Write(localDepth[idx+2]);
							s.Write(localDepth[idx+3]);
						}
					}
					
					s.Flush();
					s.Close();
				}
			}
			
			if(_captureHiResColorImages)
			{
				byte[] colorTextureBuffer = researchMode.GetPVColorBuffer();
			
				int stride = 4;
				float denominator = 1.0f / 255.0f;
				List<Color> colorArray = new List<Color>();
				int rowStride = stride * COLOR_WIDTH;
				
				for (int i = 0; i < colorTextureBuffer.Length; i += stride)
				{
					int colIdx = (i % rowStride) / stride;
					int rowIdx = i / rowStride;
					
					int idx = (rowStride * (COLOR_HEIGHT - rowIdx - 1)) + (colIdx*stride);//((rowStride - stride) - (colIdx*stride));// colorTextureBuffer.Length-stride-i;
					float a = (int)(colorTextureBuffer[idx + 3]) * denominator;
					float r = (int)(colorTextureBuffer[idx + 2]) * denominator;
					float g = (int)(colorTextureBuffer[idx + 1]) * denominator;
					float b = (int)(colorTextureBuffer[idx]) * denominator;

					colorArray.Add(new Color(r, g, b, a));
				}
				
				colorTexture.SetPixels(colorArray.ToArray());
				colorTexture.Apply();
				
				File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, debugOut+"_color.png"), colorTexture.EncodeToPNG());
			}
			
			if(_captureRectifiedColorImages)
			{
				float[] pointCloudBuffer = researchMode.GetPointCloudBuffer();
				
				int pcLen = pointCloudBuffer.Length;
				
				if (pcLen > 0)
				{	
					for(int i = 0; i < pcLen; ++i)
					{
						_pcTest[i] = pointCloudBuffer[i];	
					}
					
					int stride = 6;
					int rowStride = stride * DEPTH_WIDTH;
					
					List<Color> colorArray = new List<Color>();
					for (int i = 0; i < pcLen; i += stride)
					{
						int colIdx = (i % rowStride) / stride;
						int rowIdx = i / rowStride;
						
						int idx = (rowStride * (DEPTH_HEIGHT - rowIdx - 1)) + (colIdx*stride);//pcLen-stride-i;
						//float a = (int)(_pcTest[i + 3]);
						float r = (_pcTest[idx + 3]);
						float g = (_pcTest[idx + 4]);
						float b = (_pcTest[idx + 5]);

						colorArray.Add(new Color(r, g, b, 1.0f));
					}
					
					colorTextureRect.SetPixels(colorArray.ToArray());
					colorTextureRect.Apply();
				
					File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, debugOut+"_color_rect.png"), colorTextureRect.EncodeToPNG());
				}
			}
						
			if(_captureColorPointCloud)
			{
				float[] pointCloudBuffer = researchMode.GetPointCloudBuffer();
				
				int pcLen = pointCloudBuffer.Length;
				
				if (pcLen > 0)
				{	
					for(int i = 0; i < pcLen; ++i)
					{
						_pcTest[i] = pointCloudBuffer[i];	
					}
					

					StreamWriter s = new StreamWriter(File.Open(debugOut+".txt", FileMode.Create));
					
					for (int i = 0; i < pcLen; i+=6)
					{
						//pointCloudVector3[i] = new Vector3(pointCloudBuffer[3 * i], pointCloudBuffer[3 * i + 1], pointCloudBuffer[3 * i + 2]);
						if(_pcTest[i] != 0f && _pcTest[i+1] != 0f && _pcTest[i+2] != 0f)
						{
							s.Write(_pcTest[i].ToString("F4") + " " + _pcTest[i+1].ToString("F4")+ " " + _pcTest[i+2].ToString("F4") + " " + _pcTest[i+3].ToString("F4") + " " + _pcTest[i+4].ToString("F4")+ " " + _pcTest[i+5].ToString("F4")+ "\n");
						}
					}
					
					s.Flush();
					s.Close();
				}
			}
			
			if(_captureTransforms)
			{
				float[] depthPos = researchMode.GetDepthToWorld();
				
				string depthString = depthPos[0].ToString("F4") + " " + depthPos[4].ToString("F4") + " " + depthPos[8].ToString("F4") + " " + depthPos[12].ToString("F4") + "\n";
				depthString = depthString + (depthPos[1].ToString("F4") + " " + depthPos[5].ToString("F4") + " " + depthPos[9].ToString("F4") + " " + depthPos[13].ToString("F4") + "\n");
				depthString = depthString + (depthPos[2].ToString("F4") + " " + depthPos[6].ToString("F4") + " " + depthPos[10].ToString("F4") + " " + depthPos[14].ToString("F4") + "\n");
				depthString = depthString + (depthPos[3].ToString("F4") + " " + depthPos[7].ToString("F4") + " " + depthPos[11].ToString("F4") + " " + depthPos[15].ToString("F4") + "\n");
				
				//string depthString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
				//depthString = depthString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + projectionMatrix[0].ToString("F4") + "\n");
				//depthString = depthString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + projectionMatrix[5].ToString("F4") + "\n");
				//depthString = depthString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");
				
			    string filenameTxt = debugOut+"_trans.txt";
				System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);	
			}***/
			
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
					
				}
			}
			
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

			//colorTexture.SetPixelData(ImageConversion.EncodeArrayToPNG(colorArray.ToArray(), colorTexture.graphicsFormat, COLOR_WIDTH, COLOR_HEIGHT, COLOR_WIDTH*4), 0, 0);

			colorTexture.SetPixels(colorArray.ToArray());
			colorTexture.Apply();
			
			//targetTexture.LoadRawTextureData(ImageConversion.EncodeArrayToPNG(colorArray.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, COLOR_WIDTH, COLOR_HEIGHT));
			
			//targetTexture.Apply();

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
			

			if(WriteImagesToDisk)
			{
				//TODO - use above matrices to project color onto depth, write color image that matches depth image size and that have pixels with valid depth..
				string filenameC = string.Format(@"CapturedImage{0}_n.png", currTime);
				string filenameC2 = string.Format(@"TargetImage{0}_n.png", currTime);
				File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC2), colorTexture.EncodeToPNG());
				
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
        //}
		
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
	
	void RunSensors()
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