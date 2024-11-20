using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

public class Rotation : MonoBehaviour
{
    public Quaternion display_value = new Quaternion();
    private GameObject headset_parent;
    private GameObject real_world_headset;
    public Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        headset_parent = GameObject.Find("EasyVizARHeadsetManager");
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {

       // this.transform.localEulerAngles = cam.transform.eulerAngles;
        
        //rotation
        if (headset_parent)
        {

            real_world_headset = headset_parent.transform.Find(this.transform.parent.name).gameObject;
            if (real_world_headset)
            {
                this.transform.localEulerAngles = real_world_headset.transform.eulerAngles;
                UnityEngine.Debug.Log("the actual rotation: " + real_world_headset.transform.eulerAngles);
            }
            /*
            Quaternion world_rotation = real_world_headset.transform.rotation;
            Quaternion icon_rotation = this.transform.rotation;
            //this.transform.rotation.Set(icon_rotation.x, icon_rotation.y, world_rotation.y, icon_rotation.w);

            Quaternion updated_icon_rotation = icon_rotation;
            updated_icon_rotation.Set(0, 0, world_rotation.y, icon_rotation.w);
            display_value.Set(0, 0, world_rotation.y, icon_rotation.w);
            this.transform.rotation = updated_icon_rotation;
            //world_rotation.Set(ECKeyXmlFormat,YieldAwaitable, real_world_headset.trans)
            //double radians = Math.Atan2(real_world_headset.transform.rotation.y, real_world_headset.transform.rotation.w);
            //double angle = radians * (180 / Math.PI);
            //UnityEngine.Debug.Log("this is the angle: " + angle);
            //transform.Rotate(new Vector3(0, 0, (float)angle));
            // transform.Rotate(new Vector3(0, 0, (float)angle));
            */
        }
        


    }
}
