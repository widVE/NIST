using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
//using Color = UnityEngine.Color;


public class FeatureManager : MonoBehaviour
{

    public EasyVizARHeadsetManager manager;
    // key as id, value as the GameObject (the marker placed)
    public Dictionary<int, EasyVizAR.Feature> feature_dictionary = new Dictionary<int, EasyVizAR.Feature>(); // TODO: can delete this later after more integration

    // Each GameObject now contains a field call obj_feature (in the script MarkerObject.cs) so that feature is now one of the fields of the GameObject 
    //public Dictionary<int, GameObject> feature_gameobj_dictionary = new Dictionary<int, GameObject>(); // a seperate dictionary for keeping track of Gameobject in the scene
    
    
    public EasyVizAR.FeatureList feature_list = new EasyVizAR.FeatureList();
    public EasyVizAR.Feature featureHolder = null;
    public GameObject markerHolder = null;
    public int featureID = 32; // also a temporary holder
    //for updates 
    public string color = "";
    public string name = "";
    public EasyVizAR.Position new_position;


    //Added from SpawnListIndex
    public GameObject spawn_root;
    public GameObject spawn_parent;
    public Dictionary<string, GameObject> feature_type_dictionary = new Dictionary<string, GameObject>(); // contains all possible marker objects
    public bool isChanged = true;
    public int curr_list_size = 0;
    // Feature objects
    public GameObject ambulance_icon;
    public GameObject audio_icon;
    public GameObject bad_person_icon;
    public GameObject biohazard_icon;
    public GameObject door_icon;
    public GameObject elevator_icon;
    public GameObject exit_icon;
    public GameObject extinguisher_icon;
    public GameObject fire_icon;
    public GameObject headset_icon;
    public GameObject injury_icon;
    public GameObject message_icon;
    public GameObject object_icon;
    public GameObject person_icon;
    public GameObject radiation_icon;
    public GameObject stairs_icon;
    public GameObject user_icon;
    public GameObject warning_icon;

    //distance 
    [SerializeField]
    public GameObject distance_text;
    public float x_distance;
    public float z_distance;
    public GameObject curr_headset;
    public Vector3 headsetPos;
    public bool distance_updated;
    public GameObject distance_parent;
    public bool isFeet;
    public Vector3 oldPos;
    public Vector3 newPos;

    string location_id = "";

    // Attach QRScanner GameObject so we can listen for location change events.
    [SerializeField]
    GameObject _qrScanner;

    // Start is called before the first frame update

    void Start()
    {

        //Added from SpawnListIndex
        //populating the feature types dictionary 
        feature_type_dictionary.Add("ambulance", ambulance_icon);
        feature_type_dictionary.Add("audio", audio_icon);
        feature_type_dictionary.Add("bad-person", bad_person_icon);
        feature_type_dictionary.Add("biohazard", biohazard_icon);
        feature_type_dictionary.Add("door", door_icon);
        feature_type_dictionary.Add("elevator", elevator_icon);
        feature_type_dictionary.Add("exit", exit_icon);
        feature_type_dictionary.Add("extinguisher", extinguisher_icon);
        feature_type_dictionary.Add("fire", fire_icon);
        feature_type_dictionary.Add("headset", headset_icon);
        feature_type_dictionary.Add("injury", injury_icon);
        feature_type_dictionary.Add("message", message_icon);
        feature_type_dictionary.Add("object", object_icon);
        feature_type_dictionary.Add("person", person_icon);
        feature_type_dictionary.Add("radiation", radiation_icon);
        feature_type_dictionary.Add("stairs", stairs_icon);
        feature_type_dictionary.Add("user", user_icon);
        feature_type_dictionary.Add("warning", warning_icon);
        // distance 
        isFeet = true;  // should change in the future, but by default it's shown in ft.

        headsetPos = curr_headset.GetComponent<Transform>().position;
        oldPos = headsetPos;
        distance_updated = true;


#if UNITY_EDITOR
        // In editor mode, use the hard-coded location ID for testing.
        ListFeatures(); // this populates all the features listed on the server currently
#endif

        // Wait for a QR code to be scanned to fetch features from the correct location.
        if (_qrScanner)
        {
            var scanner = _qrScanner.GetComponent<QRScanner>();
            scanner.LocationChanged += (o, ev) =>
            {
                ListFeaturesFromLocation(ev.LocationID);
                location_id = ev.LocationID;
               
            };
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*
        // We should only need to call ListFeatures once on entering a new location.
        // After that, we can update the existing feature list from websocket events.
        if (false && isChanged)
        {
            Debug.Log("reached Update()");
            ListFeatures();
        }
        */
                
    }
    // POST 
    public void CreateNewFeature(string feature_type, GameObject marker) //TODO: change the feature_type from int to string
    {
        EasyVizAR.Feature feature_to_post = new EasyVizAR.Feature();
      
        feature_to_post.createdBy = manager.LocationID;

        EasyVizAR.Position position = new EasyVizAR.Position();
        position.x = (float)marker.transform.position.x;
        position.y = (float)marker.transform.position.y;
        position.z = (float)marker.transform.position.z;
        feature_to_post.position = position;
        feature_to_post.name = feature_type;
        // feature_to_post.type = feature_type.ToLower();
        feature_to_post.type = feature_type;

        EasyVizAR.FeatureDisplayStyle style = new EasyVizAR.FeatureDisplayStyle();
        style.placement = "point";
        feature_to_post.style = style;

        
        //Serialize the feature into JSON
        var data = JsonUtility.ToJson(feature_to_post);
        // Debug.Log("Json utility: " + feature_to_post.id);
        // Debug.Log("locations/" + manager.LocationID + "/features");
        //for sending stuff to the server 

        EasyVizARServer.Instance.Post("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, data, delegate (string result)
        {
            // Pass the relevant marker GameObject to the callback so that it can be updated.
            PostFeature(result, marker);
        });
   
        featureHolder = feature_to_post;
        markerHolder = marker;     
    }

    // a callback function
    void PostFeature(string result, GameObject marker)
    {
        if (result != "error")
        {
            var resultJSON = JsonUtility.FromJson<EasyVizAR.Feature>(result);

            // Remove the temporary local marker and add a new one with a valid ID.
            Destroy(marker);
            UpdateFeatureFromServer(resultJSON);

            //feature_dictionary.Add(resultJSON.id, featureHolder);
            //marker.name = string.Format("feature-{0}", resultJSON.id);
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }
        

    }

    [ContextMenu("GetFeature")]
    public void GetFeature() //takes in id as parameter, for some reason Unity doesn't accept that convention
    {
        var id = featureID;
        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, GetFeatureCallBack);


    }

    void GetFeatureCallBack(string result)
    {
        var resultJSON = JsonUtility.FromJson<EasyVizAR.Feature>(result);

        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);

        }
        else
        {
            Debug.Log("ERROR: " + result);
        }
    }

    //NOTE: this function is now also displaying all the features listed on the server.
    [ContextMenu("ListFeatures")]
    public void ListFeatures() 
    {
        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, ListFeatureCallBack);
        Debug.Log("ListFeatures Called");
    }

    public void ListFeaturesFromLocation(string locationID)
    {
        EasyVizARServer.Instance.Get("locations/" + locationID + "/features", EasyVizARServer.JSON_TYPE, ListFeatureCallBack);
    }
    
    void ListFeatureCallBack(string result) {
        if (result != "error")
        {
            foreach (EasyVizAR.Feature feature in feature_list.features)
            {
                DeleteFeatureFromServer(feature.id);
            }

            this.feature_list = JsonUtility.FromJson<EasyVizAR.FeatureList> ("{\"features\":" + result + "}");
            
            //Debug.Log("feature_list length: " + feature_list.features.Length);
            
            foreach (EasyVizAR.Feature feature in feature_list.features)
            {
                // This will add the feature if it is new or update an existing one.
                UpdateFeatureFromServer(feature);
            }

            //disabling the Update()
            isChanged = false;
           
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }
    }

    [ContextMenu("UpdateFeatureTest")]
    public void UpdateFeatureTest()
    {
        var id = featureID; // parameter 
        UpdateFeature(id);
    }

    public void UpdateFeature(int id) //    public void UpateFeature(int id, GameObject new_feature)
    {
        // ListFeatures();

        //Debug.Log("contain id?: " + feature_dictionary.ContainsKey(featureID));
        //Debug.Log("length of feature_list: " + this.feature_list.features.Length);

        //var id = featureID; // parameter 
        var new_feature = markerHolder; // parameter
        // the following feature will be specified by user
        
        if (this.feature_dictionary.ContainsKey(id))
        {
            Debug.Log("in the if statement");
            //creating new feature
            //EasyVizAR.Feature feature_to_patch = feature_gameobj_dictionary[id].GetComponent<MarkerObject>().feature;
            EasyVizAR.Feature feature_to_patch = feature_dictionary[id];

            if (color.Length != 0)
            {
                feature_to_patch.color = color;
            }

            if (name.Length != 0)
            {
                feature_to_patch.name = name;
                feature_to_patch.type = name;
            }

            Debug.Log("feature name: " + feature_to_patch.name);
            //eature_to_patch.name = "Updated name: ";
            // Main: updating the position

            //Find the feature in the scene to get the game object's position
            Transform feature_object_transform = spawn_parent.transform.Find(string.Format("feature-{0}", id));

            EasyVizAR.Position position = new EasyVizAR.Position();

            position.x = feature_object_transform.position.x;
            position.y = feature_object_transform.position.y;
            position.z = feature_object_transform.position.z;

            feature_to_patch.position = position;


            // TODO: might want to modify the style?
            //feature_to_patch.style.placement = "point";

            //Serialize the feature into JSON
            var data = JsonUtility.ToJson(feature_to_patch);


            // updates the dictionary
            featureHolder = feature_to_patch;
            featureID = id;
            EasyVizARServer.Instance.Patch("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, data, UpdateFeatureCallback);

        }

    }

    void UpdateFeatureCallback(string result)
    {
        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);

            feature_dictionary[featureID] = featureHolder;
            // updates the dictionary
            //Destroy(feature_gameobj_dictionary[featureID].GetComponent<MarkerObject>());
            //feature_gameobj_dictionary[featureID].AddComponent<MarkerObject>().feature = featureHolder;
            ListFeaturesFromLocation(location_id);
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }

    }


    //[ContextMenu("DeleteFeature")]
    public void DeleteFeature(int id)
    {
       //var id = featureID; //parameter 

        
        if (feature_dictionary.ContainsKey(id))
        {
            EasyVizAR.Feature delete_feature = feature_dictionary[id];
            var data = JsonUtility.ToJson(delete_feature);
            EasyVizARServer.Instance.Delete("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, data, DeleteFeatureCallBack);
        }
    }

    void DeleteFeatureCallBack(string result)
    {
        if (result != "error")
        {
            var resultJSON = JsonUtility.FromJson<EasyVizAR.Feature>(result);
            DeleteFeatureFromServer(resultJSON.id);
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }

    }

    //Added from SpawnListIndex
    public void spawnObjectAtIndex(string feature_type)
    {
        GameObject feature_to_spawn = feature_type_dictionary[feature_type];
        // billboarding effect
        GameObject cloned_feature = Instantiate(feature_to_spawn, spawn_root.transform.position, spawn_root.transform.rotation, spawn_parent.transform);

       // GameObject cloned_feature = Instantiate(feature_to_spawn, spawn_root.transform.position, spawn_root.transform.rotation, spawn_parent.transform);
        cloned_feature.name = "feature-local";
        CreateNewFeature(feature_type, cloned_feature);

    }

    // this is simply for convenience used if you want all the markers in the scene to disappear 
    [ContextMenu("DeleteAll")]
    void DeleteAll()
    {
        foreach (EasyVizAR.Feature feature in feature_list.features)
        {
            DeleteFeature(feature.id);
            feature_dictionary.Remove(feature.id);
        }
    }

    public void AddFeatureFromServer(EasyVizAR.Feature feature)
    {
        feature_dictionary.Add(feature.id, feature);

        GameObject feature_to_spawn;
        if (feature_type_dictionary.ContainsKey(feature.type))
        {
            feature_to_spawn = feature_type_dictionary[feature.type];
        } 
        else
        {
            Debug.Log("Feature type dictionary does not contain " + feature.type);
            feature_to_spawn = warning_icon;
        }

        Vector3 pos = Vector3.zero;
        pos.x = feature.position.x;
        pos.y = feature.position.y;
        pos.z = feature.position.z;

        GameObject marker = Instantiate(feature_to_spawn, pos, spawn_root.transform.rotation, spawn_parent.transform);
        marker.name = string.Format("feature-{0}", feature.id);
        
        Color myColor;
        if (ColorUtility.TryParseHtmlString(feature.color, out myColor))
        {
            marker.transform.Find("Quad").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);
           //marker.transform.Find("Quad").GetComponent<Renderer>().material.color = myColor;
        
        }
        

        MarkerObject new_marker_object = marker.GetComponent<MarkerObject>();
        if (new_marker_object is not null)
        {
            new_marker_object.feature_ID = feature.id;
            new_marker_object.manager_script = this;
        } else
        {
            Debug.Log("Warning: MarkerObject component is missing");
        }

        
    }

    public void UpdateFeatureFromServer(EasyVizAR.Feature feature)
    {
        // It is easiest to delete and recreate the feature, because
        // its display settings such as the feature type may have changed.
        DeleteFeatureFromServer(feature.id);
        AddFeatureFromServer(feature);
        isChanged = true;
    }

    public void DeleteFeatureFromServer(int id)
    {
        if (feature_dictionary.ContainsKey(id))
        {
            feature_dictionary.Remove(id);
        }

        var feature_object = spawn_parent.transform.Find(string.Format("feature-{0}", id));
        if (feature_object)
        {
            Destroy(feature_object.gameObject);
        }
    }
    
  
    // might implement in the future but might not need it 
    public void ReplaceFeature()
    {

    }

    void ReplaceFeatureCallback()
    {

    }
    
}
