using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
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
	public GameObject headset_parent; // This will get populated by EasyVizARHeadsetManager.cs
    public string local_headset_id = "";
	public bool isLocal = false;
	
	// This is for navigation
	public GameObject feature_parent;
    public LineRenderer line;
    EasyVizAR.Path path = new EasyVizAR.Path(); // this stores the path of points 

    private EasyVizAR.NavigationTarget currentTarget = new EasyVizAR.NavigationTarget();


    // Start is called before the first frame update
    void Start()
    {
		currentTarget.type = "none";

		_lastTime = UnityEngine.Time.time;
        line = GameObject.Find("Main Camera").GetComponent<LineRenderer>();

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

			DistanceCalculation d_s = this.GetComponent<DistanceCalculation>();

			// Either reload our existing headset from the server or create a new one.
			if (EasyVizARServer.Instance.TryGetHeadsetID(out string headsetId))
            {
				UnityEngine.Debug.Log("Reloading headset: " + headsetId);
                UnityEngine.Debug.Log("This is the local headset: " + headsetId);
				local_headset_id = headsetId;
				isLocal= true;
				d_s.isLocal = true;
                LoadHeadset(headsetId);
            } else
            {
				UnityEngine.Debug.Log("Creating headset...");
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

		
		// Not sure if this is the right area to do the navigation
        if (h.navigation_target.type != currentTarget.type || h.navigation_target.target_id != currentTarget.target_id) {
            currentTarget = h.navigation_target;
			if (currentTarget.type == "feature" || currentTarget.type == "headset" || currentTarget.type == "point")
			{
				FindPath(h.navigation_target.position);
			}
			else
            {
				// If target type is none, we should clear the navigation path,
				// but there is no need to call the server for pathing.
				line.positionCount = 0;
            }
            
        }
        Color newColor;
		if (ColorUtility.TryParseHtmlString(h.color, out newColor))
			_color = newColor; // this is where the color of the headset is assigned --> this field is populated 

        Transform cur_headset = headset_parent.transform.Find(h.id);
        if (cur_headset)
        {
			cur_headset.Find("Capsule").GetComponent<Renderer>().material.color = newColor;

        }
        else
		{
            UnityEngine.Debug.Log("the cur_headset is not found");

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

			UnityEngine.Debug.Log("Successfully connected headset: " + h.name);
		}
		else
		{
			UnityEngine.Debug.Log("Received an error when creating headset");
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

			// We should load the name and color information from the server, but not the location ID, which may be out of date.
			// Instead, since we probably just scanned a QR code, we should inform the server of our new location by
			// sending a check-in.
			_headsetID = h.id;
			_headsetName = h.name;

			Color newColor;
			if (ColorUtility.TryParseHtmlString(h.color, out newColor))
				_color = newColor;

			UnityEngine.Debug.Log("Successfully connected headset: " + h.name);

			CreateCheckIn(h.id, _locationID);
		}
		else
		{
			// If loading fails, make a new headset.
			CreateHeadset();
		}
	}

	public void CreateCheckIn(string headsetId, string locationId)
	{
		var checkIn = new EasyVizAR.NewCheckIn();
		checkIn.location_id = locationId;

		EasyVizARServer.Instance.Post($"/headsets/{headsetId}/check-ins", EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(checkIn), delegate (string result)
		{
			// Not much to do after check-in was created.
		});
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
			UnityEngine.Debug.Log("Received an error when obtaining past positions");
		}
	}
	
	void GetPastPositions()
	{
		EasyVizARServer.Instance.Get("headsets/"+_headsetID+"/pose-changes", EasyVizARServer.JSON_TYPE, GetPastPositionsCallback);
	}

    // Querying the server with path between two points
    public void FindPath(EasyVizAR.Position target) // Vector3 start, Vector3 target
    {
        
            UnityEngine.Debug.Log("initiated querying path");
            Vector3 start = GameObject.Find("Main Camera").transform.position;

        //UnityEngine.Debug.Log("http://easyvizar.wings.cs.wisc.edu:5000/locations/" + location_id + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + target.x + "," + target.y + "," + target.z);
        EasyVizARServer.Instance.Get("locations/" + _locationID + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + target.x + "," + target.y + "," + target.z, EasyVizARServer.JSON_TYPE, GetPathCallback);

        
    }

    void GetPathCallback(string result)
    {
        //Debug.Log("initiated querying path");
        //Debug.Log("the result: " + result);
        if (result != "error")
        {
            UnityEngine.Debug.Log("path callback: " + result);
            path = JsonUtility.FromJson<EasyVizAR.Path>("{\"points\":" + result + "}");
            int cnt = 0;
            // making sure we are creating a new line
            if (line.positionCount > 0) line.positionCount = 0;
            EasyVizAR.Position target_pos = new EasyVizAR.Position();
			foreach (EasyVizAR.Position points in path.points)
            {
                line.positionCount++;
                line.SetPosition(cnt++, new Vector3(points.x, points.y, points.z)); // this draws the line 
                //UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
                //UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);
                target_pos = points;
            }
            UnityEngine.Debug.Log("Successfully added the points");

        }

    }
}
