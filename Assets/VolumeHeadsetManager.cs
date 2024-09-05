using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeHeadsetManager : MonoBehaviour
{
    public NavigationManager local_nav_reference = null;
    public EasyVizARHeadset headset_reference = null;
    public EasyVizARHeadsetManager headsetManager_reference = null;
    Vector3 old_position;


    private void Awake()
    {
        // Find the NavigationManager in the scene and assign it to our var
        GameObject navigationManagerObject = GameObject.Find("MixedRealityPlayspace/AR Managment Axis/NavigationManager");
        GameObject locationIDObject = GameObject.Find("MixedRealityPlayspace/AR Managment Axis/EasyVizARHeadsetManager");
        if (navigationManagerObject != null)
        {
            local_nav_reference = navigationManagerObject.GetComponent<NavigationManager>();
            headset_reference = this.transform.parent.GetComponent<EasyVizARHeadset>();
            headsetManager_reference = locationIDObject.GetComponent<EasyVizARHeadsetManager>();
        }

        if (local_nav_reference != null)
        {
            Debug.Log("NavigationManager successfully found and assigned.");
        }
        else
        {
            Debug.LogError("NavigationManager not found in the scene.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Any additional logic that needs to be done in Start
    }

    public void NavigationTrigger()
    {
        if (local_nav_reference != null)
        {
            //Debug.Log(this.transform.parent.GetComponent<Transform>().localPosition);
            local_nav_reference.GiveDirectionsToUser(this.transform.parent.GetComponent<Transform>().localPosition, old_position, headsetManager_reference.LocationID, headset_reference._headsetID, "#" + ColorUtility.ToHtmlStringRGB(headset_reference._color), headset_reference.Name);
            Debug.Log("new position " + this.transform.parent.GetComponent<Transform>().localPosition);
            Debug.Log("old position " + old_position);
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

    // Update is called once per frame
    void Update()
    {
        // Any update logic
    }

    public void StorePosition()
    {
        old_position = this.transform.parent.GetComponent<Transform>().localPosition;
    }
}

