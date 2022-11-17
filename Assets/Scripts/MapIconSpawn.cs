using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapIconSpawn : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject iconParent;
    public GameObject map;
    public GameObject mainMap;
    public GameObject mapCollection;
    public GameObject feature_parent;
    // Start is called before the first frame update
    void Start()
    {
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
        EasyVizARServer.Instance.Get("locations/" + currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID + "/layers/1/", EasyVizARServer.JSON_TYPE, DisplayPNGMapCallback);

    }

    public void DisplayPNGMapCallback(string results)
    {

        if (results != "error")
        {
            Debug.Log("SUCCESS: " + results);
            var resultJSON = JsonUtility.FromJson<EasyVizAR.MapInfo>(results);
            float mapTop = resultJSON.viewBox.top;
            float mapLeft = resultJSON.viewBox.left;
            float mapHeight = resultJSON.viewBox.height;
            float mapWidth = resultJSON.viewBox.width;

            //enlarging the map to the scale listed from the server (width and height)
            map.transform.localScale = new Vector3(mapWidth/10, mapHeight/10, 1);
            float icon_origin_x = -1*(mapWidth / 2.0f + mapLeft);
            float icon_origin_y = mapHeight / 2.0f + mapTop;
            Debug.Log("origin x and y: " + icon_origin_x + ", " + icon_origin_y);
            float icon_z_offset = -0.12f;

            iconParent.transform.localPosition = new Vector3(icon_origin_x, icon_origin_y, icon_z_offset); // the scale may need to be adjusted
        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }
    
}
