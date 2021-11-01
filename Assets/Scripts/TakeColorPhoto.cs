using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.WebCam;
using System.Linq;

public class TakeColorPhoto : MonoBehaviour
{
	private UnityEngine.Windows.WebCam.PhotoCapture photoCaptureObject = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
	public void TakeAColorPhoto()
	{
		PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
	}
	
	void OnPhotoCaptureCreated(UnityEngine.Windows.WebCam.PhotoCapture captureObject)
	{
		photoCaptureObject = captureObject;

		Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		CameraParameters c = new UnityEngine.Windows.WebCam.CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = UnityEngine.Windows.WebCam.CapturePixelFormat.BGRA32;

		captureObject.StartPhotoModeAsync(c, delegate(PhotoCapture.PhotoCaptureResult result) {
			// Take a picture
			string filename = string.Format(@"CapturedImage{0}_n.png", Time.time);
			string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

			photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.PNG, OnCapturedPhotoToDisk);
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
			Debug.Log("Saved Photo to disk!");
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
		}
		else
		{
			Debug.Log("Failed to save Photo to disk");
		}
	}
}
