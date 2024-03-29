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
    LineRenderer world_line;
    LineRenderer map_line;

    [SerializeField] Transform[] waypoints; //this contains the position of the landmark/icon
    public GameObject map_parent;
    
    // For querying the server 
    string location_id;
    EasyVizAR.Path path = new EasyVizAR.Path(); // this stores the path of points 
    GameObject main_camera;
    Transform feature;

    [SerializeField] string local_headset_id = "";
    public string last_target;


    //MAGIC NUMBER Offseting the line render path 20cm above the mesh below it
    [SerializeField] float raycast_floorpath_offset = 0.2f;

    // This holds the position data of the user's target
    EasyVizAR.Position user_target_position = new EasyVizAR.Position();

    // Start is called before the first frame update
    void Start()
    {
        markerSpawnParent = this.transform.parent.gameObject.GetComponent<MapIconSpawn>().feature_parent;
        
        //IDK if this is a safe assumption -B
        map_parent = this.transform.parent.gameObject;
        last_target = map_parent.GetComponent<MapIconSpawn>().last_clicked_target;

        local_headset_id = "BRAWNDO!";

        //markerSpawnParent = GameObject.Find("Marker Spawn Parent");

        if (!markerSpawnParent)
        {
            UnityEngine.Debug.Log("Navigation: cannot find the icon parent");
        }
        // initializing the line render
        world_line = GameObject.Find("Main Camera").GetComponent<LineRenderer>();
        map_line = GameObject.Find("Map Path View").GetComponent<LineRenderer>();

        if (world_line != null) UnityEngine.Debug.Log("found line renderer!"); 
        
        world_line.positionCount = 0; // this is hard coded for now.
        waypoints = new Transform[2];

        // I think this can be accessed by the singleton class reference, but how to use that? B
        location_id = GameObject.Find("EasyVizARHeadsetManager").GetComponent<EasyVizARHeadsetManager>().LocationID;

        main_camera = GameObject.Find("Main Camera");
        //UnityEngine.Debug.Log("the type of icon: " + this.transform.Find("type").GetChild(0).name);
        if (this.transform.Find("type").GetChild(0).name != "Headset")
        {
            feature = markerSpawnParent.transform.Find(this.name);
            // Testing Querying path
            //FindPath();
        }

    }

    // Querying the server with path between two points
    public void FindPath() // Vector3 start, Vector3 target
    {
        //Vector3 start = new Vector3(2f,2f,4f); // this is hard coded for now --> will add these points later
        //Vector3 target = new Vector3(12f,-2f, 2f); // is the type Position? or Vector3?
        if (feature != null && !last_target.Equals(this.name)) // checking if the last target is the same as the newly selected target, if so, do query it again 
        {
            UnityEngine.Debug.Log("initiated querying path");
            Vector3 start = main_camera.transform.position;

            //The destination
            Vector3 destination_position = feature.position; // is the type Position? or Vector3?
            user_target_position.x = destination_position.x;
            user_target_position.y = destination_position.y;
            user_target_position.z = destination_position.z;


            //UnityEngine.Debug.Log("start: " + start.x + ", " + start.y + ", " + start.z);
            //UnityEngine.Debug.Log("target: " + target.x + ", " + target.y + ", " + target.z);

            //UnityEngine.Debug.Log("http://easyvizar.wings.cs.wisc.edu:5000/locations/" + location_id + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + target.x + "," + target.y + "," + target.z);
            EasyVizARServer.Instance.Get("locations/" + location_id + "/route?from=" + start.x + "," + start.y + "," + start.z + "&to=" + destination_position.x + "," + destination_position.y + "," + destination_position.z, EasyVizARServer.JSON_TYPE, GetPathCallback);
        }        
    }


    //MapPath will request the path from the server
    // ***this isn't working right now
    public void RenderMapPath()
    {
        // Create a new line by deleting old points
        map_line.positionCount = 0;
        //map_line.positionCount = path.points.Length;

        //??? B -> Something to do with the update new target for navigation?
        //EasyVizAR.Position target_pos = new EasyVizAR.Position();

        for (int i = 0; i < path.points.Length; i++)
        {
            map_line.positionCount++;
            map_line.SetPosition(i, new Vector3(path.points[i].x, 0, path.points[i].z)); // this draws the line 
            //UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
            //UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);
            
            //??? B
            //target_pos = path.points[i];
        }

        UnityEngine.Debug.Log("Successfully added the map path line");
    }

    void RenderWorldPath()
    {   
        // Create a new line by deleting old points
        world_line.positionCount = 0;

        int index = 0;

        foreach (EasyVizAR.Position point in path.points)
        {
            world_line.positionCount++;
            world_line.SetPosition(index++, new Vector3(point.x, point.y, point.z)); // this draws the line 
                                                                                  //UnityEngine.Debug.Log("number of points in the path is: " + line.positionCount);
                                                                                  //UnityEngine.Debug.Log("points: " + points.x + ", " + points.y + ", " + points.z);
            user_target_position = point;
        }


        UnityEngine.Debug.Log("Successfully added world path line");
    }

    void RenderWorldPathRaycastFloor()
    {
        Ray surface_ray;
        RaycastHit hit;

        // Create a new line by deleting old points
        world_line.positionCount = 0;

        //UnityEngine.Debug.Log("World path size : " + path.points.Length);


        for (int i = 0; i < path.points.Length; i++)
        {
            //Allocate space in the line render vertex array
            world_line.positionCount++;

            EasyVizAR.Position EV_point = path.points[i];
            Vector3 point = new Vector3(EV_point.x, EV_point.y, EV_point.z);

            surface_ray = new Ray(point, Vector3.down);

            if (Physics.Raycast(surface_ray, out hit))
            {
                world_line.SetPosition(i, new Vector3(point.x, hit.point.y + raycast_floorpath_offset, point.z));
                //UnityEngine.Debug.Log(hit.point);
            }

            else
            {
                //UnityEngine.Debug.Log("Added world path floor raycast line index : " + i);
                world_line.SetPosition(i, new Vector3(point.x, point.y + raycast_floorpath_offset, point.z));
            }

            user_target_position = EV_point;
        }

        UnityEngine.Debug.Log("Successfully added world path floor raycast line");
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

            
            
            //RenderWorldPath();

            RenderWorldPathRaycastFloor();

            //SphereBreadcrumbs();

            RenderMapPath();


            // send the target information to the server
            // Why do we need to patch the server? should this be in a different function? B
            // The navigation path is getting called, but I've broken some of the target seleciton logic. Now it doesn't compose
            // the correct JSON for the server. I think it's because the target variable isn't updated correctly?
            // I think maybe it's working?? 

            // The Navigation Target Update is a piece of a headset update that is required structure for patching
            NavigationTargetUpdate navigation_target_update = new NavigationTargetUpdate();

            EasyVizAR.NavigationTarget user_navigation_target = new EasyVizAR.NavigationTarget();
            user_navigation_target.type = "feature";            
            user_navigation_target.target_id = feature.Find("ID").GetChild(0).name;
            user_navigation_target.position = user_target_position;

            // updating the last target here
            last_target = this.name;
            UnityEngine.Debug.Log("the last target: " + last_target + " with ID " + user_navigation_target.target_id);

            GameObject local = GameObject.Find("LocalHeadset");
            navigation_target_update.navigation_target = user_navigation_target;

            UnityEngine.Debug.Log("returned json: headset id " + local_headset_id + " of type " + EasyVizARServer.JSON_TYPE + " the payload " + JsonUtility.ToJson(navigation_target_update));
            EasyVizARServer.Instance.Patch("headsets/" + local_headset_id, EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(navigation_target_update), PostTargetCallback);


        }

    }

    void SphereBreadcrumbs()
    {
        foreach (EasyVizAR.Position EV_point in path.points)
        {
            Vector3 point = new Vector3(EV_point.x, EV_point.y, EV_point.z);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(point.x, point.y, point.z);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            Ray surface_ray;
            RaycastHit hit;

            surface_ray = new Ray(point, Vector3.down);

            if (Physics.Raycast(surface_ray, out hit))
            {
                sphere.transform.position = new Vector3(point.x, hit.point.y + raycast_floorpath_offset, point.z);
                //UnityEngine.Debug.Log(hit.point);
            }
            else sphere.transform.position = new Vector3(point.x, point.y + raycast_floorpath_offset, point.z);
        }
    }

    void PostTargetCallback(string resultData)
    {
        UnityEngine.Debug.Log("Payload to server is: " + resultData);

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
