using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using System;

public class HeadsetConfigurationChangedEvent
{
    public Uri ServerURI;
    public string LocationID;
    public string HeadsetID;

    public EasyVizAR.HeadsetConfiguration Configuration;
}

public class EasyVizARHeadsetManager : MonoBehaviour
{
    public static EasyVizARHeadsetManager EasyVizARManager { get; private set; }

    [SerializeField]
    string _locationId = "none";
    public string LocationID
    {
        get { return _locationId; }
        set { _locationId = value; }
    }

    [SerializeField]
    GameObject _headsetPrefab;  //prefab for loading other headsets...

    [SerializeField]
    GameObject _volumeHeadsetPrefab;

    // Attach QRScanner GameObject so we can listen for location change events.
    [SerializeField]
    GameObject _qrScanner;

    [SerializeField]
    string _localHeadsetName;
    public string LocalHeadsetName => _localHeadsetName;

    GameObject _localHeadset;
    public GameObject LocalHeadset
    {
        get { return _localHeadset; }
    }

    [SerializeField]
    public string _local_headset_ID = "";

    public List<EasyVizARHeadset> _activeHeadsets = new List<EasyVizARHeadset>();

    // TODO we should be able to clear and load the headset list any time a new QR code is scanned, not just the first time
    private bool _headsetsCreated = false;

    [SerializeField]
    Material _localMaterial;

    //[SerializeField]
    //bool _visualizePreviousLocal = false;

    [SerializeField]
    bool _makeUniqueLocalHeadset = false;

    [SerializeField]
    List<GameObject> _mapObjects = new List<GameObject>();

    // I had to add these to get the slider to work. I wanted to pass the slider value as an argument
    // to this script, but I couldn't figure out how to reference that value in the event handler. This
    // way I at least have a reference to the sliders and can access their values directly
    public GameObject horizontal_offset_slider;
    public GameObject vertical_offset_slider;

    //for displaying headset icon
    public GameObject map_parent;
    public GameObject headset_icon;
    public GameObject headsetManager;
    public Color _color = Color.red;
    public GameObject feature_parent;
    public GameObject volumetricMapParent;
    public bool verbose_debug_log;

    private GameObject local_headset_map_icon;

    public event EventHandler<HeadsetConfigurationChangedEvent> HeadsetConfigurationChanged;

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (EasyVizARManager != null && EasyVizARManager != this)
        {
            Destroy(this);
        }
        else
        {
            EasyVizARManager = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //The EasyVizARHeadsetManager script componenet should be attached to the corresponding GameObject
        headsetManager = this.gameObject;

        if (_qrScanner)
        {
            var scanner = _qrScanner.GetComponent<QRScanner>();

            // The QRScanner will call this function once whenever a new location QR code is detected.
            // This would be a good place to initiate creating a new check-in and loading all of the
            // the other headsets that are already in the location.
            scanner.LocationChanged += (o, ev) =>
            {
                _locationId = ev.LocationID;

                // Update the location ID in the headset object, which will be sent with pose updates to the server.
                if (_localHeadset is not null)
                {
                    EasyVizARHeadset headset = _localHeadset.GetComponent<EasyVizARHeadset>();
                    headset.LocationID = this.LocationID;
                }

                LocalRegistrationSetup();
                Invoke(nameof(HideLocalVisuals), 2f);
            };
        }

    }

    // Update is called once per frame
    void Update()
    {
        // added for displaying distance
        //DisplayHeadsetsDistance();
        if (local_headset_map_icon is not null)
        {
            MoveAndRotateIcon(local_headset_map_icon, Camera.main.transform);      
        }
    }

    void OnEnable()
    {
        //Get the local headsetID if we've registered on the server

        //EasyVizARServer.Instance.TryGetHeadsetID(out string _local_headset_ID);
    }

    void OnDisable()
    {

    }

    public void HideLocalVisuals()
    {
        foreach(Transform headset in transform)
        {   
            if(headset.name == _local_headset_ID)
            {
                Transform match_ID = headset.Find("Capsule");
                GameObject capsule_visual = match_ID.gameObject;
                capsule_visual.SetActive(false);
            }
        }
    }

    public void HeadsetRegistrationCheck(String headset_ID)
    {
        EasyVizARServer.Instance.Get("headsets/" + headset_ID, EasyVizARServer.JSON_TYPE, RegisterCheckCallback);
    }

    void RegisterCheckCallback(string resultData)
    {
        if (verbose_debug_log) Debug.Log("Registration Callback Check: " + resultData);

        //Returns error if no headset with ID on server
        if (resultData != "error")
        {
            EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);

            //If the ID is on the server, the local headset ID is registered and we can create the headsets
            if (h.id == _local_headset_ID)
            {
                if (verbose_debug_log) Debug.Log("Registraiton ID is on server");

                // Tell the server the headset is moving to a new location (create a check-in or aka device tracking session).
                // We do not need to do this when we register a new headset because we pass the location ID during registration,
                // but we should do so when starting a new session with an existing headset. Otherwise, the headset will be
                // on the wrong map!
                CreateCheckIn(h.id, _locationId);

                CreateHeadsets();

				LoadHeadsetConfiguration(h.id);

                //callback_headset_registered = true;
            }
            else
            {
                //If the ID is not on the server, we need to create a new registration and post it to the server. This might not get reached ever, but I'm not sure atm
                if (verbose_debug_log) Debug.Log("Registraiton ID is NOT on server INNER");
                CreateNewRegistration();
            }
        }
        else
        {
            if (verbose_debug_log) Debug.Log("Registraiton ID is NOT on server");
            CreateNewRegistration();
        }
    }

	void LoadHeadsetConfiguration(String headset_id)
	{
        EasyVizARServer.Instance.Get($"/headsets/{headset_id}/configuration", EasyVizARServer.JSON_TYPE, delegate (string result)
		{
			if (result != "error") {
                if (HeadsetConfigurationChanged is not null)
                {
                    HeadsetConfigurationChangedEvent change = new HeadsetConfigurationChangedEvent();
                    change.ServerURI = EasyVizARServer.Instance.GetServerURI();
                    change.HeadsetID = headset_id;
                    change.LocationID = _locationId;
                    change.Configuration = JsonUtility.FromJson<EasyVizAR.HeadsetConfiguration>(result);

                    HeadsetConfigurationChanged(this, change);
                }
			}
		});
	}

    [ContextMenu("CreateAllHeadsets")]
    public void LocalRegistrationSetup()
    {
        //Tryget HeadsetID looks to see if there's a registration file locally
        
        //False case, there is no local registration, so we need to create a new headset, post it's data, and save that registration
        if (! EasyVizARServer.Instance.TryGetHeadsetID(out string _registered_headset_ID))
        {
            if (verbose_debug_log) Debug.Log("No Registration: " + _registered_headset_ID);

            CreateNewRegistration();
        }
        //True case, there is a local registration, so we need to check if it's on the server too
        else
        {
            _local_headset_ID = _registered_headset_ID;

            if (verbose_debug_log) Debug.Log("Found Registration: " + _registered_headset_ID);
            //If there is a UID check it against the server

            HeadsetRegistrationCheck(_local_headset_ID);
        }

        /*        if (EasyVizARServer.Instance.TryGetHeadsetID(out string new_registered_headset_ID))
                {
                    _local_headset_ID = new_registered_headset_ID;

                    if (!_headsetsCreated)
                    {
                        //CreateLocalHeadset();
                        CreateHeadsets();

                        _headsetsCreated = true;
                    }
                }*/
    }

    public void CreateCheckIn(string headsetId, string locationId)
    {
        var checkIn = new EasyVizAR.NewCheckIn();
        checkIn.location_id = locationId;

        EasyVizARServer.Instance.Post($"/headsets/{headsetId}/check-ins", EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(checkIn), delegate (string result)
        {
            // Nothing needs to be done with the response.
        });
    }

    void CreateNewRegistration()
    {
        //EasyVizAR.Headset new_registration = new EasyVizAR.Headset();
        // new_registration.location_id = this.LocationID;
        // new_registration.name = _localHeadsetName;
        //EasyVizARHeadset new_registration.CreateHeadsetToRegister();


        GameObject registration_setup_holder = new GameObject();

        EasyVizARHeadset new_registration = registration_setup_holder.AddComponent<EasyVizARHeadset>();
        new_registration.LocationID = this.LocationID;
        new_registration.Name = _localHeadsetName;
        new_registration.CreateHeadsetToRegister();

        Destroy(registration_setup_holder);
    }

    public void DisplayMapCallback(Texture resultTexture)
    {
        //Debug.Log("In map callback");
        foreach (var map_layout in _mapObjects)
        {
            //map_layout.GetComponent<Renderer>().material.mainTexture = resultTexture;
        }
    }

    public void DisplayHandMap()
    {
        EasyVizARServer.Instance.Texture("locations/" + _locationId + "/layers/1/image", "image/png", "1200", DisplayMapCallback);
    }

//a DisplayHandMap function that takes a list of maps and gets the images for each one, indexed by the map number





    public void ToggleBreadcrumbs()
    {
        //For every headset in the active list set the line renderer to the opposite state
        foreach (EasyVizARHeadset headset_user in _activeHeadsets)
        {
            //Turns out it's not the line renderer that needs to be disabled to hide the line, it's the entire object
            //holding the lines
            //--LineRenderer line_trail = headset_user.gameObject.transform.GetChild(0).GetComponent<LineRenderer>();
            GameObject line_trail = headset_user.gameObject.transform.GetChild(0).gameObject;

            //This only works for componenets
            //--line_trail.enabled = !line_trail.enabled;			

            //To de/activate game objects use
            line_trail.SetActive(!line_trail.activeSelf);
        }
    }

    //These don't work correctly. The funcitons are being called all the time while the user interacts with the slider
    //This is causing an error due to rounding I think. The funcitons are getting the position of the current trail
    //renderer and keeping one component of it, this is causing drift on the values taht are supposed to stay the same
    public void BreadcrumbsOffsetVertical()
    {
        //Getting the data from the pinch sliders as a value to offset.
        PinchSlider offset_slider_vertical = vertical_offset_slider.GetComponent<PinchSlider>();

        //if (verbose_debug_log) Debug.Log(offset_slider_vertical.SliderValue);

        // MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
        // figure out how to change it's range so I'm scaling the value here.
        // it should give a new raw range of -0.5 to 0.5

        float default_slider_middle = 0.5f;

        float slider_value_zero_offset_vertical = offset_slider_vertical.SliderValue - default_slider_middle;

        // ideally I think I want a linear scaling that allows the user to set the upper
        // and lower bounds of the values
        // I'm going to just multiply by 5 because 2.5 meters seems reasonable
        float scale_factor = 5;

        float scaled_slider_value_zero_offset_vertical = slider_value_zero_offset_vertical * scale_factor;

        //For every headset in the active list get it's position and line renderer
        //update the vertical componenet based on the offset relative to the headset position.
        foreach (EasyVizARHeadset headset_user in _activeHeadsets)
        {
            //I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
            //as to how offsets of the parent of the line rendere itself work
            var trail_root = headset_user.gameObject.transform.GetChild(0);
            LineRenderer r = trail_root.GetComponent<LineRenderer>();

            if (r != null)
            {
                //Offset relative to the head position, add the offset from the center
                var head_position = headset_user.GetComponent<Transform>().position;
                var trail_local_position = trail_root.GetComponent<Transform>().localPosition;


                //Create the new position vector from the vertical and horizontal scaled offsets
                var scaled_root_position = new Vector3(trail_local_position.x, scaled_slider_value_zero_offset_vertical, 0) + head_position;

                // Set the line renderer's parent to the new scaled offset position.
                // I THINK? check here again if line rendering is not as expected
                trail_root.GetComponent<Transform>().position = scaled_root_position;
            }
        }
    }

    public void BreadcrumbsOffsetHorizontal()
    {
        //Getting the data from the pinch sliders as a value to offset.
        PinchSlider offset_slider_horizontal = horizontal_offset_slider.GetComponent<PinchSlider>();

        //if (verbose_debug_log) Debug.Log(offset_slider_vertical.SliderValue);

        // MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
        // figure out how to change it's range so I'm scaling the value here.
        // it should give a new raw range of -0.5 to 0.5

        float default_slider_middle = 0.5f;

        float slider_value_zero_offset_horizontal = offset_slider_horizontal.SliderValue - default_slider_middle;

        // ideally I think I want a linear scaling that allows the user to set the upper
        // and lower bounds of the values
        // I'm going to just multiply by 5 because 2.5 meters seems reasonable
        float scale_factor = 5;

        float scaled_slider_value_zero_offset_horizontal = slider_value_zero_offset_horizontal * scale_factor;

        //For every headset in the active list get it's position and line renderer
        //update the vertical componenet based on the offset relative to the headset position.
        foreach (EasyVizARHeadset headset_user in _activeHeadsets)
        {
            //I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
            //as to how offsets of the parent of the line rendere itself work
            var trail_root = headset_user.gameObject.transform.GetChild(0);
            LineRenderer r = trail_root.GetComponent<LineRenderer>();

            if (r != null)
            {
                //Offset relative to the head position, add the offset from the center
                var head_position = headset_user.GetComponent<Transform>().position;
                var trail_local_position = trail_root.GetComponent<Transform>().localPosition;

                // I don't think this part makes sense, I was trying to preserve the y value, but this isn't doing that
                // it's just getting the head position y, not the trail offset y. This is the problem with this method.
                //The other approach does work, but causes drift due to rounding. I just want to modify this one
                // component of the vector and then set the new offset position

                Vector3 scaled_horizontal_position = new Vector3(scaled_slider_value_zero_offset_horizontal, 0, 0) + head_position;
                float scaled_horizontal_position_y = scaled_horizontal_position.y;
                Debug.Log("scaled_horizontal_position_y" + scaled_horizontal_position_y);


                //Trying something else, direct modification of the vector component
                //THIS IS NOT WORKING
                // I don't know why, but the pre and post offset valuesa re the same :(
                Vector3 trail_position = trail_root.GetComponent<Transform>().position;
                Debug.Log("pre offset" + trail_position);

                trail_position[0] = scaled_horizontal_position_y;
                trail_root.GetComponent<Transform>().position = trail_position;

                Debug.Log("post offset" + trail_position);

                /*

				//Create the new position vector from the vertical and horizontal scaled offsets
				var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, trail_local_position.y, 0) + head_position;

				// Set the line renderer's parent to the new scaled offset position.
				// I THINK? check here again if line rendering is not as expected
				trail_root.GetComponent<Transform>().position = scaled_root_position;
				*/
            }
        }
    }

    /*
     * We want to offset the breacrumbs realtive to the head to let the user adjust the position
     * 
     * 
     */
    public void ChangeBreadcrumbsOffset()
    {
        //Getting the data from the pinch sliders as a value to offset.
        PinchSlider offset_slider_vertical = vertical_offset_slider.GetComponent<PinchSlider>();
        PinchSlider offset_slider_horizontal = horizontal_offset_slider.GetComponent<PinchSlider>();

        // MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
        // figure out how to change it's range so I'm scaling the value here.
        // it should give a new raw range of -0.5 to 0.5

        float default_slider_middle = 0.5f;

        float slider_value_zero_offset_vertical = offset_slider_vertical.SliderValue - default_slider_middle;
        float slider_value_zero_offset_horizontal = offset_slider_horizontal.SliderValue - default_slider_middle;

        // ideally I think I want a linear scaling that allows the user to set the upper
        // and lower bounds of the values
        // I'm going to just multiply by 5 because 2.5 meters seems reasonable
        float scale_factor = 5;

        float scaled_slider_value_zero_offset_vertical = slider_value_zero_offset_vertical * scale_factor;
        float scaled_slider_value_zero_offset_horizontal = slider_value_zero_offset_horizontal * scale_factor;

        foreach (EasyVizARHeadset headset_user in _activeHeadsets)
        {
            //I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
            //as to how offsets of the parent of the line rendere itself work
            var trail_root = headset_user.gameObject.transform.GetChild(0);
            LineRenderer r = trail_root.GetComponent<LineRenderer>();

            if (r != null)
            {
                //Offset relative to the head position, add the offset from the center
                var head_position = headset_user.GetComponent<Transform>().position;
                var trail_local_position = trail_root.GetComponent<Transform>().localPosition;
                //Create the new position vector from the vertical and horizontal scaled offsets

                var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, scaled_slider_value_zero_offset_vertical, 0) + head_position;
                /*

				//Create the new position vector from the vertical and horizontal scaled offsets
				var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, trail_local_position.y, 0) + head_position;

				// Set the line renderer's parent to the new scaled offset position.
				// I THINK? check here again if line rendering is not as expected
				
				*/
                // Set the root to the new scaled offset position
                trail_root.GetComponent<Transform>().position = scaled_root_position;
            }
        }
    }

    void CreateLocalHeadset()
    {
        _localHeadset = Instantiate(_headsetPrefab, transform);
        

        if (_localHeadset != null)
        {
            EasyVizARHeadset local_headset = _localHeadset.GetComponent<EasyVizARHeadset>();

            if (_localMaterial != null)
            {
                _localHeadset.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
            }

            if (_makeUniqueLocalHeadset)
            {
                string s = System.DateTime.Now.ToString();

                local_headset.CreateLocalHeadset(_localHeadsetName + "_" + s, _locationId);
            }
            else
            {
                local_headset.CreateLocalHeadset(_localHeadsetName, _locationId);
            }
            Debug.Log("Create local headset says this is the local headset name: " + local_headset.name);
        }
    }

    /*    void OLD_CreateLocalHeadset()
        {
            if (!_visualizePreviousLocal)
            {
                _localHeadset = Instantiate(_headsetPrefab, transform);

                if (_localHeadset != null)
                {
                    EasyVizARHeadset local_headset = _localHeadset.GetComponent<EasyVizARHeadset>();

                    if (_localMaterial != null)
                    {
                        _localHeadset.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
                    }

                    if (_makeUniqueLocalHeadset)
                    {
                        string s = System.DateTime.Now.ToString();

                        local_headset.CreateLocalHeadset(_localHeadsetName + "_" + s, _locationId, !_visualizePreviousLocal);
                    }
                    else
                    {
                        local_headset.CreateLocalHeadset(_localHeadsetName, _locationId, !_visualizePreviousLocal);
                    }
                    Debug.Log("Create local headset says this is the local headset name: " + local_headset.name);
                }
            }
        }*/


    public void MoveAndRotateIcon(GameObject target, Transform source)
    {
        target.transform.localPosition = new Vector3(source.position.x, 0, source.position.z);

        Vector3 euler_rotation = new Vector3(0, source.eulerAngles.y, 0);
        var icon_visual = target.transform.Find("Icon Visuals");
        if (icon_visual is not null)
        {
            // Try to rotate only the icon.
            var rotation = icon_visual.transform.localEulerAngles;
            rotation.y = source.eulerAngles.y;
            icon_visual.transform.localEulerAngles = rotation;
        }
        else
        {
            // Otherwise, we end up rotating the text as well.
            var rotation = new Vector3(0, source.eulerAngles.y, 0);
            target.transform.localEulerAngles = rotation;
        }

        var marker_object = target.GetComponent<MarkerObject>();
        if (marker_object is not null)
        {
            marker_object.world_position = source.position;
        }
    }


    // This is where we are instantiating the GameObject of Headset in Unity
    public void CreateRemoteHeadsetOLD(EasyVizAR.Headset remote_headset)
    {
        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);
        headset_game_object.name = remote_headset.id;

        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        headset_class_data.map_parent = map_parent;
        headset_class_data.parent_headset_manager = headsetManager;
        headset_class_data.feature_parent = feature_parent;

        if (headset_class_data != null)
        {
            headset_class_data.AssignValuesFromJson(remote_headset);
            _activeHeadsets.Add(headset_class_data);
        }
    }

    //Working on the constructor implementation. There are still some values that need to be passed
    // by reference to the constructor.
    //I will also need to build a constructor for DistanceCalculation
    public void CreateRemoteHeadset(EasyVizAR.Headset remote_headset)
    {
        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);
   
        headset_game_object.name = remote_headset.id;

        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        if (headset_class_data != null)
        {
            //This has to be initialized before the JSON values are assigned 
            headset_class_data.Initialize(remote_headset, map_parent, headsetManager, feature_parent);

            _activeHeadsets.Add(headset_class_data);

            MarkerObject marker_object = headset_game_object.GetComponent<MarkerObject>();
            if (marker_object is not null)
            {
                marker_object.feature_type = "headset";
                marker_object.feature_name = remote_headset.name;
                marker_object.world_position = headset_game_object.transform.position;
            }

            if (map_parent is not null)
            {
                var headset_map_marker = Instantiate(headset_icon, map_parent.transform, false);
                headset_map_marker.name = remote_headset.id;

                MoveAndRotateIcon(headset_map_marker, headset_game_object.transform);

                var icon_renderer = headset_map_marker.transform.Find("Icon Visuals")?.GetComponent<Renderer>();
                if (icon_renderer is not null)
                {
                    Color myColor = headset_class_data._color;
                    icon_renderer.material.SetColor("_EmissionColor", myColor);
                }

                var map_marker_object = headset_map_marker.GetComponent<MarkerObject>();
                if (map_marker_object is not null)
                {
                    map_marker_object.feature_type = "headset";
                    map_marker_object.feature_name = remote_headset.name;
                }
            }
        }

        if(volumetricMapParent != null)
        {
            CreateVolumetricMapHeadset(remote_headset);
        }
    }

    public void CreateVolumetricMapHeadset(EasyVizAR.Headset remote_headset)
    {
        GameObject headset_game_object = Instantiate(_volumeHeadsetPrefab, volumetricMapParent.transform, false);

        //headset_game_object.transform.localPosition = headset_game_object.transform.position;

        headset_game_object.name = remote_headset.id;

        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        if (headset_class_data != null)
        {
            //This has to be initialized before the JSON values are assigned 
            headset_class_data.Initialize(remote_headset, volumetricMapParent, headsetManager, feature_parent);

            _activeHeadsets.Add(headset_class_data);

            MarkerObject marker_object = headset_game_object.GetComponent<MarkerObject>();
            if (marker_object is not null)
            {
                marker_object.feature_type = "headset";
                marker_object.feature_name = remote_headset.name;
                marker_object.world_position = headset_game_object.transform.position;
                
                MoveAndRotateIcon(headset_game_object, headset_game_object.transform); //this is the line that actually sets the right position for the headsets on the map

                var icon_renderer = headset_game_object.transform.Find("Capsule")?.GetComponent<Renderer>();
                if (icon_renderer is not null)
                {
                    Color myColor = headset_class_data._color;
                    icon_renderer.material.SetColor("_EmissionColor", myColor);
                }
            }

/*            if (volumetricMapParent is not null)
            {
                var headset_map_marker = Instantiate(headset_icon, volumetricMapParent.transform, false);
                headset_map_marker.name = remote_headset.id;

                MoveAndRotateIcon(headset_map_marker, headset_game_object.transform);

                var icon_renderer = headset_map_marker.transform.Find("Icon Visuals")?.GetComponent<Renderer>();
                if (icon_renderer is not null)
                {
                    Color myColor = headset_class_data._color;
                    icon_renderer.material.SetColor("_EmissionColor", myColor);
                }

                var map_marker_object = headset_map_marker.GetComponent<MarkerObject>();
                if (map_marker_object is not null)
                {
                    map_marker_object.feature_type = "headset";
                    map_marker_object.feature_name = remote_headset.name;
                }
            }*/
        }


    }

    public void UpdateRemoteHeadset(string previous_id, EasyVizAR.Headset remoteHeadset)
    {
        EasyVizARHeadset matched_headset = _activeHeadsets.Find(find_headset => find_headset._headsetID == previous_id);

        if (matched_headset == null)
        {
            CreateRemoteHeadset(remoteHeadset);
        }
        else if (matched_headset.Is_local) return;
        else
        {
            matched_headset.AssignValuesFromJson(remoteHeadset);

            var marker_object = matched_headset.GetComponent<MarkerObject>();
            if (marker_object is not null)
            {
                marker_object.feature_name = remoteHeadset.name;
                marker_object.world_position = matched_headset.transform.position;
            }

            var map_icon = map_parent.transform.Find(previous_id);
            if (map_icon is not null)
            {
                MoveAndRotateIcon(map_icon.gameObject, matched_headset.transform);

                var map_marker_object = map_icon.GetComponent<MarkerObject>();
                map_marker_object.feature_name = remoteHeadset.name;
            }
            else
            {
                UnityEngine.Debug.Log("Could not find map icon for " + previous_id);
            }
        }
    }

    public void DeleteRemoteHeadset(string id)
    {
        //foreach (var headset in _activeHeadsets)
        for (int i = 0; i < _activeHeadsets.Count; i++)
        {
            // Definitely should be matching on ID rather than name.--> changed 
            if (_activeHeadsets[i]._headsetID == id)
            {
                //Destroy(_activeHeadsets[i].gameObject); //this wasn't working for some unkn
                Transform current_headset = headsetManager.transform.Find(_activeHeadsets[i]._headsetID);

                if (current_headset)
                {
                    Destroy(current_headset.gameObject);
                }

                _activeHeadsets.RemoveAt(i);
                // TODO: if this works, delete the DeleteIcon() from MapIconSpawn.cs

                if (map_parent != null)
                {
                    //Debug.Log("the delete name: " + name);
                    Transform delete_headset = map_parent.transform.Find(_activeHeadsets[i]._headsetID);
                    if (delete_headset)
                    {
                        Debug.Log("Found and Destroyed the headset");
                        Destroy(delete_headset.gameObject);
                    }
                }

                break;
            }
        }
    }

    void CreateHeadsetsCallback(string result_data)
    {
        if (verbose_debug_log) Debug.Log(result_data);

        //Data validation check
        if (result_data == "error" || string.IsNullOrEmpty(result_data) || result_data.Length <= 2)
        {
            Debug.LogError("Error in data payload CreateHeadsetsCallback");
            return;
        }

        //parse list of headsets for this location and create...
        //the key to parsing the array - the text we add here has to match the name of the variable in the array wrapper class (headsets).
        EasyVizAR.HeadsetList headset_list = JsonUtility.FromJson<EasyVizAR.HeadsetList>(result_data);

        // why are we pre-incrementing i???
        //Only one local headset and we know the ID. Find and spawn local, otherwise remote
        for (int i = 0; i < headset_list.headsets.Length; ++i)
        {
            if (headset_list.headsets[i].id == _local_headset_ID)
            {
                Debug.Log("Found Local " + headset_list.headsets[i].id);

                CreateLocalHeadset(headset_list.headsets[i]);
            }
            else
            {
                Debug.Log("No Local " + headset_list.headsets[i].id);
                CreateRemoteHeadset(headset_list.headsets[i]);
            }
        }
    }

    private void CreateLocalHeadsetConstructor(EasyVizAR.Headset local_headset)
    {
        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);
        headset_game_object.name = local_headset.id;


        //There'
        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        //headset_class_data = new EasyVizARHeadset(remote_headset);

        //EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        if (headset_class_data != null)
        {
            //This is needed right now because the map parent is used to spawn the icon in the map, but 
            //it really shouldn't be there. it should probably be in this class or a view manager

            Debug.Log("Is it NULL?? " + headset_class_data);


            headset_class_data.map_parent = map_parent;
            headset_class_data.parent_headset_manager = headsetManager; //This needs to be assigned before JSON!!!!!!!!!!!!!!!
            headset_class_data.feature_parent = feature_parent;

            headset_class_data.AssignValuesFromJson(local_headset);

            headset_class_data.Is_local = true;
            Debug.Log("Is it local?? " + headset_class_data.Is_local);
            headset_class_data.LocationID = local_headset.location_id;
            headset_class_data.local_headset_id = local_headset.id;

            if (_localMaterial != null)
            {
                headset_game_object.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
            }


            _activeHeadsets.Add(headset_class_data);
        }
        else
        {
            Debug.Log("WHY NULL?? " + headset_class_data);
        }
    }

    private void CreateLocalHeadset(EasyVizAR.Headset local_headset)
    {
        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);
        headset_game_object.name = local_headset.id;

        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();

        if (headset_class_data != null)
        {
            Debug.Log("Is it NULL?? " + headset_class_data);


            headset_class_data.map_parent = map_parent;
            headset_class_data.parent_headset_manager = headsetManager; //This needs to be assigned before JSON!!!!!!!!!!!!!!!
            headset_class_data.feature_parent = feature_parent;

            headset_class_data.AssignValuesFromJson(local_headset);

            headset_class_data.Is_local = true;
            Debug.Log("Is it local?? " + headset_class_data.Is_local);
            headset_class_data.LocationID = local_headset.location_id;
            headset_class_data.local_headset_id = local_headset.id;
            headset_class_data.PostPositionChanges = true;

            if (_localMaterial != null)
            {
                headset_game_object.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
            }

            _activeHeadsets.Add(headset_class_data);

            if (map_parent is not null)
            {
                local_headset_map_icon = Instantiate(headset_icon, map_parent.transform, false);
                local_headset_map_icon.name = local_headset.id;

                MoveAndRotateIcon(local_headset_map_icon, Camera.main.transform);

                var icon_renderer = local_headset_map_icon.transform.Find("Icon Visuals")?.GetComponent<Renderer>();
                if (icon_renderer is not null)
                {
                    Color myColor = headset_class_data._color;
                    icon_renderer.material.SetColor("_EmissionColor", myColor);
                }

                var map_marker_object = local_headset_map_icon.GetComponent<MarkerObject>();
                if (map_marker_object is not null)
                {
                    map_marker_object.feature_type = "headset";
                    map_marker_object.feature_name = local_headset.name;
                }
            }
        }
        else
        {
            Debug.Log("WHY NULL?? " + headset_class_data);
        }
    }
/*
    void OLD_CreateHeadsetsCallback(string resultData)
    {
        if (verbose_debug_log) Debug.Log(resultData);

        if (resultData != "error" && resultData.Length > 2)
        {
            //parse list of headsets for this location and create...
            //the key to parsing the array - the text we add here has to match the name of the variable in the array wrapper class (headsets).
            EasyVizAR.HeadsetList headset_list = JsonUtility.FromJson<EasyVizAR.HeadsetList>("{\"headsets\":" + resultData + "}");

            // why are we pre-incrementing i???
            for (int i = 0; i < headset_list.headsets.Length; ++i)
            {
                Debug.Log("LOKAL NAMER OVEN " + _localHeadsetName);


                if (headset_list.headsets[i].name != _localHeadsetName || _visualizePreviousLocal)
                {
                    if (headset_list.headsets[i].name == _localHeadsetName)
                    {
                        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);

                        EasyVizARHeadset headset = headset_game_object.GetComponent<EasyVizARHeadset>();
                        if (headset != null)
                        {
                            //s.name = h.headsets[i].name;
                            headset_game_object.name = headset_list.headsets[i].id;

                            if (_localMaterial != null)
                            {
                                headset_game_object.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
                            }
                            headset.AssignValuesFromJson(headset_list.headsets[i]);
                            headset.Is_local = true;
                            headset.LocationID = headset_list.headsets[i].location_id;
                            _activeHeadsets.Add(headset);
                        }
                    }
                    else
                    {
                        CreateRemoteHeadset(headset_list.headsets[i]);
                    }
                }
            }
        }
        else
        {
            Debug.Log("LOKAL OLD_CreateHeadsetsCallback  ERROR ");
        }
    }
*/
    public void CreateHeadsets()
    {
        //list headsets from server for our location, create a prefab of each...
        EasyVizARServer.Instance.Get("headsets?envelope=headsets&location_id=" + _locationId, EasyVizARServer.JSON_TYPE, CreateHeadsetsCallback);
    }


}
