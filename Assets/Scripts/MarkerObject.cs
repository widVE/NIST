using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerObject : MonoBehaviour
{
    //public EasyVizAR.Feature feature;
    //public GameObject marker;
    public int feature_ID = -1;
    public string feature_type = "";
    public string feature_name = "";
    public Vector3 world_position;
    public FeatureManager manager_script = null;

    public bool displayDistanceInFeet = true;

    private TMPro.TextMeshPro feature_tmp = null;
    private string previous_label = "";

    void Start()
    {
        var feature_text = this.transform.Find("Feature_Text");
        if (feature_text)
        {
            feature_tmp = feature_text.GetComponent<TMPro.TextMeshPro>();
            if (feature_tmp)
            {
                StartCoroutine(UpdateDistanceText());
            }
        }
    }

    // Notify the Feature Manager that this marker has changed so that it can then notify the server.
    public void UpdatedWithID()
    {
        //StartCoroutine(waiter());
        //manager_script = GameObject.Find("HandMenu_Large_AutoWorldLock_On_HandDrop_Marker_Array").GetComponent<FeatureManager>();
        manager_script = GameObject.Find("FeatureManager").GetComponent<FeatureManager>();

        manager_script.UpdateFeature(feature_ID);
    }

    IEnumerator UpdateDistanceText()
    {
        var refreshWait = new WaitForSeconds(1f);
        while (true)
        {
            string display_name;
            if (feature_name.Length > 0)
                display_name = feature_name;
            else if (feature_type.Length > 0)
                display_name = feature_type;
            else
                display_name = name;

            double distance = Vector3.Distance(Camera.main.transform.position, world_position);
            string display_unit = "m";
            if (displayDistanceInFeet)
            {
                distance *= 3.281;
                display_unit = "ft";
            }

            // Suppress displaying distance when the marker is very close because it should be obvious.
            // This has the useful side effect of not showing zero distance for the local headset marker.
            string label = (distance >= 1) ? display_name + " : " + distance.ToString("0.0") + display_unit : display_name;

            // This might improve performance by only changing the TextMeshPro text field
            // when the label content should actually change.
            if (label != previous_label)
            {
                feature_tmp.text = label;
                previous_label = label;
            }

            yield return refreshWait;
        }
    }
}
