using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class DistanceFeatureText : MonoBehaviour
{
    public GameObject cam;
    public Vector3 cam_pos;
    public GameObject feature;
    public GameObject feature_map;
    public GameObject quad;
    public bool isFeet;

    public Vector3 oldPos;
    public Vector3 newPos;


    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.Find("Main Camera");
        isFeet = true;
        quad = feature.transform.Find("Quad").gameObject;
        cam_pos = cam.GetComponent<Transform>().position;
        oldPos = cam_pos;
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
        cam_pos = cam.GetComponent<Transform>().position;
        quad = feature.transform.Find("Quad").gameObject;

        float x_distance = (float)Math.Pow(quad.transform.position.x - cam_pos.x, 2);
        float z_distance = (float)Math.Pow(quad.gameObject.transform.position.z - cam_pos.z, 2);

        if (isFeet)
        {
            x_distance = (float)(x_distance * 3.281);
            z_distance = (float)(z_distance * 3.281);
        }
        float distance = (float)Math.Round((float)Math.Sqrt(x_distance + z_distance) * 10f) / 10f;
        
        var feature_text = feature.transform.Find("Feature_Text").GetComponent<TextMeshPro>();
        var type = feature.transform.Find(string.Format("type"));
        string feature_type = type.transform.GetChild(0).name;
        // for map icon 
        var feature_text_map = feature_map.transform.Find("Feature_Text").GetComponent<TextMeshPro>();

        if (isFeet)
        {
            feature_text.text = feature_type + ": " + distance.ToString() + "ft";
            Debug.Log("Distance before map icon: " + distance);
           
            feature_text_map.text = distance.ToString() + "ft"; ;
            Debug.Log("Distance for icon: " + distance);

            
            //feature_text_map.text = distance.ToString() + "ft";
           // Debug.Log("Distance for icon: " + distance);
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
            newPos = cam.GetComponent<Transform>().position;
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
