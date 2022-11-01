using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapIconSpawn : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject iconParent;
    public GameObject map;
    // Start is called before the first frame update
    void Start()
    {
        //MapTest3 --> Location
        //float x = -0.1121;
        //float y = 0.1224;
        //float z = -10.596918136310494 * 0.01;
        //iconParent.transform.localPosition = new Vector3(-1, 0, 0);

        float x = (float)map.transform.position.x;
        float y = (float)map.transform.position.y;
        float z = (float)(map.transform.position.z - 0.125);

        iconParent.transform.localPosition = new Vector3(x,y,z);

        //iconParent.transform.localPosition = new Vector3(x, y, z);

        // DisplayPNGMap();
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

            Debug.Log("the top is: " + resultJSON.viewBox.top);



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
