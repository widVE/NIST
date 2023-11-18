using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using System;



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
    string _local_headset_ID = "";

    [SerializeField]
    bool _shouldCreateHeadsets = false;
    public bool ShouldCreateHeadsets => _shouldCreateHeadsets;

    public List<EasyVizARHeadset> _activeHeadsets = new List<EasyVizARHeadset>();

    bool _headsetsCreated = false;

    [SerializeField]
    Material _localMaterial;

    [SerializeField]
    bool _visualizePreviousLocal = false;

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
    public GameObject headsetManager;
    public Color _color = Color.red;
    public GameObject feature_parent;
    public bool verbose_debug_log;

    private bool callback_headset_registered = false;

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
            scanner.LocationChanged += (o, ev) =>
            {
                _locationId = ev.LocationID;

                // Update the location ID in the headset object, which will be sent with pose updates to the server.
                if (_localHeadset is not null)
                {
                    EasyVizARHeadset headset = _localHeadset.GetComponent<EasyVizARHeadset>();
                    headset.LocationID = this.LocationID;
                }

                //The QR scanner code will also call the create all headsets funciton
                //I'm going to disabled this via boolean, but leave it here for now
                if (_shouldCreateHeadsets)
                {
                    CreateAllHeadsets();
                }
            };
        }

    }

    // Update is called once per frame
    void Update()
    {
        // added for displaying distance
        //DisplayHeadsetsDistance();

    }

    void OnEnable()
    {
        //Get the local headsetID if we've registered on the server

        //EasyVizARServer.Instance.TryGetHeadsetID(out string _local_headset_ID);
    }

    void OnDisable()
    {

    }

    public void HeadsetRegistrationCheck(String headset_ID)
    {
        EasyVizARServer.Instance.Get("headsets/" + headset_ID, EasyVizARServer.JSON_TYPE, RegisterCheckCallback);
    }

    void RegisterCheckCallback(string resultData)
    {
        // The result data can be many things
        // If there's no match, it will be a list of all the headsets
        // If there is a match it will be a single headset
        // I don't know how to figure that out. Perhaps a size based thing? but we
        // can't deserialze the JSON if we don't know what it is??
        if (resultData != "error")
        {
            EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);

            if (h.id == _local_headset_ID) callback_headset_registered = true;
        }
    }

    [ContextMenu("CreateAllHeadsets")]
    public void CreateAllHeadsets()
    {
        //Tryget HeadsetID looks to see if there's a registration file locally
        if (! EasyVizARServer.Instance.TryGetHeadsetID(out string _registered_headset_ID))
        {
            if (verbose_debug_log) Debug.Log("No Registration: " + _registered_headset_ID);
            EasyVizARHeadset new_registration = new EasyVizARHeadset();
            new_registration.LocationID = this.LocationID;
            new_registration.Name = _localHeadsetName;
            new_registration.CreateHeadsetToRegister();
        }
        else
        {
            if (verbose_debug_log) Debug.Log("Found Registration: " + _registered_headset_ID);
            //If there is a UID check it against the server


            //HeadsetRegistrationCheck(_registered_headset_ID);


            // I DONT THINK THIS WORKS< RACE CONDITION
            //BUT I CAN"T GET THE DATA OUTOF THE CALLBACK
            // I need the callback to finish before i can evaluate this, so maybe this has to be
            //in the callback???? but it's sooo messy with callbacks
            //If the UID is on the server, we set the local headset id to the regestered and\
            //just update its data. otherwise we will create a new headset
            
            // TODO This needs to be checked, this is never set to true
            if (callback_headset_registered)
            {
                _local_headset_ID = _registered_headset_ID;
            }
            else
            {
                EasyVizARHeadset new_registration = new EasyVizARHeadset();
                new_registration.LocationID = this.LocationID;
                new_registration.Name = _localHeadsetName;
                new_registration.CreateHeadsetToRegister();
            }
        }


       /* //Tryget HeadsetID looks to see if there's a registration file locally
        if (EasyVizARServer.Instance.TryGetHeadsetID(out string _registered_headset_ID))
        {
            _local_headset_ID = _registered_headset_ID;

            //check to see if the local headset ID in the registration file is on the server

        }
        //If not, we need to register to the server. Create a new headset
        else
        {
            EasyVizARHeadset new_registration = new EasyVizARHeadset();
            new_registration.LocationID = this.LocationID;
            new_registration.Name = _localHeadsetName;
            new_registration.CreateHeadsetToRegister();
        }*/

        if (EasyVizARServer.Instance.TryGetHeadsetID(out string new_registered_headset_ID))
        {
            _local_headset_ID = new_registered_headset_ID;

            if (!_headsetsCreated)
            {
                //CreateLocalHeadset();
                CreateHeadsets();

                _headsetsCreated = true;
            }
        }
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

                local_headset.CreateLocalHeadset(_localHeadsetName + "_" + s, _locationId, !_visualizePreviousLocal);
            }
            else
            {
                local_headset.CreateLocalHeadset(_localHeadsetName, _locationId, !_visualizePreviousLocal);
            }
            Debug.Log("Create local headset says this is the local headset name: " + local_headset.name);
        }
    }

    void OLD_CreateLocalHeadset()
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
    }


    // This is where we are instantiating the GameObject of Headset in Unity
    public void CreateRemoteHeadsetOLD(EasyVizAR.Headset remote_headset)
    {
        GameObject headset_game_object = Instantiate(_headsetPrefab, transform);
        headset_game_object.name = remote_headset.id;

        EasyVizARHeadset headset_class_data = headset_game_object.GetComponent<EasyVizARHeadset>();
        // Getting the reference for displaying the headset
        DistanceCalculation distance_calculation_script = headset_game_object.GetComponent<DistanceCalculation>();

        distance_calculation_script.map_parent = map_parent;
        distance_calculation_script.headset_name = remote_headset.name;
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

            DistanceCalculation distance_calculation_script = headset_game_object.GetComponent<DistanceCalculation>();

            distance_calculation_script.Initialize(remote_headset.name, map_parent);
        }
    }

    public void UpdateRemoteHeadset(string previous_id, EasyVizAR.Headset remoteHeadset)
    {
        EasyVizARHeadset matched_headset = _activeHeadsets.Find(find_headset => find_headset._headsetID == previous_id);

        if (matched_headset == null)
        {
            CreateRemoteHeadset(remoteHeadset);
        }
        else matched_headset.AssignValuesFromJson(remoteHeadset);

        /*foreach (var headset in _activeHeadsets)
        {
            // We should be matching on headset ID because names can change and
            // are also not guaranteed to be unique, though they should be.
            if (headset._headsetID == previous_id)
            {
                headset.AssignValuesFromJson(remoteHeadset);

                //Debug.Log("the id: " + hs.Name + " new color: " +  remoteHeadset.color);				
                return;
            }
        }
        // The updated headset was not in our list, so make a new one.
        CreateRemoteHeadset(remoteHeadset);
        */
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
            Debug.LogError("Error in data payload: " + result_data);
            return;
        }

        //parse list of headsets for this location and create...
        //the key to parsing the array - the text we add here has to match the name of the variable in the array wrapper class (headsets).
        EasyVizAR.HeadsetList headset_list = JsonUtility.FromJson<EasyVizAR.HeadsetList>("{\"headsets\":" + result_data + "}");

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
            DistanceCalculation distance_calculation_script = headset_game_object.GetComponent<DistanceCalculation>();

            distance_calculation_script.map_parent = map_parent;
            distance_calculation_script.headset_name = local_headset.name;

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
            //This is needed right now because the map parent is used to spawn the icon in the map, but 
            //it really shouldn't be there. it should probably be in this class or a view manager
            DistanceCalculation distance_calculation_script = headset_game_object.GetComponent<DistanceCalculation>();

            distance_calculation_script.map_parent = map_parent;
            distance_calculation_script.headset_name = local_headset.name;

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
    }

    void CreateHeadsets()
    {
        //list headsets from server for our location, create a prefab of each...
        EasyVizARServer.Instance.Get("headsets?location_id=" + _locationId, EasyVizARServer.JSON_TYPE, CreateHeadsetsCallback);
    }


}
