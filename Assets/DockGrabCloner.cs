using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Sentis.Layers;
using UnityEngine;

public class DockGrabCloner : MonoBehaviour
{
    //location to spawn the cloned object
    public GameObject spawn_parent_location;

    // grabbable docked object prefab
    public GameObject docked_object_prefab;

    //volumetric map prefab that will be spawned at the location the user lets go of the docked object at
    public GameObject volumetric_map_prefab;

    //funtion, when this object is picked up it will spawn a prefab at the spawn_parent_location
    public void SpawnObject(GameObject prefab)
    {
        GameObject docked_object = Instantiate(prefab, spawn_parent_location.transform.position, spawn_parent_location.transform.rotation);
        docked_object.transform.SetParent(spawn_parent_location.transform);
    }

    //spawned at the location the user lets go of the docked object at, has no parent   
    public void SpawnVolumetricMap(GameObject prefab)
    {
        
        GameObject volume_map = Instantiate(prefab, this.transform.position, quaternion.identity);
    }

    // Destroy this object
    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }


}
