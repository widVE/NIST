using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetRotation : MonoBehaviour
{
    public GameObject source_transform;

    public bool find_main_camera = true;
    public string rotation_camera_name = "Main Camera";

    // Update is called once per frame
    void Update()
    {
        this.transform.localEulerAngles = source_transform.transform.eulerAngles;
    }

    private void Start()
    {
        // We need to get the main camera only for the local headset
        // other headsets need to have their rotation defined by the server values
        if (find_main_camera) source_transform = GameObject.Find(rotation_camera_name);

    }
}
