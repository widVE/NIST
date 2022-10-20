using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIconSpawn : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject iconParent;
    public GameObject mapShow;
    // Start is called before the first frame update
    void Start()
    {
        iconParent.transform.localPosition = new Vector3(0, 0, 0);

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
