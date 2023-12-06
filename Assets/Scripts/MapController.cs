using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapController : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject iconParent;
    public List <GameObject> map_scale;
    public List<GameObject> map_lines;
    public GameObject mapCollection;
    public GameObject feature_parent;

    public bool verbose_debug = false;
    public bool mirror_axis = false;
    public string last_clicked_target = "";

    [SerializeField] String map_location_ID;
    [SerializeField] int map_resolution = 1200;

    // Start is called before the first frame update, but is not called
    // for disabled game objects until they are enabled.
    void Start()
    {
        //map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
    }

    private void Awake()
    {
        //map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
    }

    // Display the updated map whenever the game object is enabled. Not quite where
    // we want to end up, we want to get a signal from the web server, or a controller
    // to update, but this should refresh the map each time we summon it.
    void OnEnable()
    {
        map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
        UpdateMapImage();
        MapAspectRatioAndOrigin();
    }



    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Map Image")]
    public void UpdateMapImage()
    {
        EasyVizARServer.Instance.Texture("locations/" + map_location_ID + "/layers/1/image", "image/png", map_resolution.ToString(), UpdateMapImageCallback);
    }


    public void UpdateMapImageCallback(Texture resultTexture)
    {
        //Debug.Log("In map callback");
        foreach (var map_layout in map_lines)
        {
            map_layout.GetComponent<Renderer>().material.mainTexture = resultTexture;
        }
    }

    //ADDED FOR NEW MAP
    [ContextMenu("Map Aspect Ratio and Icon Origin")]
    public void MapAspectRatioAndOrigin()
    {
        EasyVizARServer.Instance.Get("locations/" + map_location_ID + "/layers/1/", EasyVizARServer.JSON_TYPE, MapAspectRatioAndOriginCallback);
        //Debug.Log("Got into DisplayPNGMap()");
    }

    public void MapAspectRatioAndOriginCallback(string results)
    {

        if (results != "error")
        {
            if (verbose_debug) Debug.Log("Map Callback png SUCCESS: " + results);
            var resultJSON = JsonUtility.FromJson<EasyVizAR.MapInfo>(results);
            float mapTop = resultJSON.viewBox.top;
            float mapLeft = resultJSON.viewBox.left;
            float mapHeight = resultJSON.viewBox.height;
            float mapWidth = resultJSON.viewBox.width;

            //enlarging the map to the scale listed from the server (width and height)
            foreach (GameObject map in map_scale) map.transform.localScale = new Vector3(mapWidth / 10, mapHeight / 10, 1);
            float icon_origin_x = (mapWidth / 2.0f + mapLeft);
            float icon_origin_y = mapHeight / 2.0f + mapTop;
            if (mirror_axis) icon_origin_x *= -1;
            if (mirror_axis) icon_origin_y *= -1;

            //float icon_origin_x = (0 - mapLeft) / mapWidth;
            //loat icon_origin_y = (0 - mapTop) / mapHeight;

            //Debug.Log("origin x and y: " + icon_origin_x + ", " + icon_origin_y);
            float icon_z_offset = -0.12f;

            iconParent.transform.localPosition = new Vector3(icon_origin_x, icon_origin_y, icon_z_offset); // the scale may need to be adjusted
        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }
    
}
