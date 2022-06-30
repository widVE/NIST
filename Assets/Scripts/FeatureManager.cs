using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureManager : MonoBehaviour
{

    public EasyVizARHeadsetManager manager;

    // key as id, value as the GameObject (the marker placed)
    public Dictionary<int, GameObject> feature_dictionary = new Dictionary<int, GameObject>();
    public List<EasyVizAR.Feature> feature_list = new List<EasyVizAR.Feature>();
    private GameObject markerHolder = null;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // a callback function
    void PostFeature(string result)
    {
        var resultJSON = JsonUtility.FromJson<EasyVizAR.Feature>(result);
        
        if (result != "error")
        { 
            Debug.Log("SUCCESS: " + result);
           
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }
        feature_dictionary.Add(resultJSON.id, markerHolder);
       
    }

    void GetFeatureCallBack(string result)
    {

        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);

        }
        else
        {
            Debug.Log("ERROR: " + result);
        }


    }

    // POST 
    public void CreateNewFeature(int index, GameObject marker)
    {

        EasyVizAR.Feature feature_to_post = new EasyVizAR.Feature();

      //feature_to_post.created = ((float)System.DateTime.Now.Hour + ((float)System.DateTime.Now.Minute*0.01f));
        feature_to_post.createdBy = "Test Marker";
      // feature_to_post.id = 1;
        feature_to_post.name = "Hallway Fire";
        feature_to_post.position = marker.transform.position;
        if (index == 0)
        {
            feature_to_post.type = "Hazard";
        }
        else if (index == 1)
        {
            feature_to_post.type = "Waypoint";
        }
        else
        {
            feature_to_post.type = "Injury";
        }

        //   feature_to_post.updated = ((float)System.DateTime.Now.Hour + ((float)System.DateTime.Now.Minute * 0.01f));
        EasyVizAR.FeatureDisplayStyle style = new EasyVizAR.FeatureDisplayStyle();
        style.placement = "point";
        feature_to_post.style = style;
        
        //Serialize the feature into JSON
        var data = JsonUtility.ToJson(feature_to_post);
        Debug.Log("Json utility: " + feature_to_post.id);
        Debug.Log("locations/" + manager.LocationID + "/features");
        //for sending stuff to the server 
        
        EasyVizARServer.Instance.Post("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, data, PostFeature);
       
        
        markerHolder = marker;
        //append to list 
        
    }

    public void GetFeature(int id)
    {

        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, GetFeatureCallBack);


    }

    public void ListFeatures()
    {
        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, ListFeatureCallBack);
    }

    void ListFeatureCallBack(string result) {
        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);
            feature_list = JsonUtility.FromJson<List<EasyVizAR.Feature>>(result);

        }
        else
        {
            Debug.Log("ERROR: " + result);
        }

    }


    // might need this in the future, but just left it here for now
    public void DeleteAllFeature()
    {

    }
}
