using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Navigation : MonoBehaviour
{
    GameObject markerSpawnParent = null;
    Collider collider;
    LineRenderer line;
    [SerializeField] Transform[] points; //this contains the position of the landmark/icon
    // Start is called before the first frame update
    void Start()
    {
        markerSpawnParent = this.transform.parent.gameObject.GetComponent<MapIconSpawn>().feature_parent;
        //markerSpawnParent = GameObject.Find("Marker Spawn Parent");

        if (!markerSpawnParent)
        {
            Debug.Log("Navigation: cannot find the icon parent");
        }
        //collider = this.GetComponent<Collider>();
        collider = this.transform.Find("Quad").gameObject.GetComponent<Collider>();

        //set the collider active --> TODO: might not need this anymore
        collider.enabled = true;
        // initializing the line render
        line = GameObject.Find("Main Camera").GetComponent<LineRenderer>();
        if (line != null) { Debug.Log("found line renderer!"); }
        line.positionCount = 0; // this is hard coded for now.
        points = new Transform[2];
    }

    // Update is called once per frame
    void Update()
    {
        
        if (points[1] != null)
        {
            line.enabled = true;
            // disable line renderer once approach the target
            RenderNavigationPath();

        }
        

        Debug.Log("Got to Update() from Navigation");
        //update the position of user and landmark 
        /*
        points[0] = Camera.main.transform;
        points[1] = markerSpawnParent.transform.Find(this.name); // might move this line to elsewhere, but for now, it should work fine

        line.SetPosition(0, points[0].position); // the position of the user (i.e. the main camera's position)
        line.SetPosition(1, points[1].position); // the position of the desire landmark/icon
        */

    }

    public void RenderNavigationPath()
    {
        //TODO: this is where I should add the line render between the user and the desire landmark/icon
        Debug.Log("Touched the icon!");
        line.positionCount = 2; // this is hard coded for now.

        points[0] = Camera.main.transform; // this is not used currently
        // newly added
        Transform cam_pos = Camera.main.transform;
        float cam_pos_y_offset = cam_pos.position.y - 0.075f; //NOTE: might change this later in the future
        Vector3 camera = new Vector3(cam_pos.position.x, cam_pos_y_offset, cam_pos.position.z);

        points[1] = markerSpawnParent.transform.Find(this.name); // might move this line to elsewhere, but for now, it should work fine

        //line.SetPosition(0, points[0].position); // the position of the user (i.e. the main camera's position)
        line.SetPosition(0, camera); // the position of the user (i.e. the main camera's position)
        if (points[1])
        {
            line.SetPosition(1, points[1].position); // the position of the desire landmark/icon

            // when successfully navigated to the target --> maybe need these later

            if ((Math.Abs(camera.x - points[1].position.x) < 0.05) || ((Math.Abs(camera.z - points[1].position.z) < 0.05))) //TODO: need to fix it! 
            {
                Debug.Log("Disabled the line.");
                line.enabled = false;
                points[1] = null;
                line.positionCount = 0;
            }

        }


    }
    // This is for the future, if we would want to turn certain marker's line renderer off
    public void DisableLineRenderer(EasyVizAR.Feature marker, string type)
    {
        Vector3 cam_pos = GetComponent<Camera>().transform.position;

        float change_x = (float)Math.Pow((cam_pos.x - marker.position.x), 2);
        float change_z = (float)Math.Pow((cam_pos.z - marker.position.z), 2);
        float change_dist = (float)Math.Sqrt(change_x + change_z);
        if (change_dist < 0.05)
        {
            Debug.Log("Disabled the line");
            line.enabled = false;
            points[1] = null;
            line.positionCount = 0;
        }

    }
}
