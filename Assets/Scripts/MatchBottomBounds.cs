using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchBottomBounds : MonoBehaviour
{
    //This class is used to match the bottom bounds of the object to the bottom bounds of the argument object. It uses the renderer bounds to do this, so the object must have a renderer component.

    public GameObject lower_bounds_source;

    public GameObject[] objects_to_match;

    //Boolean to keep track of if the object has been matched
    public bool matched = false;

    public void Start()
    {
        MatchBottomRenderBounds();
    }

    //Function that matches each object in the objects_to_match array to the lower bounds of the lower_bounds_source object
    public void MatchBottomRenderBounds()
    {
        Debug.Log("Matching:");

        if (lower_bounds_source == null)
        {
            Debug.LogWarning("Lower bounds source is null");
            return;
        }

        Renderer source_renderer = lower_bounds_source.GetComponent<Renderer>();

        if (source_renderer == null)
        {
            Debug.LogWarning("Source renderer is null");
            return;
        }

        // Get the bottom Y-coordinate of the source object's bounds
        Bounds source_bounds = source_renderer.bounds;
        float source_min_y = source_bounds.min.y;

        foreach (GameObject floored_object in objects_to_match)
        {
            if (floored_object == null)
            {
                Debug.LogWarning("Floored object is null");
                continue;
            }

            //get the renderer component of the object to match
            //Renderer object_renderer = floored_object.GetComponent<Renderer>();

            Bounds object_bounds = new Bounds();

            Renderer[] object_renderers = floored_object.GetComponentsInChildren<Renderer>();

            if (object_renderers.Length == 0)
            {
                Debug.LogWarning("No renderers found in object to match");
                return;
            }
            else
            {
                //set the object_bounds to the first renderer's bounds and grow the bounds to encapsulate all the renderers
                object_bounds = object_renderers[0].bounds;

                foreach (Renderer object_renderer in object_renderers)
                {
                    object_bounds.Encapsulate(object_renderer.bounds);
                }
            }

            //Getting the bottom coordinate of the objects to match
            float object_min_y = object_bounds.min.y;

            //Difference between the object's transform and the object's bounds
            float object_transform_difference = object_min_y - floored_object.transform.position.y;

            //get the difference between the source object and the object to match, this will be negative if the object to match is above the source object
            float difference = source_bounds.min.y - object_transform_difference;

            //print the difference
            Debug.Log("Difference: " + difference);

            ////move the object to match to match the source object
            //Vector3 new_position = new Vector3(floored_object.transform.position.x, floored_object.transform.position.y + difference, floored_object.transform.position.z);
            //floored_object.transform.position = new_position;

            // Set the object's position directly to align the bottoms
            Vector3 new_position = new Vector3(
                floored_object.transform.position.x,
                difference,
                floored_object.transform.position.z
            );

            floored_object.transform.position = new_position;
        }
    }

    //same as MatchBottomBoundsFunction, but this time it is conditional on the matched boolean
    public void MatchBottomBoundsConditional()
    {
        if (!matched)
        {
            MatchBottomRenderBounds();
            matched = true;
        }
    }
}
