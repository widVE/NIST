using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
//using VInspector.Libs;
using Unity.Sentis.Layers;

//This class really seems to be more about getting the aspect ratio of the map visualizaiton correct than anything to do with spawning icons. -b 12-24
public class MapIconSpawn : MonoBehaviour
{
    public EasyVizARHeadsetManager headset_manager;
    public GameObject iconParent;
    public List<GameObject> map_objects;
    public List<GameObject> map_lines;
    public GameObject mapCollection;

    public bool verbose_debug = false;
    public bool mirror_axis = false;
    public string last_clicked_target = "";
    
    // Start is called before the first frame update
    void Start()
    {
        headset_manager = EasyVizARHeadsetManager.EasyVizARManager;

        foreach (GameObject wall_lines in map_lines)
        {
            headset_manager.map_line_objects.Add(wall_lines);
        }

        headset_manager.DisplayHandMap();

        DisplayPNGMap();
    }

    // Update is called once per frame
    void Update()
    {

    }

    //ADDED FOR NEW MAP
    [ContextMenu("DisplayPNGMap")]
    public void DisplayPNGMap()
    {
        EasyVizARServer.Instance.Get("locations/" + headset_manager.LocationID + "/layers/1/", EasyVizARServer.JSON_TYPE, DisplayPNGMapCallback);
        //Debug.Log("Got into DisplayPNGMap()");
    }

    public void DisplayPNGMapCallback(string results)
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
            foreach (GameObject map in map_objects) map.transform.localScale = new Vector3(mapWidth / 10, mapHeight / 10, 1);
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
