using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.Input;

public class HandTracking : MonoBehaviour
{

    public GameObject leftTextMarker;
    public GameObject rightTextMarker;
    GameObject left_wrist;
    GameObject right_wrist;

    MixedRealityPose pose;


    // Start is called before the first frame update
    void Start()
    {
        left_wrist = Instantiate(leftTextMarker, this.transform);
        right_wrist = Instantiate(rightTextMarker, this.transform);

    }

    // Update is called once per frame
    void Update()
    {
        // originally set to false, check if find the correct pose, then change to true
        left_wrist.GetComponent<Renderer>().enabled = false;
        right_wrist.GetComponent<Renderer>().enabled = false;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Left, out pose))
        {
            left_wrist.GetComponent<Renderer>().enabled = true;
            left_wrist.transform.position = pose.Position;

        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Right, out pose))
        {
            right_wrist.GetComponent<Renderer>().enabled = true;
            right_wrist.transform.position = pose.Position;

        }

    }
}

