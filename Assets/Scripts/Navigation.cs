using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        line.positionCount = 2; // this is hard coded for now.
        points = new Transform[2];
    }

    // Update is called once per frame
    void Update()
    {
        
        Debug.Log("Got to Update() from Navigation");
        //update the position of user and landmark 
        /*
        points[0] = Camera.main.transform;
        points[1] = markerSpawnParent.transform.Find(this.name); // might move this line to elsewhere, but for now, it should work fine

        line.SetPosition(0, points[0].position); // the position of the user (i.e. the main camera's position)
        line.SetPosition(1, points[1].position); // the position of the desire landmark/icon

        
        Debug.Log("Number of points in array: " + points.Length);
        // TODO: ask why this is not happening... is the interaction not enabled...? Need to ask Bryce about the map icon prefab
        if (Input.touchCount > 0 && (Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Debug.Log("Got to the Navigation Touch Update");
            Ray detectRay = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            if (Physics.Raycast(detectRay, out hit))
            {
                if (hit.collider.tag == "Icon")
                {
                    //if the icon on the palmup map is touched then initiate line rendering 
                    RenderNavigationPath();
                }
            }
        }
        */


    }

    public void RenderNavigationPath()
    {
        //TODO: this is where I should add the line render between the user and the desire landmark/icon
        Debug.Log("Touched the icon!");
        points[0] = Camera.main.transform;
        points[1] = markerSpawnParent.transform.Find(this.name); // might move this line to elsewhere, but for now, it should work fine

        line.SetPosition(0, points[0].position); // the position of the user (i.e. the main camera's position)
        line.SetPosition(1, points[1].position); // the position of the desire landmark/icon
        // Note: I set the line renderer to be in world space, so it may not render because of this 
    }
}
