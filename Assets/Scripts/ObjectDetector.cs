/*
 * Helpful guides:
 * https://localjoost.github.io/HoloLens-AI-using-Yolo-ONNX-models-to-localize-objects-in-3D-space/
 * https://github.com/Unity-Technologies/sentis-samples/tree/main/DepthEstimationSample
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;
using Unity.Sentis;
using System.Runtime.InteropServices;


[System.Serializable]
public class StartUpReport
{
	public string device;
	public string model;
	public long experiment_start;

	public List<string> cameras;

	public int model_input_width;
	public int model_input_height;

	public int camera_width;
	public int camera_height;
}

[System.Serializable]
public class DetectionResult
{
	public int num_detected;
	public int num_persons;

	public float max_score;
	public string max_label;

	public HashSet<int> detected_classes;
}

[System.Serializable]
public class ImageProcessingResult
{
	public string device;
	public string model;
	public long experiment_start;
	public int number;
	public string filename;

	public int num_detected;
	public int num_persons;

	public float max_score;
	public string max_label;

	public long execution_time_ms;
	public long encode_time_ms;
}

[System.Serializable]
public class NewPhotoResult
{
	public int id;
	public string filename;
}

public enum DetectionMode
{
	Off,			// Detection dispabled, only send in continuous transmission mode
	MaxScore,		// Test for probable presence of object using max operator (relatively fast)
	BoundingBox,	// Bounding box detection using nonmaximum suppression (slower)
}

public enum TransmitMode
{
	Off,			// Disables detection and sending frames
	Selective,		// Selectively send photos based on some heuristics
	Person,			// Only send frames that appear to contain a person
	Continuous,		// Continuously send frames to the server
}


public class ObjectDetector : MonoBehaviour
{
	public DetectionMode detectionMode = DetectionMode.MaxScore;
	public TransmitMode transmitMode = TransmitMode.Selective;

	// Minimum time between transmitted frames, not considered in continuous mode.
	public int minSendInterval = 1000;

	public ModelAsset modelAsset;

	IWorker engine;
	WebCamTexture webcamTexture;
	RenderTexture scaledTexture;
	RenderTexture outputTexture;

	// Size of input to ML model
	public int modelInputWidth = 640;
	public int modelInputHeight = 480;

	public int requestedFPS = 15;

	// Maxmimum time (ms) to spend per frame in executing model
	public int computeTimePerFrame = 10;

	public float detectionThreshold = 0.65f;

	public bool drawBoundingBoxes = false;

	// For experiments, send to an alternate server
	public bool sendToExperimentServer = false;
	public string experimentServerURL = "http://easyvizar.wings.cs.wisc.edu:5001";

	// Divide photo into patches for ROI encoding
	public int numPatchesX = 16;
	public int numPatchesY = 16;
	public bool sendAsPatches = false;

	// Only start actively sending photos after a QR code has been scanned
	private bool qrScanned = false;
	private string locationID;

	// We figure this out after initializing the camera.
	// We might need to scale the image to match the ML model input size.
	private int cameraWidth = 0;
	private int cameraHeight = 0;
	private bool needScaling = false;

	bool captureStarted = false;
	private long execution_start_time;

	private long lastSendTime = 0;
	private HashSet<int> lastSendDetectionSet = new HashSet<int>();

	Tensor inputTensor;
	IEnumerator executionSchedule;

	private string[] classNames = { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

	private string current_filename = "";
	private int counter = 0;
	private long experiment_start_time = 0;

	public long GetTimestamp()
    {
		return DateTimeOffset.Now.ToUnixTimeMilliseconds();
	}

	IEnumerator Start()
	{
		Application.targetFrameRate = 60;

		experiment_start_time = GetTimestamp();

		initializeEngine();
		initializeCamera();

		if (sendToExperimentServer)
			yield return sendStartUpReport();

		GameObject qrscanner = GameObject.Find("QRScanner");
		var scanner = qrscanner.GetComponent<QRScanner>();
		scanner.LocationChanged += (o, ev) =>
		{
			locationID = ev.LocationID;
			qrScanned = true;
		};
	}

	private void initializeEngine()
    {
		var model = ModelLoader.Load(modelAsset);

		if (detectionMode == DetectionMode.BoundingBox)
		{
			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"score_threshold",
				new TensorFloat(new TensorShape(1), new[] { detectionThreshold })
			));

			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"iou_threshold",
				new TensorFloat(new TensorShape(1), new[] { 0.3f })
			));

			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"max_output_boxes_per_class",
				new TensorInt(new TensorShape(1), new[] { 200 })
			));

			model.layers.Add(new Unity.Sentis.Layers.Transpose(
				"transposed_boxes",
				"/model.22/Mul_2_output_0",
				new[] { 0, 2, 1 }
			));

			model.layers.Add(new Unity.Sentis.Layers.NonMaxSuppression(
				"selected_indices",             // name
				"transposed_boxes",             // boxes
				"/model.22/Sigmoid_output_0",   // scores
				maxOutputBoxesPerClass: "max_output_boxes_per_class",
				iouThreshold: "iou_threshold",
				scoreThreshold: "score_threshold",
				centerPointBox: Unity.Sentis.Layers.CenterPointBox.Center
			));

			model.outputs = new List<string>() { "output0", "selected_indices" };
		}
        else if (detectionMode == DetectionMode.MaxScore)
        {
			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"max_score_axes",
				new TensorInt(new TensorShape(1), new[] { 2 })
			));

			model.layers.Add(new Unity.Sentis.Layers.ReduceMax(
				"max_score",
				new[] { "/model.22/Sigmoid_output_0", "max_score_axes" },
				keepdims: false
			));

			model.outputs = new List<string>() { "output0", "max_score" };
		}

		// haven't figured out how to parse this JSON string, hence I just use a constant array of class names
		//var names = model.Metadata["names"];

		engine = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);
	}

	private void initializeCamera()
    {
		WebCamDevice[] devices = WebCamTexture.devices;
		webcamTexture = new WebCamTexture(modelInputWidth, modelInputHeight, requestedFPS);
		webcamTexture.deviceName = devices[0].name;
		webcamTexture.Play();

		cameraWidth = webcamTexture.width;
		cameraHeight = webcamTexture.height;

		if (cameraWidth != modelInputWidth || cameraHeight != modelInputHeight)
		{
			needScaling = true;
			scaledTexture = new RenderTexture(modelInputWidth, modelInputHeight, 0, RenderTextureFormat.ARGBFloat);
		}

		outputTexture = new RenderTexture(cameraWidth, cameraHeight, 0, RenderTextureFormat.ARGBFloat);
	}

	void Update()
	{
		if (qrScanned && !captureStarted)
        {
			StartCoroutine(ProcessNextFrame());
		}
	}

	private IEnumerator ProcessNextFrame()
    {
		captureStarted = true;
		long start = GetTimestamp();

		startExecution();

		bool hasMoreWork = true;
		while (hasMoreWork)
        {
			hasMoreWork = executionSchedule.MoveNext();
			if (GetTimestamp() - start >= computeTimePerFrame)
			{
				yield return null;
				start = GetTimestamp();
			}
		}

		yield return finishExecution();
		captureStarted = false;
    }

    private void startExecution()
	{
		if (needScaling)
        {
			Graphics.Blit(webcamTexture, scaledTexture);
			inputTensor = TextureConverter.ToTensor(scaledTexture, -1, -1, 3);
		}
		else
        {
			inputTensor = TextureConverter.ToTensor(webcamTexture, -1, -1, 3);
		}
		Graphics.Blit(webcamTexture, outputTexture);

		execution_start_time = GetTimestamp();
		executionSchedule = engine.StartManualSchedule(inputTensor);
		//engine.Execute(inputTensor);
	}

	private IEnumerator finishExecution()
    {
		var report = new ImageProcessingResult();
		report.device = SystemInfo.deviceName;
		report.model = modelAsset.name;
		report.experiment_start = experiment_start_time;
		report.number = counter;

		counter++;

		report.execution_time_ms = GetTimestamp() - execution_start_time;

		Texture2D texture = new Texture2D(outputTexture.width, outputTexture.height, TextureFormat.RGBA32, false);
		RenderTexture.active = outputTexture;
		texture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);

		DetectionResult dresult;
		if (detectionMode == DetectionMode.BoundingBox)
			dresult = postprocessWithBBoxes(texture);
		else if (detectionMode == DetectionMode.MaxScore)
			dresult = postprocessWithoutBBoxes(texture);
		else
			dresult = new DetectionResult();

		report.max_label = dresult.max_label;
		report.max_score = dresult.max_score;
		report.num_detected = dresult.num_detected;
		report.num_persons = dresult.num_persons;

		texture.Apply();
		RenderTexture.active = null;

		yield return null;

		inputTensor.Dispose();

		byte[] image = null;

		if (shouldSendFrame(dresult))
        {
			long encode_start = GetTimestamp();

			// Really slow (100-200ms) on the HoloLens
			//image = texture.EncodeToPNG();

			// JPEG encoder seems to be faster than PNG but still pretty slow (~30ms) on the HoloLens
			//image = texture.EncodeToJPG();

			// This might help by running the encoder in a background thread but not completely sure
			// There would be some overhead associated with sending the data to a worker thread
			var rawData = texture.GetRawTextureData();
			var format = texture.graphicsFormat;
			uint width = (uint)texture.width;
			uint height = (uint)texture.height;
			var task = Task.Run<byte[]>(() => ImageConversion.EncodeArrayToJPG(rawData, format, (uint)width, (uint)height, quality: 100));
			yield return new WaitUntil(() => task.IsCompleted);
			image = task.Result;

			// EXPERIMENTAL
			if (sendAsPatches)
			{
				yield return encodeAndSendPatches(texture);
			}

			report.encode_time_ms = GetTimestamp() - encode_start;

			lastSendTime = encode_start;
			lastSendDetectionSet = dresult.detected_classes;
		}

		yield return sendResults(image, report);
	}

	private IEnumerator encodeAndSendPatches(Texture2D texture)
    {
		int patchWidth = cameraWidth / 16;
		int patchHeight = cameraHeight / 16;

		List<IMultipartFormSection> patches = new List<IMultipartFormSection>();

		for (int y = 0; y < numPatchesY; y++)
        {
			for (int x = 0; x < numPatchesX; x++)
            {
				// Just for testing, generate patches with checkerboard pattern of compression level.
				int quality = ((x + y) % 2 == 0) ? (1 + 2 * x) : (100 - (2 * x));

				var pixels = texture.GetPixels(x * patchWidth, (numPatchesY - y - 1) * patchHeight, patchWidth, patchHeight);
				var data = ImageConversion.EncodeArrayToJPG(pixels, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)patchWidth, (uint)patchHeight, quality: quality);

				var section = new MultipartFormFileSection("patches", data, $"{y:D2}_{x:D2}.jpg", "image/jpg");
				patches.Add(section);
            }

			// I think encoding is a bit slow, so take a break after every row.
			yield return null;
		}

		yield return sendPatches(patches);
	}

	private bool shouldSendFrame(DetectionResult dresult)
    {
		if (transmitMode == TransmitMode.Continuous)
			return true;

		// Other than Continuous mode, we want to limit how often we send frames to the server.
		long interval = GetTimestamp() - lastSendTime;
		if (interval < minSendInterval)
			return false;

		if (transmitMode == TransmitMode.Person && dresult.num_persons > 0)
			return true;

		// In Selective mode, check if the set of detected objects is different from the previously sent set.
		// The idea is to avoid sending redundant photos if the scene stays the same.
		if (transmitMode == TransmitMode.Selective && !dresult.detected_classes.SetEquals(lastSendDetectionSet))
			return true;

		return false;
    }

	private DetectionResult postprocessWithBBoxes(Texture2D texture)
    {
		DetectionResult dresult = new DetectionResult();

		var boxes = engine.PeekOutput("output0") as TensorFloat;
		var indices = engine.PeekOutput("selected_indices") as TensorInt;

		boxes.MakeReadable();
		indices.MakeReadable();

#if UNITY_EDITOR
		Debug.Log($"Boxes shape {boxes.shape.ToString()}");
		Debug.Log($"Indices shape {indices.shape.ToString()}");
#endif

		float rescaleX = (float)cameraWidth / (float)modelInputWidth;
		float rescaleY = (float)cameraHeight / (float)modelInputHeight;

		int displayMaxY = outputTexture.height - 1;

		dresult.detected_classes = new HashSet<int>();

		for (int i = 0; i < indices.shape[0]; i++)
		{
			var cid = indices[i, 1];
			var index = indices[i, 2];

			float cx = boxes[0, 0, index] * rescaleX;
			float cy = boxes[0, 1, index] * rescaleY;
			float w = boxes[0, 2, index] * rescaleX;
			float h = boxes[0, 3, index] * rescaleY;

			float score = boxes[0, 4 + cid, index];

			dresult.num_detected++;
			if (cid == 0)
			{
				dresult.num_persons++;
			}

			if (score > detectionThreshold)
            {
				dresult.detected_classes.Add(cid);
            }

			if (score > dresult.max_score)
			{
				dresult.max_score = score;
				dresult.max_label = classNames[cid];
			}

			if (drawBoundingBoxes)
			{
				int top = (int)(cy - 0.5 * h);
				if (top < 0) top = 0;
				int bottom = top + (int)w;
				if (bottom >= outputTexture.height) bottom = outputTexture.height - 1;
				int left = (int)(cx - 0.5 * w);
				if (left < 0) left = 0;
				int right = left + (int)w;
				if (right >= outputTexture.width) right = outputTexture.width - 1;

				var color = Color.green;
				if (cid == 0)
				{
					color = Color.red;
				}

				for (int y = top; y < bottom; y++)
				{
					texture.SetPixel(left, displayMaxY - y, color);
					texture.SetPixel(right, displayMaxY - y, color);
				}

				for (int x = left; x < right; x++)
				{
					texture.SetPixel(x, displayMaxY - top, color);
					texture.SetPixel(x, displayMaxY - bottom, color);
				}
			}

#if UNITY_EDITOR
			Debug.Log($"  Box {i} class {classNames[cid]} score {score}: {cx} {cy} {w} {h}");
#endif
		}

		return dresult;
	}

	private DetectionResult postprocessWithoutBBoxes(Texture2D texture)
    {
		DetectionResult dresult = new DetectionResult();

		var output = engine.PeekOutput("max_score") as TensorFloat;
		output.MakeReadable();

#if UNITY_EDITOR
		Debug.Log($"Scores shape {output.shape.ToString()}");
#endif

		dresult.detected_classes = new HashSet<int>();

		if (output[0, 0] > detectionThreshold)
		{
			dresult.num_persons = 1;
		}

		for (int i = 0; i < output.shape[1]; i++)
		{
			if (output[0, i] > dresult.max_score)
			{
				dresult.max_score = output[0, i];
				dresult.max_label = classNames[i];
			}

			if (output[0, i] > detectionThreshold)
			{
				dresult.num_detected++;
				dresult.detected_classes.Add(i);
			}
		}

#if UNITY_EDITOR
		Debug.Log($"Score for person {output[0, 0]}");
#endif
		return dresult;
	}

	private IEnumerator sendResults(byte[] image, ImageProcessingResult report)
    {
		if (image is not null)
        {
			yield return sendPhoto(image, "image/jpg");
			report.filename = current_filename;
        }

		yield return sendReport(report);
    }

	private void OnDestroy()
	{
		engine.Dispose();
		outputTexture.Release();
	}

	private IEnumerator sendStartUpReport()
	{
		var report = new StartUpReport();
		report.model = modelAsset.name;
		report.experiment_start = experiment_start_time;
		report.device = SystemInfo.deviceName;

		report.model_input_width = modelInputWidth;
		report.model_input_height = modelInputHeight;
		report.camera_width = cameraWidth;
		report.camera_height = cameraHeight;

		report.cameras = new List<string>();
		foreach (var device in WebCamTexture.devices)
        {
			report.cameras.Add(device.name);
        }

		var url = experimentServerURL + "/reports";
		UnityWebRequest www = new UnityWebRequest(url, "POST");

		www.SetRequestHeader("Content-Type", "application/json");
		www.SetRequestHeader("X-Type", "start-up");

		var json_data = JsonUtility.ToJson(report);
		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(json_data);

		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		www.Dispose();
	}

	private IEnumerator sendReport(ImageProcessingResult result)
    {
		var url = experimentServerURL + "/reports";
		UnityWebRequest www = new UnityWebRequest(url, "POST");

		www.SetRequestHeader("Content-Type", "application/json");
		www.SetRequestHeader("X-Type", "detection");

		var json_data = JsonUtility.ToJson(result);
		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(json_data);

		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		www.Dispose();
	}

	IEnumerator sendPhoto(byte[] data, string contentType)
	{
		string url;
		if (sendToExperimentServer)
			url = experimentServerURL + "/photos";
		else
			url = EasyVizARServer.Instance.GetBaseURL() + "/photos";

		UnityWebRequest www = new UnityWebRequest(url, "POST");

		www.SetRequestHeader("Authorization", EasyVizARServer.Instance.GetAuthorizationHeader());
		www.SetRequestHeader("Content-Type", contentType);

		www.uploadHandler = new UploadHandlerRaw(data);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result == UnityWebRequest.Result.Success)
		{
			var created_photo = JsonUtility.FromJson<NewPhotoResult>(www.downloadHandler.text);
			current_filename = created_photo.filename;
		}
		else
		{
			current_filename = "";
		}

		www.Dispose();
	}

	IEnumerator sendPatches(List<IMultipartFormSection> patches)
	{
		string url;
		if (sendToExperimentServer)
			url = experimentServerURL + "/photos";
		else
			url = EasyVizARServer.Instance.GetBaseURL() + "/photos";

		UnityWebRequest www = UnityWebRequest.Post(url, patches);

		www.SetRequestHeader("Authorization", EasyVizARServer.Instance.GetAuthorizationHeader());
		//www.SetRequestHeader("Content-Type", contentType);

		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result == UnityWebRequest.Result.Success)
		{
			var created_photo = JsonUtility.FromJson<NewPhotoResult>(www.downloadHandler.text);
			current_filename = created_photo.filename;
		}
		else
		{
			current_filename = "";
		}

		www.Dispose();
	}
}
