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

        PositionInfo marker_pos = new PositionInfo();
        marker_pos.name = "hazard";
        marker_pos.position = new Vector3(maker_to_spawn.GetComponent<PositionInfo>().position.x, maker_to_spawn.GetComponent<PositionInfo>().position.y, maker_to_spawn.GetComponent<PositionInfo>().position.z);
        var data = JsonUtility.ToJson(marker_pos);

        //for sending stuff to the server 
        EasyVizARServer.Instance.Post("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE,data,  GetLocation);

    }

}
