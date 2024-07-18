using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Sentis.Layers;
using UnityEngine;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

public class DockGrabRayPlacer : MonoBehaviour
{
    //location to spawn the cloned object
    public GameObject spawn_parent_location;

    // grabbable docked object prefab
    public GameObject docked_object_prefab;

    //volumetric map prefab that will be spawned at the location the user lets go of the docked object at
    public GameObject wall_finder_prefab;

    private GameObject wall_map;

    //funtion, when this object is picked up it will spawn a prefab at the spawn_parent_location
    public void SpawnDockedObject(GameObject prefab)
    {
        GameObject docked_object = Instantiate(prefab, spawn_parent_location.transform.position, spawn_parent_location.transform.rotation);
        docked_object.transform.SetParent(spawn_parent_location.transform);
    }

    //spawned at the location the user lets go of the docked object at, has no parent   
    public void SpawnPrefab(GameObject prefab)
    {    
        wall_map = Instantiate(prefab);
    }

    public void StopWallfindingInChildren()
    {
        SolverHandler[] solverHandlers = wall_map.GetComponentsInChildren<SolverHandler>();
        foreach (SolverHandler solverHandler in solverHandlers)
        {
            solverHandler.enabled = false;
        }

        SurfaceMagnetism[] surfaceMagnetisms = wall_map.GetComponentsInChildren<SurfaceMagnetism>();
        foreach (SurfaceMagnetism surfaceMagnetism in surfaceMagnetisms)
        {
            surfaceMagnetism.enabled = false;
        }
    }

    // Destroy this object
    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }


}
