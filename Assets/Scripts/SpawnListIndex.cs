using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpawnListIndex : MonoBehaviour
{
    public EasyVizARHeadsetManager manager;
    public FeatureManager feature_manager = new FeatureManager(); // added
    

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
        Debug.Log("feature manager" + feature_manager);
        if (!feature_manager.feature_gameobj_dictionary.ContainsValue(cloned_feature))
        {

            feature_manager.CreateNewFeature(index, cloned_feature);
            //for testing
           // feature_manager.UpateFeature(32, cloned_marker);
            // feature_manager.marker_list.Add();
        }

        /*
        //Create our feature to be sent via JSON

        EasyVizAR.Feature feature_to_post = new EasyVizAR.Feature();

      //feature_to_post.created = ((float)System.DateTime.Now.Hour + ((float)System.DateTime.Now.Minute*0.01f));
        //placeholder name of creator
        feature_to_post.createdBy = "Test Marker";
        //is the ID assigned by the server? we don't know
      // feature_to_post.id = 1;
        feature_to_post.name = "Hallway Fire";
        feature_to_post.position = cloned_marker.transform.position;
        
        //This doesn't work but why? The transform should be set, but it's realitve
        //feature_to_post.position = maker_to_spawn.transform.position;
        //We figured it out!! it's becase the marker to spawn is the template and not the actual game object
        //that gets made via instantiate. We now have a reference to it via the cloned_marker and so we can
        //access the correct transformation yey
        
        
        feature_to_post.type = "hazard";
        //This isn't correct right now. We need to have an update
        //call when the obejct in manipulated.
        
     //   feature_to_post.updated = ((float)System.DateTime.Now.Hour + ((float)System.DateTime.Now.Minute * 0.01f));

        //Serialize the feature into JSON
        var data = JsonUtility.ToJson(feature_to_post);

        //for sending stuff to the server 
        EasyVizARServer.Instance.Post("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, data, GetLocation);
        */

    }

}
