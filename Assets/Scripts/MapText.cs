using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;
using UnityEngine.Networking;

public class MapText : MonoBehaviour
{
    public string baseURL = "http://halo05.wings.cs.wisc.edu:5000/";
    public string location_id = "66a4e9f2-e978-4405-988e-e168a9429030";

    [SerializeField] 
    public TextMeshPro map_text;

    [System.Serializable]
    public class MapField
    {
        public string id;
        public string name;

    }
    // Start is called before the first frame update
    void Start()
    {
        map_text = GetComponent<TextMeshPro>();
        map_text.color = new Color32(191, 131, 6, 255);
        // TODO: ask about how to fix the hard code problem
        string location_id = "66a4e9f2-e978-4405-988e-e168a9429030";
        string url = baseURL + "locations/" + location_id + "/layers/1/image";
        StartCoroutine(GetMapName(url));
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator GetMapName(string url)
    {
        //first get the texture of the image file
        UnityWebRequest www = UnityWebRequest.Get("http://halo05.wings.cs.wisc.edu:5000/locations/66a4e9f2-e978-4405-988e-e168a9429030");

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
}
