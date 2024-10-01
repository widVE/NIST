using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsCheck : MonoBehaviour
{
    public GameObject culler;
    public GameObject moveable_map;
    Collider culling_box_collider;
    Collider map_Collider;
    Collider culler_Collider;
    GameObject volumetric_map;
    GameObject headsets;
    GameObject features;

    void Start()
    {
        //Fetch the Collider from the GameObject this script is attached to
        culling_box_collider = GetComponent<Collider>();
        culler_Collider = culler.GetComponent<Collider>();
        
        volumetric_map = moveable_map;
        features = moveable_map.transform.Find("features").gameObject;
        headsets = moveable_map.transform.Find("headsets").gameObject;

        //call RndererBoundsCheck every 0.1 seconds
        InvokeRepeating("RendererBoundsCheck", 0.5f, 0.1f);
    }

    private void RendererBoundsCheck()
    {
        Renderer[] volumetric_map_renderers = volumetric_map.GetComponentsInChildren<Renderer>();

        foreach (Renderer map_visuals_renderer in volumetric_map_renderers)
        {
            //If the first GameObject's Bounds contains the Transform's position, output a message in the console
            if (culler_Collider.bounds.Contains(map_visuals_renderer.bounds.center))
            {
                map_visuals_renderer.enabled = true;
            }
            else
            {
                map_visuals_renderer.enabled = false;
            }
        }


        //foreach (Transform child in volumetric_map.transform)
        //{

        //    Renderer rend = child.GetComponent<Renderer>();
        //    //If the first GameObject's Bounds contains the Transform's position, output a message in the console
        //    if (culling_box_collider.bounds.Contains(rend.bounds.center) || culler_Collider.bounds.Contains(rend.bounds.center))
        //    {
        //        rend.enabled = true;
        //    }
        //    else
        //    {
        //        rend.enabled = false;
        //    }
        //}

        //foreach (Transform feature in features.transform)
        //{
        //    Renderer rend = feature.transform.Find("Icon Visuals").GetComponent<MeshRenderer>();
        //    Renderer text_rend = feature.transform.Find("Feature_Text").GetComponent<MeshRenderer>();
        //    if (rend == null || text_rend == null)
        //    {
        //        Debug.Log("feature renderer is null");
        //    }
        //    if (culling_box_collider.bounds.Contains(rend.bounds.center) || culler_Collider.bounds.Contains(rend.bounds.center))
        //    {
        //        rend.enabled = true;
        //        text_rend.enabled = true;
        //    }
        //    else
        //    {
        //        rend.enabled = false;
        //        text_rend.enabled = false;
        //    }

        //}

        //foreach (Transform headset in headsets.transform)
        //{
        //    Renderer rend = headset.transform.Find("Capsule").GetComponent<MeshRenderer>();
        //    Renderer text_rend = headset.transform.Find("Feature_Text").GetComponent<MeshRenderer>();
        //    if (rend == null || text_rend == null)
        //    {
        //        Debug.Log("feature renderer is null");
        //    }
        //    if (culling_box_collider.bounds.Contains(rend.bounds.center) || culler_Collider.bounds.Contains(rend.bounds.center))
        //    {
        //        rend.enabled = true;
        //        text_rend.enabled = true;
        //    }
        //    else
        //    {
        //        rend.enabled = false;
        //        text_rend.enabled = false;
        //    }

        //}
    }
}