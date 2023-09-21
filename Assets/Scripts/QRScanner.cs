using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit.Utilities;

// Prevent compiler errors when developing and testing on non-Windows machines.
#if (UNITY_EDITOR_WIN || UNITY_WSA || WINDOWS_UWP)
using Microsoft.Windows.Perception.Spatial;
using Microsoft.Windows.Perception.Spatial.Preview;
#endif

public class LocationChangedEventArgs
{
	public string Server;
	public string LocationID;
}

public class QRTransformChangedEventArgs
{
	public Matrix4x4 NewTransform;
	public Guid spatialNodeId;
}

[RequireComponent(typeof(AudioSource))]
public class QRScanner : MonoBehaviour
{
	struct QRData
	{ 
		public Pose pose;
		public float  size;
		public string text;
		public DateTimeOffset lastDetected;
		public Guid spatialGraphNodeId;

		public static QRData FromCode(QRCode qr)
		{
			QRData result = new QRData();

			SpatialGraphNode node = SpatialGraphNode.FromStaticNodeId(qr.SpatialGraphNodeId);

			if (node != null && node.TryLocate(FrameTime.OnUpdate, out result.pose))
			{
				/*if (CameraCache.Main.transform.parent != null)
                {
                    result.pose = result.pose.GetTransformedBy(CameraCache.Main.transform.parent);
                }*/
				
				result.spatialGraphNodeId = qr.SpatialGraphNodeId;
				
				result.pose.rotation *= Quaternion.Euler(90, 0, 0);

				// Move the anchor point to the *center* of the QR code
				var deltaToCenter = qr.PhysicalSideLength * 0.5f;
				result.pose.position += (result.pose.rotation * (deltaToCenter * Vector3.right) -
								  result.pose.rotation * (deltaToCenter * Vector3.forward));

				//System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeFound.txt"), "Detected QRCode at " + result.pose.position.ToString("F4"));
			}

			result.lastDetected = qr.LastDetectedTime;
			result.size = qr.PhysicalSideLength;
			result.text = qr.Data == null ? "" : qr.Data;
			Debug.Log("Loaded QR Data: " + result.text);
			return result;
		}
	}

	AudioSource audioSource;

	QRCodeWatcher watcher = null;
	
	[SerializeField]
	GameObject _qrPrefab;

	[SerializeField]
	GameObject _qrPrefabParent;

	[SerializeField]
	GameObject _headsetManager;

	[SerializeField]
	GameObject _meshCapture;

	[SerializeField]
	GameObject _photoCapture;

	[SerializeField]
	GameObject _researchModeManager;

	[SerializeField]
	[Tooltip("Whether we should continue to adjust the world coordinate system if the currently tracked QR code moves.")]
	bool followMovingQRCode = false;
	
	[SerializeField]
	[Tooltip("In scanning mode, we don't re-add QR codes from the Hololens 2 system memory, and we only update the QR code Axis once.")]
	bool _scanningMode = false;
	
	bool _updatedServerFromQR = false;
	string _currentLocationID = null;

	/*
	 * Simulate QR code detection through the Unity editor by setting the data field
	 * and clicking the trigger check box which acts as a fake button.
	 */
	[Header("Manual Trigger")]
	[Tooltip("Set the QR code data and click the checkbox to trigger a fake QR code detection.")]
	public string testQRCodeData = "vizar://easyvizar.wings.cs.wisc.edu:5000/locations/66a4e9f2-e978-4405-988e-e168a9429030";
	public bool triggerQRCodeDetected = false;

	/*
	 * Keep track of specially-formatted QR codes that serve as location origins.
	 * We can change location by scanning a new origin code, but that does need to
	 * trigger changes in several modules (headset list, feature list, websocket connection).
	 */
	Dictionary<Guid, QRData> originCodes = new Dictionary<Guid, QRData>();
	Guid currentOriginId = Guid.Empty;
	DateTimeOffset lastDetectedTime = DateTimeOffset.Now;
	bool isOriginChanging = false;
	bool isCoordinateSystemChanging = false;

	/// Initialization is just a matter of asking for permission, and then
	/// hooking up to the `QRCodeWatcher`'s events. `QRCodeWatcher.RequestAccessAsync`
	/// is an async call, so you could re-arrange this code to be non-blocking!
	async void Start()
	{
		audioSource = GetComponent<AudioSource>();

		if (!QRCodeWatcher.IsSupported())
        {
			Debug.Log("QR code tracking is not supported");
			return;
        }

		/*
		 * The first time this runs, RequestAccessAsync will probably return Denied
		 * before the user has a chance to click the button to allow camera access.
		 * It can be fixed by restarting the app, but it seems we should wait somehow
		 * for RequestAccessStatus to return Allowed before proceeding.
		 * 
		 * I think this is related to the discussion here:
		 * https://github.com/microsoft/MixedReality-WorldLockingTools-Samples/issues/20
		 */
		var access = await QRCodeWatcher.RequestAccessAsync();
		if (access != QRCodeWatcherAccessStatus.Allowed)
		{
			Debug.Log("QRCodeWatcher access was denied");
			return;
		}

		// Set up the watcher, and listen for QR code events.
		watcher = new QRCodeWatcher();

		watcher.Updated += (sender, e) =>
		{
			CodeAddedOrUpdated(e.Code);
		};
		
		if(!_scanningMode)
		{
			watcher.Added += (sender, e) =>
			{
				CodeAddedOrUpdated(e.Code);
			};
		}
		
		//watcher.Removed += (sender, e) => { };

		watcher.Start();
	}

	private void CodeAddedOrUpdated(QRCode qr)
    {
		//check timestamps here...
		//qrCode.LastDetectedTime;
		bool isVizarScheme = false;
		bool isLocation = false;

		if (Uri.TryCreate(qr.Data, UriKind.Absolute, out Uri uri))
        {
			if (uri.Scheme == "vizar")
			{
				// Example: http://easyvizar.wings.cs.wisc.edu:5000/
				string base_url = "http://" + uri.Authority + "/";
				Debug.Log("Detected URL from QR code: " + base_url);
				//EasyVizARServer.Instance._baseURL = base_url;
				//_updatedServerFromQR = true;

				isVizarScheme = true;
			}

			// Expected path segments: "/", "locations/", and "<location-id>"
			if (uri.Segments.Length == 3 && uri.Segments[1] == "locations/")
			{
				string loc = uri.Segments[2];
				Debug.Log("Detected location ID from QR code: " + loc);
				isLocation = true;
			}
		}

		// Example: vizar://easyvizar.wings.cs.wisc.edu:5000/locations/69e92dff-7138-4091-89c4-ed073035bfe6
		// This QR code marks a location origin. Add it to the appropriate dictionary, and we will
		// take care of the rest on the next Update cycle.
		if (isVizarScheme && isLocation)
        {
			var qrdata = QRData.FromCode(qr);

			lock (originCodes)
            {
				originCodes[qr.Id] = qrdata;

				if (qr.Id != currentOriginId && qr.LastDetectedTime > lastDetectedTime)
                {
					currentOriginId = qr.Id;
					lastDetectedTime = qr.LastDetectedTime;
					isOriginChanging = true;
					isCoordinateSystemChanging = true;
                }
				else if(followMovingQRCode && qr.Id == currentOriginId)
                {
					// We may have a better estimate of the QR code location, so trigger an update of the world coordinate system.
					isCoordinateSystemChanging = true;
					if(_scanningMode)
					{
						followMovingQRCode = false;
					}
				}
            }
        }
    }

	void ChangeOriginFromCode(QRData d)
    {
		if (Uri.TryCreate(d.text, UriKind.Absolute, out Uri uri))
		{
			if (uri.Scheme == "vizar")
			{
				// Example: http://easyvizar.wings.cs.wisc.edu:5000/
				string base_url = "http://" + uri.Authority + "/";
				Debug.Log("Detected URL from QR code: " + base_url);
				EasyVizARServer.Instance.SetBaseURL(base_url);
				_updatedServerFromQR = true;
			}
			else
			{
				// Ignore QR codes without the vizar scheme.
				return;
			}

			// Expected path segments: "/", "locations/", and "<location-id>"
			if (uri.Segments.Length == 3 && uri.Segments[1] == "locations/")
			{
				/* 
				 * Added this redundant logic to solve a race condition with the MeshCapture script,
				 * which would start sending data with the new location ID but unaligned coordinate system.
				 * Ideally, we want to set the coordinate system and location ID as an atomic operation.
				 */
				if (isCoordinateSystemChanging)
				{
					ChangeCoordinateSystemFromCode(d);
					isCoordinateSystemChanging = false;
				}

				string loc = uri.Segments[2];
				Debug.Log("Detected location ID from QR code: " + loc);
				if (loc != _currentLocationID)
				{
					LocationChangedEventArgs args = new LocationChangedEventArgs();
					args.Server = uri.Authority; // Authority gives host:port
					args.LocationID = loc;

					if (LocationChanged is not null)
					{
						LocationChanged(this, args);
					}

					_currentLocationID = loc;

					// Fetch location information from the server and apply any applicable configuration.
					EasyVizARServer.Instance.Get("/locations/" + loc, EasyVizARServer.JSON_TYPE, delegate (string result)
					{
						var location = JsonUtility.FromJson<EasyVizAR.Location>(result);
						ApplyLocationConfiguration(location);
					});
				}
			}
		}

		//Calls into the manager when the QR code is detected. Spawns headsets for this location ID
		if (_headsetManager is not null)
        {
			var manager = _headsetManager.GetComponent<EasyVizARHeadsetManager>();
			manager.CreateAllHeadsets();
		}
	}

	// Enable or disable certain features based on the configuration values from the server.
	void ApplyLocationConfiguration(EasyVizAR.Location location)
    {
		if (_meshCapture)
        {
			_meshCapture.SetActive(location.headset_configuration.enable_mesh_capture);
        }

		if (_photoCapture)
        {
			var script = _photoCapture.GetComponent<TakeColorPhoto>();
			if (location.headset_configuration.enable_photo_capture)
            {
				script.BeginContinuousCapture();
            }
            else
            {
				script.EndContinuousCapture();
            }
        }

        if (_researchModeManager) {
            var script = _researchModeManager.GetComponent<HololensDepthPVCapture>();
            if (location.headset_configuration.enable_extended_capture)
            {
                script.RunSensors();
            }
            else
            {
                script.StopSensorsEvent();
            }
        }
    }

	void ChangeCoordinateSystemFromCode(QRData d)
    {
		if (_qrPrefab != null && _qrPrefabParent != null)
		{
			_qrPrefabParent.SetActive(true);
			_qrPrefab.SetActive(true);

			Matrix4x4 m = Matrix4x4.TRS(d.pose.position, d.pose.rotation, Vector3.one);
			Matrix4x4 mInv = m.inverse;
			
			QRTransformChangedEventArgs args = new QRTransformChangedEventArgs();
			//args.NewTransform = new Matrix4x4();
			args.NewTransform = mInv;
			args.spatialNodeId = d.spatialGraphNodeId;
			
			if(QRTransformChanged is not null)
			{
				QRTransformChanged(this, args);
			}
			
			_qrPrefabParent.transform.SetPositionAndRotation(mInv.GetPosition(), mInv.rotation);

			_qrPrefab.transform.localPosition = d.pose.position;
			_qrPrefab.transform.localRotation = d.pose.rotation;

			//int cc = _qrPrefab.transform.childCount;
		}
	}

	// For shutdown, we just need to stop the watcher
	void OnDestroy()
	{
		Debug.Log("Stopping QR Watcher");

		if (watcher is not null)
        {
			watcher.Stop();
			watcher = null;
        }
	}

	void Update()
	{
		if (isOriginChanging)
        {
			audioSource.Play();
			ChangeOriginFromCode(originCodes[currentOriginId]);
			isOriginChanging = false;
        }

		if (isCoordinateSystemChanging)
        {
			ChangeCoordinateSystemFromCode(originCodes[currentOriginId]);
			isCoordinateSystemChanging = false;
        }
	}

	//This is getting called on play mode exit, but it doesn't seem to be enough to stop the crashes
	// the crashes have stopped, but it takes a surprisingly long time for this to stop, sometimes over a minute
	void OnApplicationQuit()
	{
		Debug.Log("Stopping QR Watcher");

		if (watcher is not null)
        {
			watcher.Stop();
			watcher = null;
        }

		Debug.Log("Application ending after " + Time.time + " seconds");
	}

	void OnValidate()
    {
		if (triggerQRCodeDetected)
        {
			var code = new QRData();
			code.text = testQRCodeData;
			code.size = 1.0f;
			code.pose = Pose.identity;

			var guid = Guid.NewGuid();
			
			lock (originCodes)
            {
				originCodes[guid] = code;
				currentOriginId = guid;
				lastDetectedTime = DateTimeOffset.Now;
				isOriginChanging = true;
			}

			triggerQRCodeDetected = false;
        }
    }

	public event EventHandler<LocationChangedEventArgs> LocationChanged;
	
	public event EventHandler<QRTransformChangedEventArgs> QRTransformChanged;
}
