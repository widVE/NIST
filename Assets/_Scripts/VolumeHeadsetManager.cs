using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeHeadsetManager : MonoBehaviour
{
    public NavigationManager local_navigation_manager = null;
    public EasyVizARHeadset headset_reference = null;
    public EasyVizARHeadsetManager headsetManager_reference = null;
    public PathPreview pathPreview = null;

    public GameObject prefab_root;

    Vector3 user_start_position;

    public GameObject ghost_user;
    public Material ghost_material;

    private void Awake()
    {
        headsetManager_reference = EasyVizARHeadsetManager.EasyVizARManager;
        headset_reference = this.transform.parent.GetComponent<EasyVizARHeadset>();

        // Find the NavigationManager in the scene and assign it to our var
        GameObject navigation_manager_game_object = GameObject.Find("NavigationManager");

        if (navigation_manager_game_object != null)
        {
            local_navigation_manager = navigation_manager_game_object.GetComponent<NavigationManager>();
        }

        if (local_navigation_manager != null)
        {
            Debug.Log("NavigationManager successfully found and assigned.");
        }
        else
        {
            Debug.LogError("NavigationManager not found in the scene.");
        }
    }

    public void NavigationTrigger()
    {
        if (local_navigation_manager != null)
        {
            //Debug.Log(this.transform.parent.GetComponent<Transform>().localPosition);
            local_navigation_manager.GiveDirectionsToUser(this.transform.parent.GetComponent<Transform>().localPosition, user_start_position, headsetManager_reference.LocationID, headset_reference._headsetID, headset_reference._color, headset_reference.Name);
            Debug.Log("new position " + this.transform.parent.GetComponent<Transform>().localPosition);
            Debug.Log("old position " + user_start_position);
            Debug.Log("location id " + headsetManager_reference.LocationID);
            Debug.Log("headset id " + headset_reference._headsetID);
            Debug.Log("color " + "#" + ColorUtility.ToHtmlStringRGB(headset_reference._color));
            Debug.Log("headset name " + headset_reference.Name);

        }
        else
        {
            Debug.LogError("NavigationTrigger called but local_nav_reference is null.");
        }
    }

    public void StorePosition()
    {
        try
        {
            user_start_position = prefab_root.transform.localPosition;
        }
        catch (System.NullReferenceException ex)
        {
            Debug.LogError("Null reference exception: " + ex.Message);
        }
    }

    public void StartPathPreview(GameObject cursor)
    {
        if (pathPreview)
        {
            pathPreview.StartPreview(cursor, prefab_root);
        }
    }

    public void StopPathPreview()
    {
        if (pathPreview)
        {
            pathPreview.StopPreview();
        }
    }

    // Reset the ghost user to the origin, intended to be called when the user starts interacting with the avatar so that it will be back at the same location as the user avatar
    public void GhostReset()
    {
        ghost_user.transform.localPosition = new Vector3(0, 0, 0);
        ghost_user.transform.localRotation = new Quaternion(0, 0, 0, 0);
    }

    public void GhostActiveToggle()
    {
        if (ghost_user.activeSelf)
        {
            ghost_user.SetActive(false);
        }
        else
        {
            ghost_user.SetActive(true);
        }
    }

    //same as last function but takes a bool argument instead
    public void GhostActiveToggle(bool active)
    {
        ghost_user.SetActive(active);
    }
}
