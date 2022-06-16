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
    GameObject left_wrist_label;
    GameObject right_wrist_label;

    MixedRealityPose pose;

    bool left_hand_seen = false;
    bool right_hand_seen = false;


    // Start is called before the first frame update
    void Start()
    {
        left_wrist_label = Instantiate(leftTextMarker, this.transform);
        right_wrist_label = Instantiate(rightTextMarker, this.transform);
    }

    // Update is called once per frame
    void Update()
    {
        // originally set to false, check if find the correct pose, then change to trueX
        //left_wrist.GetComponent<Renderer>().enabled = false;
        //right_wrist.GetComponent<Renderer>().enabled = false;

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Left, out pose))
        {
            if (!left_hand_seen)
            {
                left_wrist_label.GetComponent<Renderer>().enabled = true;
                left_hand_seen = true;
            }
            left_wrist_label.transform.position = pose.Position;
        }
        else
        {
            left_wrist_label.GetComponent<Renderer>().enabled = false;
        }

        if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Wrist, Handedness.Right, out pose))
        {
            if (!right_hand_seen)
            {
                right_wrist_label.GetComponent<Renderer>().enabled = true;
                right_hand_seen = true;
            }
            right_wrist_label.transform.position = pose.Position;
        }
        else
        {
            right_wrist_label.GetComponent<Renderer>().enabled = false;
        }

    }

    public void PalmAwayRight()
    {
        right_wrist_label.GetComponent<Renderer>().enabled = true;
    }

    public void PalmTowardsRight()
    {
        right_wrist_label.GetComponent<Renderer>().enabled = false;
    }

    public void PalmAwayLeft()
    {
        left_wrist_label.GetComponent<Renderer>().enabled = true;
    }

    public void PalmTowardsLeft()
    {
        left_wrist_label.GetComponent<Renderer>().enabled = false;
    }
}

