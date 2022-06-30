using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using Microsoft.MixedReality.QR;
using Microsoft.Windows.Perception.Spatial;
using Microsoft.MixedReality.OpenXR;
using Microsoft.MixedReality.Toolkit.Utilities;

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

	
	QRCodeWatcher watcher;
	DateTime      watcherStart;
	Dictionary<Guid, QRData> poses = new Dictionary<Guid, QRData>();
	
	[SerializeField]
	GameObject _qrPrefab;

	[SerializeField]
	GameObject _qrPrefabParent;
	
	bool _updatedServerFromQR = false;
	
	/// Initialization is just a matter of asking for permission, and then
	/// hooking up to the `QRCodeWatcher`'s events. `QRCodeWatcher.RequestAccessAsync`
	/// is an async call, so you could re-arrange this code to be non-blocking!
	/// 
	/// You'll also notice there's some code here for filtering out QR codes.
	/// The default behavior for the QR code library is to provide all QR
	/// codes that it knows about, and that includes ones that were found
	/// before the session began. We don't need that, so we're ignoring those.
	void Start()
	{
#if UNITY_EDITOR

		if(!UnityEditor.SessionState.GetBool("FirstInitDone", false))
		{
			Debug.Log("Request Async Editor");
			UnityEditor.SessionState.SetBool("FirstInitDone", true);
			
			var status = QRCodeWatcher.RequestAccessAsync().Result;
			
			if (status != QRCodeWatcherAccessStatus.Allowed)
				return;
		}
#else
		// Ask for permission to use the QR code tracking system
		
		//THE CRASH STARTS HERE, When this block is commented out you can
		//reuse play mode witout crash. I just ended up disabling the entire
		//QR watcher game object and that also fixes it. But this is what
		//I tracked it down to. I couldn't get the watcher to stop with
		//on application end, but if we shut down this async request there 
		//that might do it.
		var status = QRCodeWatcher.RequestAccessAsync().Result;
		if (status != QRCodeWatcherAccessStatus.Allowed)
			return;
#endif
		
		// Set up the watcher, and listen for QR code events.
		watcherStart = DateTime.Now;
		watcher      = new QRCodeWatcher();


		// What does this mean? += (o, qr) =>
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
	void OnDestroy()
	{
		Debug.Log("Stopping QR Watcher");
		// Why is there a question mark here?
		watcher?.Stop();
	}

	void Update()
	{
		foreach(QRData d in poses.Values)
		{ 
			if(!_updatedServerFromQR)
			{
				//parse server and location from here...
				int p = d.text.LastIndexOf("/");
				if(p != -1)
				{
					int l = d.text.Length;
					string serverString = d.text.Substring(0, p);
					int p2 = serverString.LastIndexOf("/");
					string url = serverString.Substring(0, p2+1);
					url = url.Replace("vizar", "http");
					EasyVizARServer.Instance._baseURL = url;
					Debug.Log(EasyVizARServer.Instance._baseURL);
					string loc = d.text.Substring(p+1, l-p-1);
					Debug.Log(loc);
					if(_qrPrefab != null)
					{
						int cc = _qrPrefab.transform.childCount;
						_qrPrefab.transform.GetChild(cc-1).GetComponent<EasyVizARHeadsetManager>().LocationID = loc;
					}	
				}
				_updatedServerFromQR = true;
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
		watcher.Stop();
		Debug.Log("Application ending after " + Time.time + " seconds");
	}
}