using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Microsoft.MixedReality.QR;
using Microsoft.Windows.Perception.Spatial;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit.Utilities;

public class LocationChangedEventArgs
{
	public string Server;
	public string LocationID;
}

public class QRScanner : MonoBehaviour
{
	struct QRData
	{ 
		public Pose pose;
		public float  size;
		public string text;

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
				
				result.pose.rotation *= Quaternion.Euler(90, 0, 0);

				// Move the anchor point to the *center* of the QR code
				var deltaToCenter = qr.PhysicalSideLength * 0.5f;
				result.pose.position += (result.pose.rotation * (deltaToCenter * Vector3.right) -
								  result.pose.rotation * (deltaToCenter * Vector3.forward));

				//System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeFound.txt"), "Detected QRCode at " + result.pose.position.ToString("F4"));
			}
			
			result.size = qr.PhysicalSideLength;
			result.text = qr.Data == null ? "" : qr.Data;
			Debug.Log(result.text);
			return result;
		}
	}
	
	QRCodeWatcher watcher = null;
	DateTime      watcherStart;
	Dictionary<Guid, QRData> poses = new Dictionary<Guid, QRData>();
	
	[SerializeField]
	GameObject _qrPrefab;

	[SerializeField]
	GameObject _qrPrefabParent;
	
	bool _updatedServerFromQR = false;
	string _currentLocationID = null;
	
	/// Initialization is just a matter of asking for permission, and then
	/// hooking up to the `QRCodeWatcher`'s events. `QRCodeWatcher.RequestAccessAsync`
	/// is an async call, so you could re-arrange this code to be non-blocking!
	/// 
	/// You'll also notice there's some code here for filtering out QR codes.
	/// The default behavior for the QR code library is to provide all QR
	/// codes that it knows about, and that includes ones that were found
	/// before the session began. We don't need that, so we're ignoring those.
	async void Start()
	{
		if (!QRCodeWatcher.IsSupported())
        {
			Debug.Log("QR code tracking is not supported");
			return;
        }

		var access = await QRCodeWatcher.RequestAccessAsync();
		if (access != QRCodeWatcherAccessStatus.Allowed)
        {
			Debug.Log("QR code access was denied");
			return;
        }

		// Set up the watcher, and listen for QR code events.
		watcherStart = DateTime.Now;
		watcher = new QRCodeWatcher();
		
		// What does this mean? += (o, qr) =>
		// Answer: add an anonymous callback function to the watcher Added event handler
		watcher.Added   += (o, qr) => {
			// QRCodeWatcher will provide QR codes from before session start,
			// so we often want to filter those out.
			if (qr.Code.LastDetectedTime > watcherStart) 
			{
				AudioSource aSource = GetComponent<AudioSource>();
				if(aSource != null)
				{
					aSource.Play();
				}
				Debug.Log("QR Code: " + qr.Code.Data.ToString());
				Debug.Log("Adding QR Code: " + qr.Code.Id.ToString());
				poses.Add(qr.Code.Id, QRData.FromCode(qr.Code)); 
			}
		};
		
		watcher.Updated += (o, qr) => 
		{
			poses[qr.Code.Id] = QRData.FromCode(qr.Code);
		};
		
		watcher.Removed += (o, qr) => poses.Remove(qr.Code.Id);

		watcher.Start();
				
		Debug.Log("Starting QR Watcher");
	}

	// For shutdown, we just need to stop the watcher
	void DestroyObject()
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
		foreach (QRData d in poses.Values)
		{
			Debug.Log("QR: " + d.text);

			if(!_updatedServerFromQR)
			{
				if (Uri.TryCreate(d.text, UriKind.Absolute, out Uri uri))
                {
					if (uri.Scheme == "vizar")
                    {
						// Example: http://halo05.wings.cs.wisc.edu:5000/
						string base_url = "http://" + uri.Authority + "/";
						Debug.Log("Detected URL from QR code: " + base_url);
						EasyVizARServer.Instance._baseURL = base_url;
						_updatedServerFromQR = true;
					}
					else
                    {
						// Ignore QR codes without the vizar scheme.
						continue;
                    }

					// Expected path segments: "/", "locations/", and "<location-id>"
					if (uri.Segments.Length == 3 && uri.Segments[1] == "locations/")
                    {
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
						}
					}
				}
			}
			
			if(_qrPrefab != null && _qrPrefabParent != null)
			{
				_qrPrefabParent.SetActive(true);
				_qrPrefab.SetActive(true);
				
				Matrix4x4 m = Matrix4x4.TRS(d.pose.position, d.pose.rotation, Vector3.one);
				Matrix4x4 mInv = m.inverse;
				_qrPrefabParent.transform.SetPositionAndRotation(mInv.GetPosition(), mInv.rotation);
				
				_qrPrefab.transform.localPosition = d.pose.position;
				_qrPrefab.transform.localRotation = d.pose.rotation;

				int cc = _qrPrefab.transform.childCount;
				
				_qrPrefab.transform.GetChild(cc-1).GetComponent<EasyVizARHeadsetManager>().CreateAllHeadsets();
			}
		}
	}

	//This is getting called on play mode exit, but it doesn't seem to be enough to stop the crashes
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

	public event EventHandler<LocationChangedEventArgs> LocationChanged;
}
