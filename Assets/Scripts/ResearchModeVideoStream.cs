using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    TCPClient tcpClient;

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
	
	bool startRealtimePreview = true;
	bool renderPointCloud = false;
	bool _isCapturing = false;
	
	float _lastCaptureTime = 0.0f;
	
	Texture2D targetTexture = null;
	
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

		//if(longDepthPreviewPlane != null)
		{
			//longDepthMediaMaterial = longDepthPreviewPlane.GetComponent<MeshRenderer>().material;
			longDepthMediaTexture = new Texture2D(320, 288, TextureFormat.Alpha8, false);
			//longDepthMediaMaterial.mainTexture = longDepthMediaTexture;
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

        tcpClient = GetComponent<TCPClient>();

#if ENABLE_WINMD_SUPPORT
#if UNITY_EDITOR
#else
        researchMode = new HL2ResearchMode();
	
        researchMode.SetPointCloudDepthOffset(0);

        // Depth sensor should be initialized in only one mode
        targetTexture = new Texture2D(760, 428, TextureFormat.RGBA32, false);
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
		c.cameraResolutionWidth = 760;//cameraResolution.width;
		c.cameraResolutionHeight = 428;//cameraResolution.height;
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
				byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
				if (frameTexture.Length > 0)
				{
					/*if (longDepthFrameData == null)
					{
						longDepthFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
					}

					longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
					longDepthMediaTexture.Apply();
					
					//write out this PNG... do this at the same time when the color photo is saved...
					byte[] pngData = longDepthMediaTexture.EncodeToPNG();*/
					
					/*for(int j = 0; j < 320; ++j)
					{
						for(int i = 0; i < 288; ++i)
						{
							int idx = j * 288 + i;
							int flipIdx = j * (288-1-i) + i;
							byte b = frameTexture[idx];
							byte flipB = frameTexture[flipIdx];
							frameTexture[idx] = flipB;
							frameTexture[flipIdx] = b;
						}
					}*/
					
					List<byte> imageBufferList = new List<byte>();
					photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferList);
					
					int stride = 4;
					float denominator = 1.0f / 255.0f;
					List<Color> colorArray = new List<Color>();
					for (int i = imageBufferList.Count - 1; i >= 0; i -= stride)
					{
						float a = (int)(imageBufferList[i - 0]) * denominator;
						float r = (int)(imageBufferList[i - 1]) * denominator;
						float g = (int)(imageBufferList[i - 2]) * denominator;
						float b = (int)(imageBufferList[i - 3]) * denominator;

						colorArray.Add(new Color(r, g, b, a));
					}

					targetTexture.SetPixels(colorArray.ToArray());
					targetTexture.Apply();
					
					string filenameC = string.Format(@"CapturedImage{0}_n.png", currTime);
					File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filenameC), targetTexture.EncodeToPNG());//ImageConversion.EncodeArrayToPNG(imageBufferList.ToArray(), UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 760, 428));
					
					if(photoCaptureFrame.hasLocationData)
					{
						//Matrix4x4 cameraToWorldMatrix = Matrix4x4.identity;
						//Matrix4x4 projectionMatrix = Matrix4x4.identity;
						
						photoCaptureFrame.TryGetCameraToWorldMatrix(out Matrix4x4 cameraToWorldMatrix);

						//Vector3 position = cameraToWorldMatrix.GetColumn(3) - cameraToWorldMatrix.GetColumn(2);
						//Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));

						//photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out Matrix4x4 projectionMatrix);
						
						//write pv / projection matrices...
						string colorString = cameraToWorldMatrix[0].ToString("F4") + " " + cameraToWorldMatrix[1].ToString("F4") + " " + cameraToWorldMatrix[2].ToString("F4") + " " + cameraToWorldMatrix[3].ToString("F4") + "\n";
						colorString = colorString + (cameraToWorldMatrix[4].ToString("F4") + " " + cameraToWorldMatrix[5].ToString("F4") + " " + cameraToWorldMatrix[6].ToString("F4") + " " + cameraToWorldMatrix[7].ToString("F4") + "\n");
						colorString = colorString + (cameraToWorldMatrix[8].ToString("F4") + " " + cameraToWorldMatrix[9].ToString("F4") + " " + cameraToWorldMatrix[10].ToString("F4") + " " + cameraToWorldMatrix[11].ToString("F4") + "\n");
						colorString = colorString + (cameraToWorldMatrix[12].ToString("F4") + " " + cameraToWorldMatrix[13].ToString("F4") + " " + cameraToWorldMatrix[14].ToString("F4") + " " + cameraToWorldMatrix[15].ToString("F4") + "\n");
						string filenameTxtC = string.Format(@"PV2World{0}_n.txt", currTime);
						System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxtC), colorString);
					}
					
			
					//we probably want 16 bit here instead...
					
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
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
		}
		
		_lastCaptureTime = currTime;
		_isCapturing = false;
	}
	
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
					/*if (longDepthFrameData == null)
					{
						longDepthFrameData = frameTexture;
					}
					else
					{
						System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
					}

					longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
					longDepthMediaTexture.Apply();
					
					//write out this PNG... do this at the same time when the color photo is saved...
					byte[] pngData = longDepthMediaTexture.EncodeToPNG();*/
					
					/*for(int j = 0; j < 320; ++j)
					{
						for(int i = 0; i < 288; ++i)
						{
							int idx = j * 288 + i;
							int flipIdx = j * (288-1-i) + i;
							byte b = frameTexture[idx];
							byte flipB = frameTexture[flipIdx];
							frameTexture[idx] = flipB;
							frameTexture[flipIdx] = b;
						}
					}*/
					
					//we probably want 16 bit here instead...
					
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
        // update depth map texture
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
        // update short-throw AbImage texture
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
		
        // update long depth map texture
        if (startRealtimePreview && researchMode.LongDepthMapTextureUpdated())
        {
            //byte[] frameTexture = researchMode.GetLongDepthMapTextureBuffer();
            //if (frameTexture.Length > 0)
            {
                /*if (longDepthFrameData == null)
                {
                    longDepthFrameData = frameTexture;
                }
                else
                {
                    System.Buffer.BlockCopy(frameTexture, 0, longDepthFrameData, 0, longDepthFrameData.Length);
                }

                longDepthMediaTexture.LoadRawTextureData(longDepthFrameData);
                longDepthMediaTexture.Apply();*/
				
				float currTime = Time.time;
				
				if(_lastCaptureTime == 0.0)
				{
					_lastCaptureTime = currTime;
				}
				
				if(currTime - _lastCaptureTime > 1f)
				{
					if(!_isCapturing)
					{
						_isCapturing = true;
						PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
					}
					
					//write out this PNG... do this at the same time when the color photo is saved...
					/*byte[] pngData = longDepthMediaTexture.EncodeToPNG();
					string filename = string.Format(@"CapturedImageDepth{0}_n.png", currTime);
					File.WriteAllBytes(System.IO.Path.Combine(Application.persistentDataPath, filename), pngData);
					
					float[] depthPos = researchMode.GetDepthSensorPosition();
					string depthString = depthPos[0].ToString("F4") + " " + depthPos[1].ToString("F4") + " " + depthPos[2].ToString("F4") + "\n";
					string filenameTxt = string.Format(@"Position{0}_n.txt", currTime);
					System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, filenameTxt), depthString);*/
				}
            }
        }

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
        tcpClient.SendUINT16Async(depthMap, AbImage);
#endif
#endif
    }
    #endregion
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) StopSensorsEvent();
    }
}