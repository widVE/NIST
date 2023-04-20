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
    public string rotation_camera_name = "Main Camera";

    // Update is called once per frame
    void Update()
    {
        Vector3 euler_rotation_source = new Vector3();

        if(copy_x_rotation) euler_rotation_source.x = source_transform.transform.eulerAngles.x;
        if(copy_y_rotation) euler_rotation_source.y = source_transform.transform.eulerAngles.y;
        if(copy_z_rotation) euler_rotation_source.z = source_transform.transform.eulerAngles.z;

        this.transform.localEulerAngles = euler_rotation_source;
    }

    private void Start()
    {
        // We need to get the main camera only for the local headset
        // other headsets need to have their rotation defined by the server values
        if (find_main_camera) source_transform = GameObject.Find(rotation_camera_name);

    }
}
