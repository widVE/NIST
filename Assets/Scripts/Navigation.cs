using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.CodeDom;
using System.Security.Cryptography;
//using UnityEngine.Debug;

[System.Serializable]
public class NavigationTargetUpdate
{
    public EasyVizAR.NavigationTarget navigation_target;
}


public class Navigation : MonoBehaviour
{
    GameObject markerSpawnParent = null;
    Collider collider;
    LineRenderer line;
    [SerializeField] Transform[] waypoints; //this contains the position of the landmark/icon
    public GameObject map_parent;
    
    // For querying the server 
    string location_id;
    EasyVizAR.Path path = new EasyVizAR.Path(); // this stores the path of points 
    GameObject main_cam;
    Transform feature;

    public string local_headset_id = "";
    public string last_target;

    // Start is called before the first frame update
    void Start()
    {
        markerSpawnParent = this.transform.parent.gameObject.GetComponent<MapIconSpawn>().feature_parent;
        map_parent = this.transform.parent.gameObject;
        last_target = map_parent.GetComponent<MapIconSpawn>().last_clicked_target;
        
        //markerSpawnParent = GameObject.Find("Marker Spawn Parent");

        if (!markerSpawnParent)
        {
            UnityEngine.Debug.Log("Navigation: cannot find the icon parent");
        }
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

    

    // Querying the server with path between two points
    public void FindPath() // Vector3 start, Vector3 target
    {
        //Vector3 start = new Vector3(2f,2f,4f); // this is hard coded for now --> will add these points later
        //Vector3 target = new Vector3(12f,-2f, 2f); // is the type Position? or Vector3?
        if (feature != null && !last_target.Equals(this.name)) // checking if the last target is the same as the newly selected target, if so, do query it again 
        {
            UnityEngine.Debug.Log("initiated querying path");
            Vector3 start = main_cam.transform.position;
            Vector3 target = feature.position; // is the type Position? or Vector3?
            //UnityEngine.Debug.Log("start: " + start.x + ", " + start.y + ", " + start.z);
            //UnityEngine.Debug.Log("target: " + target.x + ", " + target.y + ", " + target.z);
            
            //UnityEngine.Debug.Log("http://easyvizar.wings.cs.wisc.edu:5000/locations/" + location_id + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + target.x + "," + target.y + "," + target.z);
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
            //UnityEngine.Debug.Log("the path is: " + path.points);
            //UnityEngine.Debug.Log("location id: " + location_id);
            
            int cnt = 0;
            // making sure we are creating a new line
            if (line.positionCount > 0) line.positionCount = 0;
            EasyVizAR.Position target_pos = new EasyVizAR.Position();
            foreach (EasyVizAR.Position points in path.points)
            {
                line.positionCount++;
                line.SetPosition(cnt++, new Vector3(points.x, points.y, points.z)); // this draws the line 
                //UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
                //UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);
                target_pos = points;
            }


            UnityEngine.Debug.Log("Successfully added the points");


            // send the target information to the server
            NavigationTargetUpdate nav_update = new NavigationTargetUpdate();
            EasyVizAR.NavigationTarget target = new EasyVizAR.NavigationTarget();
            target.type = "feature";
            target.target_id = feature.Find("ID").GetChild(0).name;
            target.position = target_pos;
            // updating the last target here
            last_target = this.name;
            UnityEngine.Debug.Log("the last target: " + last_target);

            GameObject local = GameObject.Find("LocalHeadset");
            local_headset_id = local.transform.GetChild(0).name;
            nav_update.navigation_target = target;

            //UnityEngine.Debug.Log("returned json: " + JsonUtility.ToJson(target));
            EasyVizARServer.Instance.Patch("headsets/" + local_headset_id, EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(nav_update), PostTargetCallback);


        }

    }

    void PostTargetCallback(string resultData)
    {
        //Debug.Log(resultData);

        if (resultData != "error")
        {
            UnityEngine.Debug.Log("successfully patched the target to the server: " + resultData);
        }
        else
        {
            UnityEngine.Debug.Log("failed to patch the target to the server");

        }
    }

}
