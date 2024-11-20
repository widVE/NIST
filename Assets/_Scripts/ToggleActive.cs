using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 Iterate through list and set the active state of the object opposite to it's current state. Note that this doesn't enable
children in a hierarchy if the partent is inactive. That is to say that they will be marked active but inherit the inactive of the parent.
 */

public class ToggleActive : MonoBehaviour
{
    public List<GameObject> toggle_objects; 

    [ContextMenu("Toggle Objects")]
    public void Toggle()
    {
        foreach(GameObject widget in toggle_objects)
        {
            widget.SetActive(!widget.activeSelf);
        }
    }
}
