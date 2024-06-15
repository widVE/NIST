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

    public void Awake()
    {
        MatchBottomRenderBounds();
    }

    //Function that matches each object in the objects_to_match array to the lower bounds of the lower_bounds_source object
    public void MatchBottomRenderBounds()
    {
        Debug.Log("Matching: ");


        foreach (GameObject floored_object in objects_to_match)
        {
            //get the bounds of the source object
            Bounds source_bounds = lower_bounds_source.GetComponent<Renderer>().bounds;

            //get the bounds of the object to match
            Bounds object_bounds = floored_object.GetComponent<Renderer>().bounds;

            //check if object_bounds is null, check if source_bounds is null
            if (object_bounds == null || source_bounds == null)
            {
                Debug.LogWarning("Bounds are null");
                return;
            }

            //get the difference between the source object and the object to match, this will be negative if the object to match is above the source object
            float difference = source_bounds.min.y - object_bounds.min.y;

            //print the difference
            Debug.Log("Difference: " + difference);

            //move the object to match to match the source object
            floored_object.transform.position = new Vector3(floored_object.transform.position.x, floored_object.transform.position.y + difference, floored_object.transform.position.z);
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
