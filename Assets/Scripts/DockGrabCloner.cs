using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Sentis.Layers;
using UnityEngine;
using System.Linq;

public class DockGrabCloner : MonoBehaviour
{
    //location to spawn the cloned object
    public GameObject spawn_parent_location;

    // grabbable docked object prefab
    public GameObject docked_object_prefab;

    //volumetric map prefab that will be spawned at the location the user lets go of the docked object at
    public GameObject volumetric_map_prefab;

    //Name of the culling region in the volumetric map prefab, this is the ultimate size of the object
    public string culling_box_name = "Manual Culling Box Adjustment";

    //funtion, when this object is picked up it will spawn a prefab at the spawn_parent_location
    public void SpawnObject(GameObject prefab)
    {
        GameObject docked_object = Instantiate(prefab, spawn_parent_location.transform.position, spawn_parent_location.transform.rotation);
        docked_object.transform.SetParent(spawn_parent_location.transform);
    }

    //spawned at the location the user lets go of the docked object at, has no parent   
    public void SpawnVolumetricMap(GameObject prefab)
    {
        //find the child of volumetric_map_prefab GameObject that has the name "VolumeMap"
        //this is using LINQ, which is a way to query objects in a collection. I'm not really sure how it works, but I thought it was cool
        GameObject culling_region = volumetric_map_prefab.GetComponentsInChildren<Transform>().FirstOrDefault(c => c.gameObject.name == culling_box_name)?.gameObject;

        //get the y extents bounds of the volume map and add that value to spawn_position.y
        //so the volume map is spawned above the object
        Vector3 spawn_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);  
        
        spawn_position.y += culling_region.GetComponent<BoxCollider>().bounds.extents.y;
        
        GameObject volume_map = Instantiate(prefab, spawn_position, quaternion.identity);
    }

    // Destroy this object
    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }


}