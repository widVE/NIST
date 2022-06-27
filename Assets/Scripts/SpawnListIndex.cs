using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnListIndex : MonoBehaviour
{
    public EasyVizARHeadsetManager manager;


    public List<GameObject> spawn_list = null;
    public GameObject spawn_root;
    public GameObject spawn_parent;
    public float offset_distance_z = 1;

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
        

    }

}
