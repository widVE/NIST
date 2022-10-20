using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapIconSpawn : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject mapParent;
    // Start is called before the first frame update
    void Start()
    {

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

        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }
}
