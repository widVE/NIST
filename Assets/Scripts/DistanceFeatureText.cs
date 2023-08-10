using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Collections.Specialized;

public class DistanceFeatureText : MonoBehaviour
{
    public GameObject main_camera;
    public Vector3 camera_position;
    public GameObject feature;
    public GameObject feature_map;
    public GameObject quad;
    public bool is_feet = true;
    public GameObject parent;

    public Vector3 oldPos;
    public Vector3 newPos;

    // Start is called before the first frame update
    void Start()
    {
        main_camera = GameObject.Find("Main Camera");
        quad = feature.transform.Find("Quad").gameObject;
        camera_position = main_camera.GetComponent<Transform>().position;
        oldPos = camera_position;
        CalcFeatureDist();
        StartCoroutine(HeadsetDistanceCalculate());
    }

    // Update is called once per frame
    void Update()
    {
        //cam_pos = cam.GetComponent<Transform>().position;
        //CalcFeatureDist();
    }

    void CalcFeatureDist()
    {
        camera_position = main_camera.GetComponent<Transform>().position;
        
        // The ocmmented part is not doing what it's supposed to, but it should be working, but the original code below (uncommented portion) does work
        /*
        Vector3 feature_pos = quad.transform.position;

        float distance = Vector3.Distance(cam_pos, feature_pos);
        distance = Mathf.Round(distance * 10f) / 10f; // round to one decimal place

        if (isFeet)
        {
            distance *= 3.281f; // convert to feet
        }
        */

        float x_distance = (float)Math.Pow(quad.transform.position.x - camera_position.x, 2);
        float z_distance = (float)Math.Pow(quad.transform.position.z - camera_position.z, 2);

        if (is_feet)
        {
            x_distance = (float)(x_distance * 3.281);
            z_distance = (float)(z_distance * 3.281);
        }
        float distance = (float)Math.Round((float)Math.Sqrt(x_distance + z_distance) * 10f) / 10f;

        var feature_text = feature.transform.Find("Feature_Text").GetComponent<TextMeshPro>();
        var type = feature.transform.Find(string.Format("type"));

        //string feature_type = feature.transform.Find("type").GetChild(0).name;
        //if (type != null) { UnityEngine.Debug.Log("type is null"); }
        string feature_type = type.transform.GetChild(0).name;

        // for map icon 
        //var feature_text_map = feature_map.transform.Find("Feature_Text").GetComponent<TextMeshPro>();
        var feature_text_map = this.gameObject.GetComponent<TextMeshPro>();

        if (is_feet)
        {
            feature_text.text = feature_type + ": " + distance.ToString() + "ft";
            feature_text_map.text = feature_text.text;
        }
        else
        {
            feature_text.text = feature_type + " : " + distance.ToString() + "m";
            feature_text_map.text = distance.ToString() + "m";
        }
    }

    IEnumerator HeadsetDistanceCalculate()
    {
        while (true)
        {
            newPos = main_camera.GetComponent<Transform>().position;
            float change_x = (float)Math.Pow((newPos.x - oldPos.x), 2);
            float change_z = (float)Math.Pow((newPos.z - oldPos.z), 2);
            float change_dist = (float)Math.Sqrt(change_x + change_z);
            if (change_dist > 0.05)
            {
                CalcFeatureDist();
                oldPos = newPos;
            }
            yield return new WaitForSeconds(1f);
        }
    }
}
