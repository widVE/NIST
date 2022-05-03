using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;

public class EasyVizARHeadsetManager : MonoBehaviour
{
	[SerializeField]
	string _locationId = "none";
	public string LocationID => _locationId;
	
	[SerializeField]
	GameObject _headsetPrefab;	//prefab for loading other headsets...
	
	[SerializeField]
	string _localHeadsetName;
	public string LocalHeadsetName => _localHeadsetName;
	
	[SerializeField]
	QRTracking.QRCodesVisualizer _qrCodes;
	
	//the transform of this game object could serve as the common coordinate space for all headsets (i.e. make them as children to this object)
	//and use QR code detection to set this object's transform.
	
	List<EasyVizARHeadset> _activeHeadsets = new List<EasyVizARHeadset>();
	
    // Start is called before the first frame update
    void Start()
    {
		//CreateLocalHeadset();
		//CreateHeadsets();
		//QRTracking.QRCodesManager.Instance.QRCodeAdded += QRCodeDetected;
    }

    // Update is called once per frame
    void Update()
    {

    }
	
	void OnEnable()
	{
		
	}
	
	void OnDisable()
	{
		//QRTracking.QRCodesManager.Instance.QRCodeAdded -= QRCodeDetected;
	}
	
	private void QRCodeDetected(object sender, QRTracking.QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
	{
		System.Guid guid = e.Data.Id;
		GameObject g = _qrCodes.GetQRCodeGameObjectForID(guid);
		if(g != null)
		{
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeDetected.txt"), "Detected QRCode at " + g.transform.position.ToString("F4"));
			transform.SetParent(g.transform.parent);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "qrCodeDetected2.txt"), "Our new tranform: " + transform.position.ToString("F4"));
		}
	}
	
	void CreateLocalHeadset()
	{
		GameObject localHeadset = Instantiate(_headsetPrefab, transform);
		if(localHeadset != null)
		{
			EasyVizARHeadset h = localHeadset.GetComponent<EasyVizARHeadset>();
			h.CreateLocalHeadset(_localHeadsetName);
		}
	}
	
	void CreateHeadsetsCallback(string resultData)
	{
		Debug.Log(resultData);
		
		if(resultData != "error" && resultData.Length > 2)
		{
			//parse list of headsets for this location and create...
			EasyVizAR.HeadsetList h = JsonUtility.FromJson<EasyVizAR.HeadsetList>(resultData);
			for(int i = 0; i < h.headsets.Length; ++i)
			{
				GameObject s = Instantiate(_headsetPrefab);
				EasyVizARHeadset hs = s.GetComponent<EasyVizARHeadset>();
				if(hs != null)
				{
					hs.AssignValuesFromJson(h.headsets[i]);
					_activeHeadsets.Add(hs);
				}
			}
		}
	}
	
	void CreateHeadsets()
	{
		//list headsets from server for our location, create a prefab of each...
		EasyVizARServer.Instance.Get("headsets?location_id="+_locationId, EasyVizARServer.JSON_TYPE, CreateHeadsetsCallback);
	}
}
