using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Sentis.Layers;
using UnityEngine;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

public class HeasetGrabPlacer : MonoBehaviour
{
    //location to spawn the preview object
    public GameObject spawn_parent_location;

    // grabbable docked object prefab
    public GameObject object_preview;

    //volumetric map prefab that will be spawned at the location the user lets go of the docked object at
    public GameObject object_drop;

    private GameObject hidden_object_preview = null;

    //funtion, when this object is picked up it will spawn a prefab at the spawn_parent_location
    public void ReplacePreview()
    {

        hidden_object_preview = Instantiate(object_preview, spawn_parent_location.transform.position, spawn_parent_location.transform.rotation);
        hidden_object_preview.transform.SetParent(spawn_parent_location.transform);
    }

    //public function to find and enable the all mesh renderer of the hidden object preview
    public void EnablePreview()
    {
        if (hidden_object_preview != null)
        {
            //get the mesh renderer of the hidden object preview or its children and enable them all
            MeshRenderer[] meshRenderers = hidden_object_preview.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = true;
            }            
        }
    }

    // function spawned at the location the user lets go of the hidden_object_preview gameobject at
    public void ReleaseHiddenObjectPreview()
    {
        if (hidden_object_preview != null)
        {
            // Instantiate the object_drop_prefab at the position and rotation of the hidden_object_preview
            GameObject object_drop_instance = Instantiate(object_drop, hidden_object_preview.transform.position, hidden_object_preview.transform.rotation);
        }
    }

    // Destroy this object
    public void DestroyObject()
    {
        Destroy(hidden_object_preview.gameObject);
    }


}
