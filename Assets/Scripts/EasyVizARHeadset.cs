using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyVizARHeadset : MonoBehaviour
{
	[SerializeField]
	float _updateFrequency;
	
	[SerializeField]
	string _headsetName;
	public string Name => _headsetName;
	
	[SerializeField]
	bool _isLocal = false;
	public bool IsLocal
	{
		get { return _isLocal; }
		set { _isLocal = value; }
	}
	
	[SerializeField]
	bool _showPositionChanges = false;
	public bool ShowPositionChanges => _showPositionChanges;
	
	[SerializeField]
	bool _realTimeChanges = false;
	public bool RealTimeChanges => _realTimeChanges;
	
	[SerializeField]
	bool _postPositionChanges = false;
	public bool PostPositionChanges => _postPositionChanges;
	
	public string _headsetID;
	public Color _color = Color.red;
	
	public string _locationID;// will change this back to just string w/o public
	
	public string LocationID
	{
		get { return _locationID; }
		set { _locationID = value; }
	}
	
	float _lastTime;
	
	bool _isRegisteredWithServer = false;
	
	//if local, we set this to the MainCamera (the Hololens 2's camera)
	Camera _mainCamera;
	public GameObject map_parent; // This will get populated by EasyVizARHeadsetManager.cs
	public GameObject headset_parent;
	
    // Start is called before the first frame update
    void Start()
    {
		_lastTime = UnityEngine.Time.time;
		//Debug.Log("In start");
		//CreateHeadset();
		//RegisterHeadset();

    }

	public void CreateLocalHeadset(string headsetName, string location, bool postChanges)
	{
		_isLocal = true;
		_mainCamera = Camera.main;
		_headsetName = headsetName;
		_locationID = location;
		
		if(postChanges)
		{
			_realTimeChanges = true;

			// Either reload our existing headset from the server or create a new one.
			if (EasyVizARServer.Instance.TryGetHeadsetID(out string headsetId))
            {
				Debug.Log("Reloading headset: " + headsetId);
				LoadHeadset(headsetId);
            } else
            {
				Debug.Log("Creating headset...");
				CreateHeadset();
            }
		}
		
		_postPositionChanges = postChanges;
	}
	
    // Update is called once per frame
    void Update()
    {
		if(_isLocal)
		{
			if(_mainCamera && _postPositionChanges)
			{
				transform.position = _mainCamera.transform.position;
				transform.rotation = _mainCamera.transform.rotation;
			}
			
			float t = UnityEngine.Time.time;
			if(t - _lastTime > _updateFrequency)
			{
				if(_isRegisteredWithServer && _postPositionChanges)
				{
					PostPosition();
				}
				_lastTime = t;
				
				if(_realTimeChanges && _showPositionChanges)
				{
					GetPastPositions();
				}
			}
		}
    }
	
	public void AssignValuesFromJson(EasyVizAR.Headset h)
	{
		Vector3 newPos = Vector3.zero;

		newPos.x = h.position.x;
		newPos.y = h.position.y;
		newPos.z = h.position.z;	
		
		transform.position = newPos;
		transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);

		_headsetID = h.id;
		_headsetName = h.name;
		_locationID = h.location_id;

		Color newColor;
		if (ColorUtility.TryParseHtmlString(h.color, out newColor))
			_color = newColor;
		
		Transform headset_icon = map_parent.transform.Find(h.id);
		Transform cur_headset = headset_parent.transform.Find(h.id);
		if (headset_icon)
		{
			headset_icon.Find("Quad").GetComponent<Renderer>().material.SetColor("_EmissionColor", newColor);
		}
		if (cur_headset)
        {
			cur_headset.Find("Capsule").GetComponent<Renderer>().material.color = newColor;
		}


		if (_showPositionChanges)
		{
			GetPastPositions();
		}
	}
	
	void RegisterHeadset()
	{
		//register the headset with the server, first checking if it exists there already or not...
		EasyVizARServer.Instance.Get("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, RegisterCallback);
	}
	
	void RegisterCallback(string resultData)
	{
		if(resultData != "error")
		{
			//Debug.Log(resultData);
			EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);
			//fill in any local data here from the server...
			AssignValuesFromJson(h);
		}
		else
		{
			//headset doesn't yet exist, let's create a new one...
			CreateHeadset();
		}
	}
	
	void CreateHeadset()
	{
		EasyVizAR.Headset h = new EasyVizAR.Headset();
		h.position = new EasyVizAR.Position();
		h.position.x = transform.position.x;
		h.position.y = transform.position.y;
		h.position.z = transform.position.z;
		h.orientation = new EasyVizAR.Orientation();
		h.orientation.x = transform.rotation[0];
		h.orientation.y = transform.rotation[1];
		h.orientation.z = transform.rotation[2];
		h.orientation.w = transform.rotation[3];
		
		h.name = _headsetName;
		h.location_id = _locationID;
		
		EasyVizARServer.Instance.Post("headsets", EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), CreateCallback);
	}
	
	void CreateCallback(string resultData)
	{
		if(resultData != "error")
		{
			_isRegisteredWithServer = true;
			
			EasyVizAR.RegisteredHeadset h = JsonUtility.FromJson<EasyVizAR.RegisteredHeadset>(resultData);
			Vector3 newPos = Vector3.zero;

			newPos.x = h.position.x;
			newPos.y = h.position.y;
			newPos.z = h.position.z;	
			
			transform.position = newPos;
			transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);
			
			_headsetID = h.id;
			_headsetName = h.name;
			_locationID = h.location_id;

			Color newColor;
			if (ColorUtility.TryParseHtmlString(h.color, out newColor))
				_color = newColor;

			EasyVizARServer.Instance.SaveRegistration(h.id, h.token);

			Debug.Log("Successfully connected headset: " + h.name);
		}
		else
		{
			Debug.Log("Received an error when creating headset");
		}
	}

	void LoadHeadset(string headsetId)
	{
		EasyVizARServer.Instance.Get("headsets/" + headsetId, EasyVizARServer.JSON_TYPE, LoadHeadsetCallback);
	}

	void LoadHeadsetCallback(string resultData)
	{
		if (resultData != "error")
		{
			_isRegisteredWithServer = true;

			EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);
			Vector3 newPos = Vector3.zero;

			newPos.x = h.position.x;
			newPos.y = h.position.y;
			newPos.z = h.position.z;

			transform.position = newPos;
			transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);

			_headsetID = h.id;
			_headsetName = h.name;
			_locationID = h.location_id;

			Color newColor;
			if (ColorUtility.TryParseHtmlString(h.color, out newColor))
				_color = newColor;

			Debug.Log("Successfully connected headset: " + h.name);
		}
		else
		{
			// If loading fails, make a new headset.
			CreateHeadset();
		}
	}

	void PostPosition()
	{
		EasyVizAR.HeadsetPositionUpdate h = new EasyVizAR.HeadsetPositionUpdate();
		h.position = new EasyVizAR.Position();
		h.position.x = transform.position.x;
		h.position.y = transform.position.y;
		h.position.z = transform.position.z;
		h.orientation = new EasyVizAR.Orientation();
		h.orientation.x = transform.rotation[0];
		h.orientation.y = transform.rotation[1];
		h.orientation.z = transform.rotation[2];
		h.orientation.w = transform.rotation[3];
		h.location_id = _locationID;
		
		EasyVizARServer.Instance.Patch("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), PostPositionCallback);
	}
	
	
	void PostPositionCallback(string resultData)
	{
		//Debug.Log(resultData);
		
		if(resultData != "error")
		{
			
		}
		else
		{
			
		}
	}
	
/*
	Line Renderer Method and Post parsing?

	What is a callback for here? Is this part of the visualization or is this related to the parsing infomation
	The line/trail rederer doesn't move the lines once they've been made, even if the root is offset. They need
	to be redrawn from the path. We may want to seperate the parsing of the positions from the visulaization 
	because the only way we've figured out how to update the whole trail is by replaying the past position with the
	new offset included.
 */
	void GetPastPositionsCallback(string resultData)
	{
		if(resultData != "error")
		{
			EasyVizAR.PoseChanges p = JsonUtility.FromJson<EasyVizAR.PoseChanges>("{\"poseChanges\":" + resultData + "}");
			
			//either make a line renderer or trail renderer, etc. with these positions / orientations
			LineRenderer r = transform.GetChild(0).GetComponent<LineRenderer>();
			r.positionCount = p.poseChanges.Length;
			
			for(int i = 0; i < p.poseChanges.Length; ++i)
			{
				// X is negated here to offset the coordinate mismatch between the server and Unity
				Vector3 vPos = Vector3.zero;
				vPos.x = -p.poseChanges[i].position.x;
				vPos.y = p.poseChanges[i].position.y;
				vPos.z = p.poseChanges[i].position.z;
				r.SetPosition(i, vPos);
			}
		}
		else
		{
			Debug.Log("Received an error when obtaining past positions");
		}
	}
	
	void GetPastPositions()
	{
		EasyVizARServer.Instance.Get("headsets/"+_headsetID+"/pose-changes", EasyVizARServer.JSON_TYPE, GetPastPositionsCallback);
	}
}
