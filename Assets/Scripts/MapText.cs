using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.Networking;

public class MapText : MonoBehaviour
{
    public string baseURL = "http://halo05.wings.cs.wisc.edu:5000/";

    [SerializeField]
    public string location_id = "";

    [SerializeField] 
    public TextMeshPro map_text;


    [System.Serializable]
    public class MapField
    {
        public string id;
        public string name;

    }

    [System.Serializable]
    public class LocationItem 
    {
        public string id;
        public string name;
    }

    [System.Serializable]
    public class Location
    {
        public LocationItem[] loc;
    }
    // Start is called before the first frame update
    void Start()
    {
        map_text = GetComponent<TextMeshPro>();
        map_text.color = new Color32(191, 131, 6, 255);
        // TODO: ask about how to fix the hard code problem
        StartCoroutine(GetLocationID(baseURL + "locations"));
        Debug.Log("This is the location ID: " + location_id);
        string url = baseURL + "locations/" + location_id;

        StartCoroutine(GetMapName(url));
        
    }

    IEnumerator GetMapName(string url)
    {
        //first get the texture of the image file
        UnityWebRequest www = UnityWebRequest.Get(url);

        // www.SetRequestHeader("name", "CS Building"); // TODO: ask Lance to name the image file [need to fix]
        yield return www.SendWebRequest();


        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Successfully done a UnityWebRequest");

            var txt = www.downloadHandler.text;
            MapField mapField = JsonUtility.FromJson<MapField>(txt);
            map_text.text = mapField.name;
            Debug.Log(map_text.text);
        }


    }

    IEnumerator GetLocationID(string url)
    {
        //first get the texture of the image file
        UnityWebRequest www = UnityWebRequest.Get(url);

        yield return www.SendWebRequest();


        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Successfully done a UnityWebRequest");

            var txt = www.downloadHandler.text;
            Location myLocation = new Location();
            myLocation = JsonUtility.FromJson<Location>("{\"loc\":" + txt + "}");

            this.location_id = myLocation.loc[0].id; //TODO: will have to fix this after (ask Ross about how he is setting up location id) 
            
            Debug.Log(map_text.text);
        }


    }

}
