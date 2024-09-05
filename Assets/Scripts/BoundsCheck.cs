using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsCheck : MonoBehaviour
{
    public GameObject culler;
    public GameObject moveable_map;
    Collider m_Collider;
    Collider map_Collider;
    Collider culler_Collider;
    GameObject volumetric_map;

    void Start()
    {
        //Fetch the Collider from the GameObject this script is attached to
        m_Collider = GetComponent<Collider>();
        culler_Collider = culler.GetComponent<Collider>();
        
        volumetric_map = moveable_map.transform.Find("WavefrontObject").gameObject;

    }

    void Update()
    {
        foreach(Transform child in volumetric_map.transform)
        {

            Renderer rend = child.GetComponent<Renderer>();
            //If the first GameObject's Bounds contains the Transform's position, output a message in the console
            if (m_Collider.bounds.Contains(rend.bounds.center) || culler_Collider.bounds.Contains(rend.bounds.center)) 
            {
                rend.enabled = true;
                //Debug.Log("Bounds does not contain the point : " + rend.bounds.center);
            }
            else
            {
                rend.enabled = false;
                //Debug.Log("Bounds contain the point : " + rend.bounds.center);
            }
        }

    }
}