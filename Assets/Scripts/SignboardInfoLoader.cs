using System.Collections;
using System.Collections.Generic;
using TMPro;
//using Unity.VectorGraphics;
//using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class SignboardInfoLoader : MonoBehaviour
{
    //public GameObject[] direction, distance, dest_info;
    //public GameObject destination;
    //public NavMeshAgent agent;

    public int maxDestinationLines = 5;

    float agent_dist;

    private void Start()
    {
        var features = GameObject.FindGameObjectsWithTag("Feature");

        var destination = transform.Find("destination").gameObject;
        var direction = transform.Find("direction").gameObject;
        var distance = transform.Find("distance").gameObject;

        var destinationText = destination.GetComponent<TextMeshPro>();
        var directionText = direction.GetComponent<TextMeshPro>();
        var distanceText = distance.GetComponent<TextMeshPro>();

        destinationText.text = "";
        directionText.text = "";
        distanceText.text = "";

        int index = 0;
        foreach (var feature in features)
        {
            var markerObject = feature.GetComponent<MarkerObject>();

            destinationText.text += markerObject.name + "\n\n";

            Vector3 dir_vec = (feature.transform.position - this.transform.position).normalized;
            float dir = Mathf.Atan2(dir_vec.x, dir_vec.z) * Mathf.Rad2Deg;
            if (dir < 0) { dir += 360; }

            directionText.text += dir.ToString("F1") + "°\n\n";

            float dist = Vector3.Distance(feature.transform.position, this.transform.position);
            
            distanceText.text += dist.ToString("F1") + "m\n\n";

            index++;

            // Limit the number of lines on the sign if there are a lot of features.
            if (index >= maxDestinationLines)
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
