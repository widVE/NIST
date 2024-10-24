using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetRotation : MonoBehaviour
{
    public GameObject source_transform;

    //Editor facing control variables
    public bool find_main_camera = true;
    public bool copy_x_rotation = false;
    public bool copy_y_rotation = true;
    public bool copy_z_rotation = false;
    public bool verbose = true;

    public string rotation_source_name = "Main Camera";
    public string headset_manager_name = "EasyVizARHeadsetManager";

    //We only want to grab our local camera rotation data if we are the local headset
    //the other map icon rotations should come from the server
    private bool local_headset = false;

    // Update is called once per frame
    void Update()
    {
        if (local_headset)
        {
            Vector3 euler_rotation_source = this.transform.localEulerAngles;

            if (copy_x_rotation) euler_rotation_source.x = source_transform.transform.eulerAngles.x;
            if (copy_y_rotation) euler_rotation_source.y = source_transform.transform.eulerAngles.y;
            if (copy_z_rotation) euler_rotation_source.z = source_transform.transform.eulerAngles.z;

            this.transform.localEulerAngles = euler_rotation_source;
        }
    }

    private void Start()
    {
        EasyVizARHeadsetManager headset_manager_script = EasyVizARHeadsetManager.EasyVizARManager.gameObject.GetComponent<EasyVizARHeadsetManager>();

        if (headset_manager_script != null)
        {
            if (verbose) Debug.Log("Found Manger");
        }
        else
        {
            if (verbose) Debug.Log("No Manager");
        }

        foreach (EasyVizARHeadset headset in headset_manager_script._activeHeadsets)
        {
            bool is_local_headset = headset.GetComponent<DistanceCalculation>().is_local;
            if (is_local_headset)
                //check against the name of the connected headset;
                if (this.transform.parent.name == headset.name)
                {
                    if (verbose) Debug.Log("Found local headset: " + this.transform.parent.name + " is active, and matches " + headset.name);
                    local_headset = true;
                    this.transform.Find("Star").gameObject.SetActive(true);
                }
        }

        // We need to get the main camera only for the local headset
        // other headsets need to have their rotation defined by the server values
        if (local_headset && find_main_camera) source_transform = GameObject.Find(rotation_source_name);

    }
}
