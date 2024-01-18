using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Windows.WebCam;
using System.Linq;
using UnityEngine.InputSystem;

public class TakeColorPhoto : MonoBehaviour
{
	private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;

	public EasyVizARHeadsetManager headsetManager;

	[SerializeField]
	int continuousResolutionWidth = 760;

	[SerializeField]
	int continuousResolutionHeight = 428;

	[SerializeField]
	float continuousHologramOpacity = 0.0f;

	[Header("Manual Trigger")]
	[Tooltip("Click to trigger a photo capture.")]
	public bool triggerCapture = false;

	private string _currentFilePath;
	private int _currentWidth;
	private int _currentHeight;

	bool _isCapturing = false;
	bool _continuousCapture = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void OnValidate()
    {
		if (triggerCapture)
        {
			TakeAColorPhoto();
			triggerCapture = false;
        }
    }

	public void TakeAColorPhoto()
    {
		if (!_isCapturing)
        {
			_isCapturing = true;
			PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		}
    }

	public void TakeAColorPhoto(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
            if (!_isCapturing)
            {
                _isCapturing = true;
                PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
            }
        }
	}

	public void BeginContinuousCapture()
    {
		_continuousCapture = true;
		if (!_isCapturing)
        {
			_isCapturing = true;
			PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		}
	}
	
	public void EndContinuousCapture()
    {
		_continuousCapture = false;
	}

	void OnPhotoCaptureCreated(UnityEngine.Windows.WebCam.PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		CameraParameters c = new UnityEngine.Windows.WebCam.CameraParameters();
		c.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

		if (_continuousCapture)
        {
			c.hologramOpacity = continuousHologramOpacity;
			c.cameraResolutionWidth = continuousResolutionWidth;
			c.cameraResolutionHeight = continuousResolutionHeight;
		}
        else
        {
			Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

			c.hologramOpacity = 0.0f;
			c.cameraResolutionWidth = cameraResolution.width;
			c.cameraResolutionHeight = cameraResolution.height;
		}
		
		_currentWidth = c.cameraResolutionWidth;
		_currentHeight = c.cameraResolutionHeight;

		captureObject.StartPhotoModeAsync(c, delegate(PhotoCapture.PhotoCaptureResult result) {
			// Take a picture
			string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
			string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
			_currentFilePath = filePath;

			photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
		});
	}
	
	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
		_isCapturing = false;
	}
	
	private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
			string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
			_currentFilePath = filePath;

			photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
		}
		else
		{
			Debug.LogError("Unable to start photo mode!");
		}
	}
	
	void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			Debug.Log("Saved Photo to disk: " + _currentFilePath);
			StartCoroutine(UploadImage(_currentFilePath, _currentWidth, _currentHeight));
			
			if (_continuousCapture)
			{
				OnPhotoModeStarted(result);
			}
			else
			{
				photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
			}
		}
		else
		{
			Debug.Log("Failed to save Photo to disk");
		}
	}

	IEnumerator UploadImage(string path, int width, int height)
    {
		string baseURL = EasyVizARServer.Instance.GetBaseURL();
		UnityWebRequest www = new UnityWebRequest(baseURL + "/photos", "POST");
		www.SetRequestHeader("Authorization", EasyVizARServer.Instance.GetAuthorizationHeader());
		www.SetRequestHeader("Content-Type", "image/png");

		www.uploadHandler = new UploadHandlerFile(path);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();
		if (www.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr.txt"), www.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut.txt"), "successfully sent photo");
		}

		www.Dispose();
	}
}
