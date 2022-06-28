using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SpawnListIndex : MonoBehaviour
{
    public EasyVizARHeadsetManager manager;


    public List<GameObject> spawn_list = null;
    public GameObject spawn_root;
    public GameObject spawn_parent;
    public GameObject curr_headset;
    public float offset_distance_z = 1;

    [System.Serializable]
    public class PositionInfo
    {
        public string name;
        public Vector3 position;
    }

     void GetLocation(string result)
    {
        if (result != "error")
        {
            //Debug.Log(resultData);
        }
        else
        {
        }

    }

    public void spawnObjectAtIndex(int index)
    {
        GameObject maker_to_spawn = spawn_list[index];

        //Wow, this turns out to be really tricky wehn doing it via script. this was giving really weird
        //results because of the local vs world coordniate spaces I was thinking in. This code does
        //more to achive a shifted translation, that while relative to the camera, is always offset
        // in the z world direciton, not as I inteded as a scaled amount in front of the user
        //---Vector3 offset_position = spawn_root.transform.position + Vector3.forward*offset_distance_z;

        //Using a empty offset connected to the main camera as our spawn target works well and is easy to 
        //visualize in the editor

        Instantiate(maker_to_spawn, spawn_root.transform.position, spawn_root.transform.rotation, spawn_parent.transform);

        //Create our feature to be sent via JSON

        EasyVizAR.Feature feature_to_post = new EasyVizAR.Feature();

        feature_to_post.created = System.DateTime.Now;
        //placeholder name of creator
        feature_to_post.createdBy = "Test Marker";
        //is the ID assigned by the server? we don't know
        feature_to_post.id = 1;
        feature_to_post.name = "Hallway Fire";
        feature_to_post.position = maker_to_spawn.transform.position;
        feature_to_post.type "hazard";
        //This isn't correct right now. We need to have an update
        //call when the obejct in manipulated.
        feature_to_post.updated = System.DateTime.Now;

        //Serialize the feature into JSON
        var data = JsonUtility.ToJson(feature_to_post);

        //for sending stuff to the server 
        EasyVizARServer.Instance.Post("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE,data,  GetLocation);

    }

}
