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
	
	List<EasyVizARHeadset> _activeHeadsets = new List<EasyVizARHeadset>();
	
	bool _headsetsCreated = false;
	
	[SerializeField]
	Material _localMaterial;
	
	[SerializeField]
	bool _visualizePreviousLocal = false;
	
    // Start is called before the first frame update
    void Start()
    {
		//CreateAllHeadsets();
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
		
	}
	
	public void CreateAllHeadsets()
	{
		if(!_headsetsCreated)
		{
			CreateLocalHeadset();
			CreateHeadsets();
			
			_headsetsCreated = true;
		}
	}
	
	void CreateLocalHeadset()
	{
		if(!_visualizePreviousLocal)
		{
			GameObject localHeadset = Instantiate(_headsetPrefab, transform);
			if(localHeadset != null)
			{
				EasyVizARHeadset h = localHeadset.GetComponent<EasyVizARHeadset>();
				if(_localMaterial != null)
				{
					localHeadset.GetComponent<MeshRenderer>().material = _localMaterial;
				}
				
				h.CreateLocalHeadset(_localHeadsetName, _locationId, !_visualizePreviousLocal);
			}
		}
	}
	
	void CreateHeadsetsCallback(string resultData)
	{
		Debug.Log(resultData);
		
		if(resultData != "error" && resultData.Length > 2)
		{
			//parse list of headsets for this location and create...
			//the key to parsing the array - the text we add here has to match the name of the variable in the array wrapper class (headsets).
			EasyVizAR.HeadsetList h = JsonUtility.FromJson<EasyVizAR.HeadsetList>("{\"headsets\":" + resultData + "}");
			for(int i = 0; i < h.headsets.Length; ++i)
			{
				if(h.headsets[i].name != _localHeadsetName || _visualizePreviousLocal)
				{
					GameObject s = Instantiate(_headsetPrefab, transform);
					EasyVizARHeadset hs = s.GetComponent<EasyVizARHeadset>();
					if(hs != null)
					{
						s.name = h.headsets[i].name;
						hs.AssignValuesFromJson(h.headsets[i]);
						_activeHeadsets.Add(hs);
					}
				}
				else if(h.headsets[i].name == _localHeadsetName && _visualizePreviousLocal)
				{
					GameObject s = Instantiate(_headsetPrefab, transform);
					EasyVizARHeadset hs = s.GetComponent<EasyVizARHeadset>();
					if(hs != null)
					{
						s.name = h.headsets[i].name;
						if(_localMaterial != null)
						{
							s.GetComponent<MeshRenderer>().material = _localMaterial;
						}
						hs.AssignValuesFromJson(h.headsets[i]);
						hs.IsLocal = true;
						hs.LocationID = h.headsets[i].location_id;
						_activeHeadsets.Add(hs);
					}
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
