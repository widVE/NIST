using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.CodeDom;
//using UnityEngine.Debug;

public class Navigation : MonoBehaviour
{
    GameObject markerSpawnParent = null;
    Collider collider;
    LineRenderer line;
    [SerializeField] Transform[] waypoints; //this contains the position of the landmark/icon
    
    // For querying the server 
    string location_id;
    EasyVizAR.Path path = new EasyVizAR.Path();
    GameObject main_cam;
    Transform feature;
   // [SerializeField] Vector3[] points; //this contains the position of the landmark/icon

    // Start is called before the first frame update
    void Start()
    {
        markerSpawnParent = this.transform.parent.gameObject.GetComponent<MapIconSpawn>().feature_parent;
        //markerSpawnParent = GameObject.Find("Marker Spawn Parent");

        if (!markerSpawnParent)
        {
            UnityEngine.Debug.Log("Navigation: cannot find the icon parent");
        }
        //collider = this.GetComponent<Collider>();
        //The prefabs aren't the same anymore so we will need to restructure them, for now
        //we will have two different ways to assign the variable
        //collider = this.transform.Find("Icon Visuals").GetComponent<Collider>();
        //if (collider == null) collider = this.transform.Find("Quad").GetComponent<Collider>();

        //set the collider active --> TODO: might not need this anymore
        //collider.enabled = true;
        // initializing the line render
        line = GameObject.Find("Main Camera").GetComponent<LineRenderer>();
        if (line != null) { UnityEngine.Debug.Log("found line renderer!"); }
        line.positionCount = 0; // this is hard coded for now.
        waypoints = new Transform[2];


        location_id = GameObject.Find("EasyVizARHeadsetManager").GetComponent<EasyVizARHeadsetManager>().LocationID;

        main_cam = GameObject.Find("Main Camera");
        //UnityEngine.Debug.Log("the type of icon: " + this.transform.Find("type").GetChild(0).name);
        if (this.transform.Find("type").GetChild(0).name != "Headset")
        {
            feature = markerSpawnParent.transform.Find(this.name);
            // Testing Querying path
            //FindPath();
        }

    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (waypoints[1] != null)
        {
            line.enabled = true;
            // disable line renderer once approach the target
            RenderNavigationPath();

        }
        */

    }

    public void RenderNavigationPath()
    {
        //TODO: this is where I should add the line render between the user and the desire landmark/icon
        //Debug.Log("Touched the icon!");
       line.positionCount = 2; // this is hard coded for now.

        waypoints[0] = Camera.main.transform; // this is not used currently
        // newly added
        Transform cam_pos = Camera.main.transform;
        float cam_pos_y_offset = cam_pos.position.y *0.5f; //NOTE (0.5f): might change this later in the future --> if need to lower it substantially
        Vector3 camera = new Vector3(cam_pos.position.x, cam_pos_y_offset, cam_pos.position.z);

        waypoints[1] = markerSpawnParent.transform.Find(this.name); // might move this line to elsewhere, but for now, it should work fine

        //line.SetPosition(0, points[0].position); // the position of the user (i.e. the main camera's position)
        line.SetPosition(0, camera); // the position of the user (i.e. the main camera's position)
        if (waypoints[1])
        {
            //float waypoints_y_offset = waypoints[1].position.y*0.75f;
           // line.SetPosition(1, new Vector3(waypoints[1].position.x, waypoints_y_offset, waypoints[1].position.z)); // the position of the desire landmark/icon
            line.SetPosition(1, waypoints[1].position); // the position of the desire landmark/icon
            UnityEngine.Debug.Log("line count: " + line.positionCount);
            /*
            // when successfully navigated to the target, disable line renderer --> maybe need these later
            if ((Math.Abs(camera.x - waypoints[1].position.x) < 0.05) || ((Math.Abs(camera.z - waypoints[1].position.z) < 0.05))) //TODO: need to fix it! 
            {
                Debug.Log("Disabled the line.");
                line.enabled = false;
                waypoints[1] = null;
                line.positionCount = 0;
            }
            */
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
            //Debug.Log("Disabled the line");
            line.enabled = false;
            waypoints[1] = null;
            line.positionCount = 0;
        }

    }

    // Querying the server with path between two points
    public void FindPath() // Vector3 start, Vector3 target
    {
        //Vector3 start = new Vector3(2f,2f,4f); // this is hard coded for now --> will add these points later
        //Vector3 target = new Vector3(12f,-2f, 2f); // is the type Position? or Vector3?
        if (feature != null)
        {
            UnityEngine.Debug.Log("initiated querying path");
            Vector3 start = main_cam.transform.position;
            Vector3 target = feature.position; // is the type Position? or Vector3?
            UnityEngine.Debug.Log("start: " + start.x + ", " + start.y + ", " + start.z);
            UnityEngine.Debug.Log("target: " + target.x + ", " + target.y + ", " + target.z);

            EasyVizARServer.Instance.Get("locations/" + location_id + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + target.x + "," + target.y + "," + target.z, EasyVizARServer.JSON_TYPE, GetPathCallback);

        }        
    }

    void GetPathCallback(string result)
    {
        //Debug.Log("initiated querying path");
        //Debug.Log("the result: " + result);
        if (result != "error")
        {
            UnityEngine.Debug.Log("path callback: " + result);
            // defining the length of the waypoints
            //waypoints = new Transform
            path = JsonUtility.FromJson<EasyVizAR.Path>("{\"points\":" + result + "}");
            UnityEngine.Debug.Log("the path is: " + path.points);
            UnityEngine.Debug.Log("location id: " + location_id);
            int cnt = 0;
            // making sure we are creating a new line
            if (line.positionCount > 0) line.positionCount = 0;
            foreach (EasyVizAR.Position points in path.points)
            {
                line.positionCount++;
                line.SetPosition(cnt++, new Vector3(points.x, points.y, points.z)); // this draws the line 
                UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
                UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);

            }

           
            UnityEngine.Debug.Log("Successfully added the points");
           
        }

    }
}
