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
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = width;
		h.height = height;
		h.contentType = "image/png";
		h.camera_location_id = headsetManager.LocationID;

		var headset = headsetManager.LocalHeadset;
		if (headset != null)
        {
			var hsObject = headset.GetComponent<EasyVizARHeadset>();
			if (hsObject != null)
            {
				//h.created_by = hsObject._headsetID;
			}

			h.camera_position = new EasyVizAR.Position();
			h.camera_position.x = headset.transform.position.x;
			h.camera_position.y = headset.transform.position.y;
			h.camera_position.z = headset.transform.position.z;

			h.camera_orientation = new EasyVizAR.Orientation();
			h.camera_orientation.x = headset.transform.rotation.x;
			h.camera_orientation.y = headset.transform.rotation.y;
			h.camera_orientation.z = headset.transform.rotation.z;
			h.camera_orientation.w = headset.transform.rotation.w;
		}

		string baseURL = EasyVizARServer.Instance.GetBaseURL();

		UnityWebRequest www = new UnityWebRequest(baseURL + "/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");

		string ourJson = JsonUtility.ToJson(h);

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

		UnityWebRequest www2 = new UnityWebRequest(baseURL + h2.imageUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(path);//new UploadHandlerRaw(image_as_bytes2);//
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
}
