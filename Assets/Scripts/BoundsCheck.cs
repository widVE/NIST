using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class BoundsCheck : MonoBehaviour
{
    public GameObject culler;
    public GameObject volumetric_map;
    Renderer culler_renderer;

    bool map_shrunk = false;


    void Start()
    {
        //Fetch the Collider from the GameObject that does the culling
        culler_renderer = culler.GetComponent<Renderer>();

        //call RndererBoundsCheck every 0.1 seconds
        InvokeRepeating("RendererBoundsCheck", 0.5f, 0.1f);
    }

    private void RendererBoundsCheck()
    {
        //This operates on all the renderers in the volumetric map hierarchy, not just the 3d map segments, so this includes the markers and headsets too
        Renderer[] volumetric_map_renderers = volumetric_map.GetComponentsInChildren<Renderer>();

        foreach (Renderer map_visuals_renderer in volumetric_map_renderers)
        {
            //If the first GameObject's Bounds contains the Transform's position, output a message in the console
            if (culler_renderer.bounds.ContainsCompletely(map_visuals_renderer.bounds))
            {
                map_visuals_renderer.enabled = true;
            }
            else 
            {
                map_visuals_renderer.enabled = false;
            }
        }
    }

    // This function will autofit the local scale of the volumetric map to be encompassed by the culler renderer
    public void AutofitVolumetricMap()
    {
        // Get the bounds of the culler renderer
        Bounds culler_bounds = culler_renderer.bounds;

        // Get the bounds of the volumetric map
        Renderer[] volumetric_map_renderers = volumetric_map.GetComponentsInChildren<Renderer>();

        //set the object_bounds to the first renderer's bounds and grow the bounds to encapsulate all the renderers
        Bounds map_visual_bounds = volumetric_map_renderers[0].bounds;

        foreach (Renderer object_renderer in volumetric_map_renderers)
        {
            map_visual_bounds.Encapsulate(object_renderer.bounds);
        }

        // Calculate the scale factor for each axis!!! DON"T APPLY THIS TO ALL THE AXIS, Only use the smallest scale factor and uniformlay scale the map, otherwise each axis will be scaled differently
        float scaleX = culler_bounds.size.x / map_visual_bounds.size.x;
        float scaleY = culler_bounds.size.y / map_visual_bounds.size.y;
        float scaleZ = culler_bounds.size.z / map_visual_bounds.size.z;

        // Apply the scale factor of the smallest factor uniformaly to the volumetric map only if the map has not been shrunk already
        if (!map_shrunk)
        {
            float scale_factor = Mathf.Min(scaleX, scaleY, scaleZ);
            volumetric_map.transform.localScale = new Vector3(scale_factor, scale_factor, scale_factor);
            map_shrunk = true;
        }
        
    }


}