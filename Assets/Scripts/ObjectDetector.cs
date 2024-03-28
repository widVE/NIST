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
using UnityEngine.Rendering;
using Unity.Collections;

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

	public int binary_size;
	public float average_quality;
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
	CoarseSegment,	// Small grid of values indicating detection strength (fast)
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

	// Try to send a frame at least this often (applies to selective mode).
	public int maxSendInterval = 5000;

	public ModelAsset detectionModel;
	public ModelAsset coarseSegmentationModel;

	public GameObject headAttachedDisplay;
	private HeadAttachedText headAttachedText;

	string modelName;
	IWorker engine;
	WebCamTexture webcamTexture;
	RenderTexture scaledTexture;
	RenderTexture outputTexture;

	// Size to request from camera
	// This need not match the model input because we can scale the image
	public int requestCameraWidth = 640;
	public int requestCameraHeight = 480;

	public int requestedFPS = 15;

	// Maxmimum time (ms) to spend per frame in executing model
	public int computeTimePerFrame = 10;

	public float detectionThreshold = 0.65f;
	public float foregroundThreshold = 0.45f;

	public bool drawBoundingBoxes = false;

	// For experiments, send to an alternate server
	public bool sendToExperimentServer = false;
	public string experimentServerURL = "http://easyvizar.wings.cs.wisc.edu:5001";

	// If performing ROI encoding, also send the original image (primarily for experiments).
	public bool sendOriginalWithPatches = false;

	// Whether to send as individually encoded JPEG patches. It seems the better alternative is
	// to send as one JPEG image with independently scaled regions.
	public bool sendAsPatches = false;

	// Image quality for ordinary JPEG encoding, range 1-100.
	public int imageQuality = 100;
	public int backgroundScaleFactor = 20;

	// Minimum quality to use for image patch encoding.
	// The lowest valid setting is 1, but slightly higher may be preferred.
	public int minimumPatchQuality = 10;

	// Server image queue to use (detection|identification|done).
	public string uploadQueueName = "detection";

	// Speed limits to prevent capturing images when the camera is moving.
	public float speedLimit = 0.9f;
	public float angularSpeedLimit = 90.0f;

	public Material multiScaleMask;

	// Size of input to ML model
	private int modelInputWidth = -1;
	private int modelInputHeight = -1;

	// Only start actively sending photos after a QR code has been scanned
	private bool running = false;
	private bool sentStartupReport = false;

	// We figure this out after initializing the camera.
	// We might need to scale the image to match the ML model input size.
	private int cameraWidth = -1;
	private int cameraHeight = -1;
	private bool needScaling = false;

	bool captureStarted = false;

	private long lastSendTime = 0;
	private HashSet<int> lastSendDetectionSet = new HashSet<int>();

	Tensor inputTensor;
	IEnumerator executionSchedule;

	private string[] classNames = { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

	private string current_filename = "";
	private int current_photo_id = -1;
	private int counter = 0;
	private long experiment_start_time = 0;

	private Quaternion lastOrientation;
	private long lastOrientationTime;
	private float estimatedAngularSpeed;

	public long GetTimestamp()
    {
		return DateTimeOffset.Now.ToUnixTimeMilliseconds();
	}

    void Start()
	{
		Application.targetFrameRate = 60;

		experiment_start_time = GetTimestamp();

		// Disable the game object until enabled by configuration loader below.
		gameObject.SetActive(false);

		if (headAttachedDisplay)
			headAttachedText = headAttachedDisplay.GetComponent<HeadAttachedText>();

		GameObject headsetManager = GameObject.Find("EasyVizARHeadsetManager");
		if (headsetManager)
        {
			var manager = headsetManager.GetComponent<EasyVizARHeadsetManager>();
			manager.HeadsetConfigurationChanged += (sender, change) =>
			{
				var config = change.Configuration;
				switch(config.photo_capture_mode)
                {
					case "objects":
						detectionMode = DetectionMode.MaxScore;
						transmitMode = TransmitMode.Selective;
						uploadQueueName = "detection";
						break;
					case "people":
						detectionMode = DetectionMode.CoarseSegment;
						transmitMode = TransmitMode.Person;
						uploadQueueName = "identification";
						break;
					case "continuous":
						detectionMode = DetectionMode.Off;
						transmitMode = TransmitMode.Continuous;
						break;
					default:
						detectionMode = DetectionMode.Off;
						transmitMode = TransmitMode.Off;
						break;
				}

				maxSendInterval = (int)(config.photo_target_interval * 1000.0f);
				detectionThreshold = config.photo_detection_threshold;

				running = config.photo_capture_mode != "off";
				gameObject.SetActive(running);
			};
		}
	}

    private void OnEnable()
    {
		if (running)
        {
			Debug.Log("Initializing ObjectDetector");
			initializeEngine();
			initializeCamera();
			StartCoroutine(CaptureLoop());
		}
	}

    private void OnDisable()
    {
		if (running)
		{
			webcamTexture.Stop();
			running = false;
		}
	}

	private void initializeEngine()
    {
		ModelAsset asset = detectionModel;
		if (detectionMode == DetectionMode.CoarseSegment)
			asset = coarseSegmentationModel;

		Model model = ModelLoader.Load(asset);
		modelName = asset.name;
		Debug.Log($"Loaded model {modelName}");

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
		else if (detectionMode == DetectionMode.CoarseSegment)
		{
			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"norm_scale",
				new TensorFloat(new TensorShape(3), new[] { 1.0f, 1.0f, 1.0f })
			));

			model.AddConstant(new Unity.Sentis.Layers.Constant(
				"norm_bias",
				new TensorFloat(new TensorShape(3), new[] { 0.0f, 0.0f, 0.0f })
			));

			int n = model.layers.Count;

			// First add the new layer. This will append at the end of the layers list (index n).
			// I think we need to call AddLayer rather than modify the layer list directly because
			// it probably sets some internal metadata.
			model.AddLayer(new Unity.Sentis.Layers.InstanceNormalization(
				model.layers[0].inputs[0], // output name set to the input name of the next layer
				"norm_input", // input name, will become the new model input
				"norm_scale",
				"norm_bias"
			));

			// Then move the normalization layer to to the top of the list.
			var layer = model.layers[n];
			model.layers.RemoveAt(n);
			model.layers.Insert(0, layer);
			
			// Replace what is considered the model input with our normalization layer.
			model.AddInput("norm_input", model.inputs[0].dataType, model.inputs[0].shape);
			model.inputs.RemoveAt(0);
		}

		// Assuming only one input, the image
		// Shape should by batch, channel, height, width (b, c, h, w)
		var input = model.inputs[0];
		if (input.shape.IsFullyKnown())
		{
			var shape = input.shape.ToTensorShape();
			modelInputHeight = shape[2];
			modelInputWidth = shape[3];
		}

		// haven't figured out how to parse this JSON string, hence I just use a constant array of class names
		//var names = model.Metadata["names"];

		engine = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);
	}

	private void initializeCamera()
    {
		WebCamDevice[] devices = WebCamTexture.devices;
		webcamTexture = new WebCamTexture(requestCameraWidth, requestCameraHeight, requestedFPS);
		webcamTexture.deviceName = devices[0].name;
		webcamTexture.Play();

		cameraWidth = webcamTexture.width;
		cameraHeight = webcamTexture.height;

		// The model is initialized before the camera, so this means perhas the model has dynamic input size.
		// This has not been tested!
		if (modelInputWidth < 0 || modelInputHeight < 0)
        {
			modelInputWidth = cameraWidth;
			modelInputHeight = cameraHeight;
        }

		else if (cameraWidth != modelInputWidth || cameraHeight != modelInputHeight)
		{
			needScaling = true;
			scaledTexture = new RenderTexture(modelInputWidth, modelInputHeight, 0, RenderTextureFormat.ARGBFloat);
		}

		outputTexture = new RenderTexture(cameraWidth, cameraHeight, 0, RenderTextureFormat.ARGBFloat);

		lastOrientation = Camera.main.transform.rotation;
		lastOrientationTime = GetTimestamp();
	}

	private IEnumerator CaptureLoop()
    {
		var stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		while (running)
        {
			if (shouldProcessFrame())
            {
				yield return ProcessNextFrame();

				// Slow down how often we process camera frames to match the configured minimum interval.
				long remaining = minSendInterval - stopwatch.ElapsedMilliseconds;
				if (remaining > 0)
				{
					yield return new WaitForSeconds(remaining / 1000.0f);
				}

				stopwatch.Restart();
			}
			else
            {
				yield return null;
            }
        }
    }

	private IEnumerator ProcessNextFrame()
    {
		captureStarted = true;

		if (!sentStartupReport && sendToExperimentServer)
        {
			yield return sendStartUpReport();
			sentStartupReport = true;
		}

		startExecution();

		long executionTime = 0;

		var stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		bool hasMoreWork = true;
		while (hasMoreWork)
        {
			hasMoreWork = executionSchedule.MoveNext();

			if (stopwatch.ElapsedMilliseconds > computeTimePerFrame)
			{
				executionTime += stopwatch.ElapsedMilliseconds;
				yield return null;
				stopwatch.Restart();
			}
		}

		executionTime += stopwatch.ElapsedMilliseconds;

		var report = new ImageProcessingResult();
		report.device = SystemInfo.deviceName;
		report.model = modelName;
		report.experiment_start = experiment_start_time;
		report.number = counter;
		report.execution_time_ms = executionTime;

		counter++;

		yield return finishExecution(report);
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
		
		executionSchedule = engine.StartManualSchedule(inputTensor);
		//engine.Execute(inputTensor);
	}

	private IEnumerator finishExecution(ImageProcessingResult report)
    {
		Texture2D texture = new Texture2D(outputTexture.width, outputTexture.height, TextureFormat.RGBA32, false);
		RenderTexture.active = outputTexture;

		//texture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);

		// ReadPixels is really slow. AsyncGPUReadback should be a bit better.
		bool textureReady = false;
		AsyncGPUReadback.Request(outputTexture, 0, TextureFormat.RGBA32, (request) =>
			{
				texture.LoadRawTextureData(request.GetData<uint>());
				texture.Apply();
				textureReady = true;
			}
		);
		yield return new WaitUntil(() => textureReady);

		DetectionResult dresult;
		if (detectionMode == DetectionMode.BoundingBox)
			dresult = postprocessWithBBoxes(texture);
		else if (detectionMode == DetectionMode.MaxScore)
			dresult = postprocessWithoutBBoxes(texture);
		else if (detectionMode == DetectionMode.CoarseSegment)
			dresult = postprocessCoarseSegmentation(texture);
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
			if (detectionMode == DetectionMode.CoarseSegment)
			{
				if (sendAsPatches)
				{
					yield return encodeAndSendPatches(texture, report);
					report.filename = current_filename;

					if (sendToExperimentServer)
					{
						yield return sendReport(report);
					}
				}
                else
                {
					long encode_start = GetTimestamp();

					var composite = encodeMultiScale(texture, report);

					// This might help by running the encoder in a background thread but not completely sure
					// There would be some overhead associated with sending the data to a worker thread
					var rawData = composite.GetRawTextureData();
					var format = composite.graphicsFormat;
					uint width = (uint)composite.width;
					uint height = (uint)composite.height;
					var task = Task.Run<byte[]>(() => ImageConversion.EncodeArrayToJPG(rawData, format, (uint)width, (uint)height, quality: imageQuality));
					yield return new WaitUntil(() => task.IsCompleted);
					image = task.Result;

					report.binary_size = image.Length;
					report.encode_time_ms = GetTimestamp() - encode_start;

					yield return sendPhoto(image, "image/jpg");
					report.filename = current_filename;

					if (sendToExperimentServer)
					{
						yield return sendReport(report);
					}
					else if (headAttachedDisplay is not null)
                    {
						yield return waitAndDisplayResult();
                    }

					// Do we need to call task.Dispose()? Not sure.
					// https://devblogs.microsoft.com/pfxteam/do-i-need-to-dispose-of-tasks/
					task.Dispose();

					Destroy(composite);
				}
			}

			if (detectionMode != DetectionMode.CoarseSegment || sendOriginalWithPatches)
			{
				long encode_start = GetTimestamp();

				// This might help by running the encoder in a background thread but not completely sure
				// There would be some overhead associated with sending the data to a worker thread
				var rawData = texture.GetRawTextureData();
				var format = texture.graphicsFormat;
				uint width = (uint)texture.width;
				uint height = (uint)texture.height;
				var task = Task.Run<byte[]>(() => ImageConversion.EncodeArrayToJPG(rawData, format, (uint)width, (uint)height, quality: imageQuality));
				yield return new WaitUntil(() => task.IsCompleted);
				image = task.Result;

				report.binary_size = image.Length;
				report.encode_time_ms = GetTimestamp() - encode_start;
				report.average_quality = (float)imageQuality;

				yield return sendPhoto(image, "image/jpg");
				report.filename = current_filename;

				if (sendToExperimentServer)
				{
					yield return sendReport(report);
				}

				// Do we need to call task.Dispose()? Not sure.
				// https://devblogs.microsoft.com/pfxteam/do-i-need-to-dispose-of-tasks/
				task.Dispose();
			}

			lastSendTime = GetTimestamp();
			lastSendDetectionSet = dresult.detected_classes;
		}

		// We will cause a memory leak if we do not manually free Texture2D objects
		Destroy(texture);
	}

	private RenderTexture rescaleTexture(Texture2D texture, int reduction)
    {
		var scaledRT = new RenderTexture(texture.width / reduction, texture.height / reduction, 0, RenderTextureFormat.ARGB32);
		var rescaledRT = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
		Graphics.Blit(texture, scaledRT);
		Graphics.Blit(scaledRT, rescaledRT);
		scaledRT.Release();
		return rescaledRT;
	}

	private Texture2D encodeMultiScale(Texture2D texture, ImageProcessingResult report)
	{
		var output = engine.PeekOutput("output") as TensorFloat;
		output.MakeReadable();

		int numPatchesX = output.shape[3];
		int numPatchesY = output.shape[2];

		int patchWidth = cameraWidth / numPatchesX;
		int patchHeight = cameraHeight / numPatchesY;

		var stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		Texture2D compositeTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
		Graphics.CopyTexture(texture, compositeTexture);

		var background = rescaleTexture(texture, backgroundScaleFactor);

		float summedQuality = 0;

		Color[] clear = new Color[patchWidth * patchHeight];
		for (int i = 0; i < clear.Length; i++)
        {
			clear[i] = Color.clear;
        }

		for (int i = 0; i < numPatchesY; i++)
		{
			for (int j = 0; j < numPatchesX; j++)
			{
				int y = i * patchHeight;
				int x = j * patchWidth;

				if (output[0, 0, i, j] < foregroundThreshold)
				{
					summedQuality += 100.0f / backgroundScaleFactor;

					int fy = (numPatchesY - i - 1) * patchHeight;
					compositeTexture.SetPixels(x, fy, patchWidth, patchHeight, clear);
				}
                else
                {
					summedQuality += 100.0f;
				}
			}
		}

		compositeTexture.Apply();
		Graphics.Blit(compositeTexture, background, multiScaleMask);
		
		RenderTexture.active = background;
		compositeTexture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);

		report.average_quality = summedQuality / (numPatchesY * numPatchesX);
		report.encode_time_ms = stopwatch.ElapsedMilliseconds;

		RenderTexture.active = null;

		background.Release();

		return compositeTexture;
	}

	private IEnumerator encodeAndSendPatches(Texture2D texture, ImageProcessingResult report)
    {
		var output = engine.PeekOutput("output") as TensorFloat;
		output.MakeReadable();

		int numPatchesX = output.shape[3];
		int numPatchesY = output.shape[2];

		int patchWidth = cameraWidth / numPatchesX;
		int patchHeight = cameraHeight / numPatchesY;

		int binarySize = 0;
		long encodeTime = 0;
		int summedQuality = 0;

#if UNITY_EDITOR
		Debug.Log($"Original camera image size {cameraWidth}x{cameraHeight}");
		Debug.Log($"Patch size {patchWidth}x{patchHeight} grid {numPatchesX}x{numPatchesY}");
		Debug.Log($"Scores shape {output.shape.ToString()}");
#endif

		List<IMultipartFormSection> patches = new List<IMultipartFormSection>();

		var stopwatch = new System.Diagnostics.Stopwatch();
		stopwatch.Start();

		for (int y = 0; y < numPatchesY; y++)
        {
			for (int x = 0; x < numPatchesX; x++)
            {
				int quality = 1;

				if (output[0, 0, y, x] >= detectionThreshold)
				{
					quality = 100;
				}
				else
                {
					quality = (int)(100 * (output[0, 0, y, x] / detectionThreshold));
				}

				if (quality < minimumPatchQuality)
					quality = minimumPatchQuality;

				summedQuality += quality;

				var pixels = texture.GetPixels(x * patchWidth, (numPatchesY - y - 1) * patchHeight, patchWidth, patchHeight);
				var data = ImageConversion.EncodeArrayToJPG(pixels, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, (uint)patchWidth, (uint)patchHeight, quality: quality);
				binarySize += data.Length;

				var section = new MultipartFormFileSection("patches", data, $"{y:D2}_{x:D2}.jpg", "image/jpg");
				patches.Add(section);
            }

			// Image encoding is a bit slow, so take a break if our compute limit has been exceeded.
			if (stopwatch.ElapsedMilliseconds > computeTimePerFrame)
			{
				encodeTime += stopwatch.ElapsedMilliseconds;
				yield return null;
				stopwatch.Restart();
			}
		}

		encodeTime += stopwatch.ElapsedMilliseconds;

		report.encode_time_ms = encodeTime;
		report.binary_size = binarySize;
		report.average_quality = (float)summedQuality / (numPatchesX * numPatchesY);

		yield return sendPatches(patches);
	}

	private bool shouldProcessFrame()
	{
		if (estimatedAngularSpeed > angularSpeedLimit)
			return false;

		if (Camera.main.velocity.magnitude > speedLimit)
			return false;

		if (transmitMode == TransmitMode.Continuous)
			return true;

		// Other than Continuous mode, we want to limit how often we send frames to the server.
		long interval = GetTimestamp() - lastSendTime;
		if (interval < minSendInterval)
			return false;

		return true;
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

		// Selective mode tries to avoid sending redundant photos, which are either poor quality images
		// or too similiar to the previously-sent image. We check a few things: whether at least one
		// object class was detected, whether the set of detected objects differs from the previously sent
		// image, and whether the time since the previous image is too long.
		if (transmitMode == TransmitMode.Selective && dresult.detected_classes.Count > 0)
		{
			if (!dresult.detected_classes.SetEquals(lastSendDetectionSet))
				return true;

			if (interval > maxSendInterval)
				return true;
		}

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

	private DetectionResult postprocessCoarseSegmentation(Texture2D texture)
    {
		DetectionResult dresult = new DetectionResult();

		var output = engine.PeekOutput("output") as TensorFloat;
		output.MakeReadable();

		float min_score = 1000.0f;

#if UNITY_EDITOR
		Debug.Log($"Scores shape {output.shape.ToString()}");
#endif

		for (int i = 0; i < output.shape[2]; i++)
        {
			for (int j = 0; j < output.shape[3]; j++)
            {
				if (output[0, 0, i, j] > dresult.max_score)
                {
					dresult.max_score = output[0, 0, i, j];
					dresult.max_label = "person";
                }

				if (output[0, 0, i, j] < min_score)
					min_score = output[0, 0, i, j];

				if (output[0, 0, i, j] > detectionThreshold)
                {
					dresult.num_detected = 1;
					dresult.num_persons = 1;
                }
            }
        }

#if UNITY_EDITOR
		Debug.Log($"Max score {dresult.max_score}, min score {min_score}");
#endif

		return dresult;
	}

	private void OnDestroy()
	{
		engine?.Dispose();
		outputTexture?.Release();
		scaledTexture?.Release();
	}

    private void Update()
    {
		long currentTime = GetTimestamp();
		if (currentTime > lastOrientationTime)
        {
			// Roughly estimate the angular rotation speed of the camera.
			// We will avoid processing frames when the camera is moving too much.
			Quaternion currentOrientation = Camera.main.transform.rotation;
			float diff = Quaternion.Angle(lastOrientation, currentOrientation);
			estimatedAngularSpeed = 1000.0f * diff / (currentTime - lastOrientationTime);
			lastOrientation = currentOrientation;
			lastOrientationTime = currentTime;
        }
    }

    private IEnumerator sendStartUpReport()
	{
		var report = new StartUpReport();
		report.model = modelName;
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
		www.SetRequestHeader("X-Queue-Name", uploadQueueName);

		www.uploadHandler = new UploadHandlerRaw(data);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result == UnityWebRequest.Result.Success)
		{
			var created_photo = JsonUtility.FromJson<NewPhotoResult>(www.downloadHandler.text);
			current_filename = created_photo.filename;
			current_photo_id = created_photo.id;
		}
		else
		{
			current_filename = "";
			current_photo_id = -1;
		}

		www.Dispose();
	}

	IEnumerator waitAndDisplayResult()
	{
		string url = EasyVizARServer.Instance.GetBaseURL() + $"/photos/{current_photo_id}?wait=5";

		UnityWebRequest www = new UnityWebRequest(url, "GET");

		www.SetRequestHeader("Authorization", EasyVizARServer.Instance.GetAuthorizationHeader());
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		string detectionResult = "";

		if (www.result == UnityWebRequest.Result.Success)
		{
			Debug.Log(www.downloadHandler.text);
			var photo = JsonUtility.FromJson<EasyVizAR.PhotoInfo>(www.downloadHandler.text);
			if (photo is not null && photo.annotations is not null)
			{
				foreach (var annotation in photo.annotations)
				{
					if (annotation.label == "face" && headAttachedText is not null)
					{
						detectionResult = annotation.sublabel;
						break;
					}
				}
			}
		}

		if (headAttachedText)
			headAttachedText.EnqueueMessage(detectionResult, 5.0f);

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
