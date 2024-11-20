using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpawnListIndex : MonoBehaviour
{
    public EasyVizARHeadsetManager manager;
    public FeatureManager feature_manager; // added


    public List<GameObject> spawn_list = null;
    public GameObject spawn_root;
    public GameObject spawn_parent;
    public float offset_distance_z = 1;

    public Dictionary<string, GameObject> feature_type_dictionary = new Dictionary<string, GameObject>(); // contains all possible marker objects


    //For feature type 
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


    // Start is called before the first frame update
    void Start()
    {
        //populating the feature types dictionary //TODO: change to lowercase
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
        
        // BS 11-21 I don't think this should be here anymoore? We want to list features after we scan the
        //QR code not at start of application. that is handeled in the feature manager
        //feature_manager.ListFeatures();
        
        
        //DisplayAllFeatureGameObjects();


    }

    void Update()
    {
        DisplayAllFeatureGameObjects();
    }


    

    public void spawnObjectAtIndex(string feature_type)
    {
        GameObject feature_to_spawn = feature_type_dictionary[feature_type];

        //Wow, this turns out to be really tricky wehn doing it via script. this was giving really weird
        //results because of the local vs world coordniate spaces I was thinking in. This code does
        //more to achive a shifted translation, that while relative to the camera, is always offset
        // in the z world direciton, not as I inteded as a scaled amount in front of the user
        //---Vector3 offset_position = spawn_root.transform.position + Vector3.forward*offset_distance_z;

        //Using a empty offset connected to the main camera as our spawn target works well and is easy to 
        //visualize in the editor

        GameObject cloned_feature = Instantiate(feature_to_spawn, spawn_root.transform.position, spawn_root.transform.rotation, spawn_parent.transform);
        //cloned_feature.AddComponent<MarkerObject>().index = index; // this might be useful for future if we want to change the object index
        Debug.Log("feature manager" + this.feature_manager);
        
        
        this.feature_manager.CreateNewFeature(feature_type, cloned_feature);
            //for testing
            // feature_manager.UpateFeature(32, cloned_marker);
            // feature_manager.marker_list.Add();
        

    }


    // Displaying all the features for all Hololens 
    [ContextMenu("DisplayAllFeatureGameObjects")]
    public void DisplayAllFeatureGameObjects()
    {
        
        
        Debug.Log("number of elements in dictionary: " + feature_manager.feature_dictionary.Count);

        foreach (EasyVizAR.Feature feature in this.feature_manager.feature_list.features)
        {
            // if (feature_manager.feature_dictionary.ContainsKey(feature.id)) continue;
            // display only the feature from the server that is not currently in your scene. 
            GameObject feature_object = feature_type_dictionary[feature.type];
                //GameObject marker_to_display = this.feature_manager.feature_gameobj_dictionary[feature.id];
            Instantiate(feature_object, new Vector3(feature.position.x, feature.position.y, feature.position.z), feature_object.transform.rotation, spawn_parent.transform);

            
        }
        
    }



}
