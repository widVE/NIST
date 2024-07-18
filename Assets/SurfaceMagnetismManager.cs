using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMagnetismManager : MonoBehaviour
{
    public GameObject SurfaceMagnetismGameObject;
    
    public void StopWallfinding()
    {
        //disable the SolverHandler and SurfaceMagnetism components on this object
        SolverHandler solverHandler = GetComponent<SolverHandler>();
        if (solverHandler != null)
        {
            solverHandler.enabled = false;
        }

        SurfaceMagnetism surfaceMagnetism = GetComponent<SurfaceMagnetism>();
        if (surfaceMagnetism != null)
{
            surfaceMagnetism.enabled = false;
        }
    }

    //disable the SolverHandler and SurfaceMagnetism components on the SurfaceMagnetismManager object
    public void StopWallfindingInParent()
    {
        SolverHandler solverHandler = SurfaceMagnetismGameObject.GetComponent<SolverHandler>();
        if (solverHandler != null)
        {
            solverHandler.enabled = false;
        }
        else
        {
            Debug.Log("SolverHandler is null");
        }

        SurfaceMagnetism surfaceMagnetism = SurfaceMagnetismGameObject.GetComponent<SurfaceMagnetism>();
        if (surfaceMagnetism != null)
        {
            surfaceMagnetism.enabled = false;
        }
        else
        {
            Debug.Log("SurfaceMagnetism is null");
        }
    }

    //disable the SolverHandler and SurfaceMagnetism components on this object's children
    public void StopWallfindingInChildren()
    {
        SolverHandler[] solverHandlers = GetComponentsInChildren<SolverHandler>();
        foreach (SolverHandler solverHandler in solverHandlers)
        {
            solverHandler.enabled = false;
        }

        SurfaceMagnetism[] surfaceMagnetisms = GetComponentsInChildren<SurfaceMagnetism>();
        foreach (SurfaceMagnetism surfaceMagnetism in surfaceMagnetisms)
        {
            surfaceMagnetism.enabled = false;
        }
    }

    public void StartWallfinding()
    {
        //enable the SolverHandler and SurfaceMagnetism components on this object
        SolverHandler solverHandler = GetComponent<SolverHandler>();
        if (solverHandler != null)
        {
            solverHandler.enabled = true;
        }

        SurfaceMagnetism surfaceMagnetism = GetComponent<SurfaceMagnetism>();
        if (surfaceMagnetism != null)
        {
            surfaceMagnetism.enabled = true;
        }
    }

}
