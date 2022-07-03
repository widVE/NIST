using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureManager : MonoBehaviour
{

    public EasyVizARHeadsetManager manager;

    // key as id, value as the GameObject (the marker placed)
    public Dictionary<int, EasyVizAR.Feature> feature_dictionary = new Dictionary<int, EasyVizAR.Feature>();
    public Dictionary<int, GameObject> marker_dictionary = new Dictionary<int, GameObject>(); // a seperate dictionary for keeping track of Gameobject in the scene
    public EasyVizAR.FeatureList feature_list = new EasyVizAR.FeatureList();
    public EasyVizAR.Feature featureHolder = null;
    public GameObject markerHolder = null;
    public int featureID = 32; // also a temporary holder

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
        Debug.Log("new ID added: " + resultJSON.id);
        feature_dictionary.Add(resultJSON.id, featureHolder);
        marker_dictionary.Add(resultJSON.id, markerHolder);
        Debug.Log("post contain id?: " + feature_dictionary.ContainsKey(resultJSON.id));


    }

    void GetFeatureCallBack(string result)
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
       
        
        featureHolder = feature_to_post;
        markerHolder = marker;
        //append to list 
        
    }
    [ContextMenu("GetFeature")]
    public void GetFeature() //takes in id as parameter, for some reason Unity doesn't accept that convention
    {
       var id = featureID;
        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, GetFeatureCallBack);


    }
    [ContextMenu("ListFeatures")]
    public void ListFeatures()
    {
        EasyVizARServer.Instance.Get("locations/" + manager.LocationID + "/features", EasyVizARServer.JSON_TYPE, ListFeatureCallBack);
    }

    void ListFeatureCallBack(string result) {
        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);
            feature_list = JsonUtility.FromJson<EasyVizAR.FeatureList> ("{\"features\":" + result + "}"); 


        }
        else
        {
            Debug.Log("ERROR: " + result);
        }

    }

    [ContextMenu("UpateFeature")]
    public void UpateFeature() //    public void UpateFeature(int id, GameObject new_feature)
    {
        Debug.Log("reached update method");
        Debug.Log("contain id?: " + feature_dictionary.ContainsKey(featureID));


        var id = featureID;
        var new_feature = markerHolder;
        if (feature_dictionary.ContainsKey(id))
        {
            
            //creating new feature
            EasyVizAR.Feature feature_to_patch = feature_dictionary[id];
            Debug.Log("feature name: " + feature_to_patch.name);
            feature_to_patch.name = "Updated name";
            // Main: updating the position
            feature_to_patch.position = new_feature.transform.position;


            // TODO: might want to modify the style?
            //feature_to_patch.style.placement = "point";

            //Serialize the feature into JSON
            var data = JsonUtility.ToJson(feature_to_patch);


            // updates the dictionary
            featureHolder = feature_to_patch;
            featureID = id;
            EasyVizARServer.Instance.Patch("locations/" + manager.LocationID + "/features/" + id, EasyVizARServer.JSON_TYPE, data, UpdateFeatureCallback);

        }

    }

    void UpdateFeatureCallback(string result)
    {
        Debug.Log("reached update callback method");
        if (result != "error")
        {
            Debug.Log("SUCCESS: " + result);
            // updates the dictionary
            feature_dictionary[featureID] = featureHolder;

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
