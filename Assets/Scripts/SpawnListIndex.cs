using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpawnListIndex : MonoBehaviour
{
    public EasyVizARHeadsetManager manager;
    public FeatureManager feature_manager;// = new FeatureManager(); // added


    public List<GameObject> spawn_list = null;
    public GameObject spawn_root;
    public GameObject spawn_parent;
    public float offset_distance_z = 1;

    /*
    void GetLocation(string result)
    {
        if (result != "error")
        {
            Debug.Log(result);
        }
        else
        {
        }

    }
    */

    public void spawnObjectAtIndex(int index)
    {
        GameObject feature_to_spawn = spawn_list[index];
        
        //Wow, this turns out to be really tricky wehn doing it via script. this was giving really weird
        //results because of the local vs world coordniate spaces I was thinking in. This code does
        //more to achive a shifted translation, that while relative to the camera, is always offset
        // in the z world direciton, not as I inteded as a scaled amount in front of the user
        //---Vector3 offset_position = spawn_root.transform.position + Vector3.forward*offset_distance_z;

        //Using a empty offset connected to the main camera as our spawn target works well and is easy to 
        //visualize in the editor

        GameObject cloned_feature = Instantiate(feature_to_spawn, spawn_root.transform.position, spawn_root.transform.rotation, spawn_parent.transform);
        cloned_feature.AddComponent<MarkerObject>().index = index; // this might be useful for future if we want to change the object index
        Debug.Log("feature manager" + this.feature_manager);
        if (!this.feature_manager.feature_gameobj_dictionary.ContainsValue(cloned_feature))
        {

            this.feature_manager.CreateNewFeature(index, cloned_feature);
            //for testing
           // feature_manager.UpateFeature(32, cloned_marker);
            // feature_manager.marker_list.Add();
        }

    }

    // Displaying all the features for all Hololens 
    [ContextMenu("DisplayAllFeatureGameObjects")]
    public void DisplayAllFeatureGameObjects()
    {
        this.feature_manager.ListFeatures(); // populating the feature_list 
        Debug.Log("contain 64?: " + this.feature_manager.feature_gameobj_dictionary.ContainsKey(64));
        Debug.Log("number of elements in dictionary: " + this.feature_manager.feature_gameobj_dictionary.Count);

        foreach (EasyVizAR.Feature feature in this.feature_manager.feature_list.features)
        {
            
                GameObject marker_to_display = this.feature_manager.feature_gameobj_dictionary[feature.id];
                //Instantiate(marker_to_display, feature.position, marker_to_display.transform.rotation, spawn_parent.transform);

            
        }
        
    }



}
