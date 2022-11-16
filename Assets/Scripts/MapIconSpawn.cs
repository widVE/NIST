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
    // Start is called before the first frame update
    void Start()
    {
        //MapTest3 --> Location
        //float x = -0.1121;
        //float y = 0.1224;
        //float z = -10.596918136310494 * 0.01;
        //iconParent.transform.localPosition = new Vector3(-1, 0, 0);
/*
        float x = (float)mainMap.transform.position.x;
        float y = (float)mainMap.transform.position.y;
        float z = (float)(mainMap.transform.position.z - 0.5);
        iconParent.transform.localPosition = new Vector3(x,y,z);
*/

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
            //mapCollection.transform.localScale = new Vector3((float)(1/mapWidth), (float)(1/mapHeight), 0);


            //finding the origin of the QR Code
            //float Rx = ((0 - mapLeft) / mapWidth);
            //float Ry = ((0 - mapTop) / mapHeight);
            //iconParent.transform.localPosition = new Vector3(, , (float)-0.125);



            /*
            //this is where we might want to scale the finder based on the viewbox
            foreach (Transform child in iconParent.transform)
            {
                 //child.transform.localPosition = 
            }
            */

        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }
}
