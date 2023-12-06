using EasyVizAR;
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
	double _updateFrequency;
	
	[SerializeField]
	string _headsetName;
	public string Name
	{
		get { return _headsetName;}
		set { _headsetName = value; }
	}
	
	public bool verbose_debug = false;

	[SerializeField]
	bool _is_local;
	public bool Is_local
	{
		get { return _is_local; }
		set { _is_local = value; }
	}
	
	[SerializeField]
	bool _showPositionChanges = false;
	public bool ShowPositionChanges => _showPositionChanges;
	
	[SerializeField]
	public bool _realTimeChanges = true;
    //public bool RealTimeChanges
    //{
    //    get { return _realTimeChanges; }
    //    set { _realTimeChanges = value; }
    //}

    [SerializeField]
	bool _postPositionChanges = false;
	public bool PostPositionChanges
    {
        get { return _postPositionChanges; }
        set { _postPositionChanges = value; }
    }

	public string _headsetID;
	public Color _color = Color.red;
	
	private string _locationID;// will change this back to just string w/o public
	
	public string LocationID
	{
		get { return _locationID; }
		set { _locationID = value; }
	}
	
	float _lastTime;
	
	bool _isRegisteredWithServer = false;
	
	//if local, we set this to the MainCamera (the Hololens 2's camera)
	Camera _mainCamera;
	public GameObject map_parent; // This will get populated by the map's spawn target
	public GameObject parent_headset_manager; // This will get populated by EasyVizARHeadsetManager.cs
    public string local_headset_id = "";
    //public bool is_local = false;

    // This is for navigation
    public GameObject feature_parent;
    public LineRenderer navigation_line;
    EasyVizAR.Path path = new EasyVizAR.Path(); // this stores the path of points 

    //private EasyVizAR.NavigationTarget currentTarget;
    private EasyVizAR.NavigationTarget currentTarget = new EasyVizAR.NavigationTarget();

    public EasyVizARHeadset()
    {
        
    }

    public EasyVizARHeadset(Headset headset_class_data)
    {
		AssignValuesFromJson(headset_class_data);
		//_color = headset_class_data.color;
        //headset_class_data.map_parent = map_parent;
        //headset_class_data.parent_headset_manager = headsetManager;
        //headset_class_data.feature_parent = feature_parent;
    }

    void Awake()
	{
        currentTarget = new EasyVizAR.NavigationTarget();
        currentTarget.type = "uninitialized";
        currentTarget.target_id = "uninitialized";

        _mainCamera = Camera.main;

        navigation_line = _mainCamera.GetComponent<LineRenderer>();

        _lastTime = UnityEngine.Time.time;

        parent_headset_manager = EasyVizARHeadsetManager.EasyVizARManager.gameObject;
    }

    // Start is called before the first frame update
    void Start()
    {
        //I think this should perhaps be set in the constructor, with the calling
        //manager passing a reference, but this will work for now.
        parent_headset_manager = EasyVizARHeadsetManager.EasyVizARManager.gameObject;

        //if (_is_local) StartCoroutine(PositionPostingUpdate(_updateFrequency));
        StartCoroutine(PositionPostingUpdate(_updateFrequency));
    }

    public void Initialize(Headset headset_class_data)
    {
        AssignValuesFromJson(headset_class_data);
    }

    public void Initialize(Headset headset_class_data, GameObject map_parent, GameObject parent_headset_manager, GameObject feature_parent)
    {        
        this.map_parent = map_parent;
        //this.parent_headset_manager = parent_headset_manager;
        this.feature_parent = feature_parent;
		
		//JSON values can only be assigned after the headset manager is set
        AssignValuesFromJson(headset_class_data);
    }
    /*
        public EasyVizARHeadset(float updateFrequency, string headsetName, bool is_local, bool showPositionChanges, bool realTimeChanges, bool postPositionChanges, string headsetID, Color color, string locationID, float lastTime, bool isRegisteredWithServer, Camera mainCamera, GameObject map_parent, GameObject parent_headset_manager, string local_headset_id, GameObject feature_parent, LineRenderer line, EasyVizAR.Path path, NavigationTarget currentTarget)
        {
            ~~~~_headsetID = headsetID;
            ~~~~_color = color;
            ~~~~LocationID = locationID;
            ~~~~_headsetName = headsetName;

            @@@@_lastTime = lastTime;
            @@@@this.line = line;


            _updateFrequency = updateFrequency; 
            this.Is_local = is_local;
            _showPositionChanges = showPositionChanges;
            _realTimeChanges = realTimeChanges;
            _postPositionChanges = postPositionChanges;

            _isRegisteredWithServer = isRegisteredWithServer;
            _mainCamera = mainCamera;
            this.map_parent = map_parent;
            this.parent_headset_manager = parent_headset_manager;
            this.local_headset_id = local_headset_id;
            this.feature_parent = feature_parent;
            this.path = path;
            this.currentTarget = currentTarget;
        }
    */

    public void CreateLocalHeadset(string headsetName, string location, bool postChanges)
	{
		_is_local = true;
		_headsetName = headsetName;
		_locationID = location;
		
		if(_postPositionChanges)
		{
			_realTimeChanges = true;

			DistanceCalculation distance_calculator = this.GetComponent<DistanceCalculation>();

			// Either reload our existing headset from the server or create a new one.
			if (EasyVizARServer.Instance.TryGetHeadsetID(out string headsetId))
            {
				UnityEngine.Debug.Log("Reloading headset: " + headsetId);
                UnityEngine.Debug.Log("This is the local headset: " + headsetId);
				local_headset_id = headsetId;
                Is_local = true;
				distance_calculator.is_local = true;
                LoadHeadset(headsetId);
            } 
			else
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
        if (_is_local)
		{
            //PostPosition();

            float t = UnityEngine.Time.time;

			if(t - _lastTime > _updateFrequency)
			{
                if (_mainCamera && _postPositionChanges)
                {
                    transform.position = _mainCamera.transform.position;
                    transform.rotation = _mainCamera.transform.rotation;
                }

                //if (_isRegisteredWithServer && _postPositionChanges)
				{
					//PostPosition();
				}				
			}

            _lastTime = t;

            if (_realTimeChanges && _showPositionChanges)
            {
                GetPastPositions();
            }
        }
    }

    IEnumerator PositionPostingUpdate(double update_rate_in_seconds)
	{
		if (_is_local)
		{
			while (true)
			{
				transform.position = _mainCamera.transform.position;
				transform.rotation = _mainCamera.transform.rotation;
				//PostPosition();

				if (_mainCamera && _postPositionChanges)
				{
					transform.position = _mainCamera.transform.position;
					transform.rotation = _mainCamera.transform.rotation;
					PostPosition();
				}

				if (_isRegisteredWithServer && _postPositionChanges)
				{
					//PostPosition();
				}

                yield return new WaitForSeconds((float)update_rate_in_seconds);
            }
        }

        yield return new WaitForSeconds((float)update_rate_in_seconds);
    }
	

	//This might be a problem here? B I don't think this should be doing what it's doing /B
	public void AssignValuesFromJson(EasyVizAR.Headset json_headset_data)
	{
        // this is where the _color of the headset is assigned --> this field is populated 
        Color newColor;
        if (ColorUtility.TryParseHtmlString(json_headset_data.color, out newColor)) _color = newColor;

        _headsetID = json_headset_data.id;
        _locationID = json_headset_data.location_id;
        _headsetName = json_headset_data.name;

        transform.rotation = new Quaternion(json_headset_data.orientation.x, json_headset_data.orientation.y, json_headset_data.orientation.z, json_headset_data.orientation.w);

        transform.position = new Vector3(json_headset_data.position.x, json_headset_data.position.y, json_headset_data.position.z);

        Transform cur_headset = parent_headset_manager.transform.Find(json_headset_data.id);
        
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


        // Not sure if this is the right area to do the navigation

		//Getting errors here becuase of trying to access uninitialized class memebers
		//we should have a constructor for these classes that are used in comparison to
		//prevent null strings from being used in comparisons -B

        if (currentTarget == null || json_headset_data.navigation_target.target_id != currentTarget.target_id)
        {
            currentTarget = json_headset_data.navigation_target;

            if (currentTarget.type == "feature" || currentTarget.type == "headset" || currentTarget.type == "point")
            {
                FindPath(json_headset_data.navigation_target.position);
            }
            else
            {
                // If target type is none, we should clear the navigation path,
                // but there is no need to call the server for pathing.

                //this line is throwing an null refernce error /B
                navigation_line.positionCount = 0;
            }

        }
    }

    public void RegisterHeadset()
	{
		//register the headset with the server, first checking if it exists there already or not...
		EasyVizARServer.Instance.Get("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, RegisterCallback);
	}
/*
    public bool HeadsetRegistrationCheck(String headset_ID)
    {
        //register the headset with the server, first checking if it exists there already or not...
        EasyVizARServer.Instance.Get("headsets/" + headset_ID, EasyVizARServer.JSON_TYPE, RegisterCallback);
    }
*/
    void RegisterCallback(string resultData)
	{
		if(resultData != "error")
		{
			//Debug.Log(resultData);
			EasyVizAR.Headset h = JsonUtility.FromJson<Headset>(resultData);
			//fill in any local data here from the server...
			AssignValuesFromJson(h);
		}
		else
		{
			//headset doesn't yet exist, let's create a new one...
			CreateHeadset();
		}
	}

	//This is a special case, where the headset is not registered to the server yet, so 
	//there is also no game object associated with this class. It is being used as a data
	//container to regiseter the headset with the server to recieve a UID. It needs to have
	//a special case because there are game object componenets that will recieve data after it
	//has been registered
    public void CreateHeadsetToRegister()
    {
        EasyVizAR.Headset h = new EasyVizAR.Headset();
        h.position = new EasyVizAR.Position();
        h.orientation = new EasyVizAR.Orientation();

        h.position.x = 0;
        h.position.y = 0;
        h.position.z = 0;

        h.orientation.x = 0;
        h.orientation.y = 0;
        h.orientation.z = 0;
        h.orientation.w = 0;


        h.name = _headsetName;
        h.location_id = _locationID;

        EasyVizARServer.Instance.Post("headsets", EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), CreateRegisterCallback);
    }

    void CreateRegisterCallback(string resultData)
    {
        if (resultData != "error")
        {
            _isRegisteredWithServer = true;

            EasyVizAR.RegisteredHeadset h = JsonUtility.FromJson<EasyVizAR.RegisteredHeadset>(resultData);
/*            
 *            
 *         Vector3 newPos = Vector3.zero;

            newPos.x = h.position.x;
            newPos.y = h.position.y;
            newPos.z = h.position.z;

            transform.position = newPos;
            transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);
*/
            _headsetID = h.id;
            _headsetName = h.name;
            _locationID = h.location_id;

            Color newColor;
            if (ColorUtility.TryParseHtmlString(h.color, out newColor))
                _color = newColor;

            EasyVizARServer.Instance.SaveRegistration(h.id, h.token);

            //This calls back to the inital registration check so that the local ID can be checked and initialized
            EasyVizARHeadsetManager.EasyVizARManager.gameObject.GetComponent<EasyVizARHeadsetManager>().LocalRegistrationSetup();

            UnityEngine.Debug.Log("Successfully connected headset: " + h.name);
        }
        else
        {
            UnityEngine.Debug.Log("Received an error when creating headset");
        }
    }


    void CreateHeadset()
	{
		EasyVizAR.Headset h = new EasyVizAR.Headset();
		h.position = new EasyVizAR.Position();
        h.orientation = new EasyVizAR.Orientation();

		// If this script is not attached to a game object we want to avoid a null
		// refrence siutaiton

            h.position.x = transform.position.x;
            h.position.y = transform.position.y;
            h.position.z = transform.position.z;

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

		h.location_id = _locationID;
        h.position = new EasyVizAR.Position();
		h.position.x = (float)transform.position.x;
		h.position.y = (float)transform.position.y;
		h.position.z = (float)transform.position.z;
		h.orientation = new EasyVizAR.Orientation();
		h.orientation.x = (float)transform.rotation[0];
		h.orientation.y = (float)transform.rotation[1];
		h.orientation.z = (float)transform.rotation[2];
		h.orientation.w = (float)transform.rotation[3];
        JsonUtility.ToJson(h);


        EasyVizARServer.Instance.Patch("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), PostPositionCallback);
	}
	
	
	void PostPositionCallback(string resultData)
	{
		if(verbose_debug)UnityEngine.Debug.Log(resultData);
		
		if(resultData != "error")
		{
			
		}
		else
		{
			UnityEngine.Debug.Log("Received an error when posting position");
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

            // I don't think this should happen here, we don't want the visuals tied to the data callback -B
            bool BUG_TEST = false;
            if (BUG_TEST)
			{
				int cnt = 0;
				// making sure we are creating a new line
				if (navigation_line.positionCount > 0) navigation_line.positionCount = 0;
				EasyVizAR.Position target_pos = new EasyVizAR.Position();
				foreach (EasyVizAR.Position points in path.points)
				{
					navigation_line.positionCount++;
					navigation_line.SetPosition(cnt++, new Vector3(points.x, points.y, points.z)); // this draws the line 
																						//UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
																						//UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);
					target_pos = points;
				}
			}
            UnityEngine.Debug.Log("Successfully added the points");

        }

    }
}
