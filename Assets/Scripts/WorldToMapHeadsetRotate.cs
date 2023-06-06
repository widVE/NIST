using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldToMapHeadsetRotate : MonoBehaviour
{

    private GameObject headset_manager_GO = null;
    private EasyVizARHeadsetManager headset_manager_script = null;

    public GameObject map_spawn_target = null;

    public string headset_manager_name = "EasyVizARHeadsetManager";
    public bool verbose = false;

    // Start is called before the first frame update
    void Start()
    {
        headset_manager_GO = GameObject.Find(headset_manager_name);
        if (verbose) Debug.Log("LOOK AT " + headset_manager_GO);        
        
        headset_manager_script = headset_manager_GO.GetComponent<EasyVizARHeadsetManager>();

        if (headset_manager_script != null)
        {
            if (verbose) Debug.Log("Found Script!!!");
        }
        else
        {
            if (verbose) Debug.Log("No Manager");
        }
    }

    
    void Update()
    {
        //Look through all of the active worldspace headsets
        foreach (EasyVizARHeadset headset in headset_manager_script._activeHeadsets)
        {
            //If they are not the local headset, then copy their rotation from the world space into the map
            bool is_local_headset = headset.GetComponent<DistanceCalculation>().is_local;
            
            if (!is_local_headset)
            {
                //Get the child from the map with the same name as the world space headset
                GameObject map_headset_icon = map_spawn_target.transform.Find(headset.name).gameObject;

                if (map_headset_icon != null)
                {
                    Vector3 euler_rotation_source = new Vector3();

                    //euler_rotation_source.x = headset.transform.eulerAngles.x;
                    euler_rotation_source.y = headset.transform.eulerAngles.y;
                    //euler_rotation_source.z = headset.transform.eulerAngles.z;

                    map_headset_icon.transform.localEulerAngles = euler_rotation_source;
                }
                else
                {
                    Debug.LogWarning("Missing headset from map: " + headset.name);
                }
            }
        }
    }
}
