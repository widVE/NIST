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

    float time_app_start = 0;
    float tutorial_hand_hints_time = 45.0f;


    // Start is called before the first frame update
    void Start()
    {
        left_wrist_label = Instantiate(leftTextMarker, this.transform);
        right_wrist_label = Instantiate(rightTextMarker, this.transform);
        StartCoroutine(UpdateHandTracking());

        time_app_start = Time.time;
    }

    IEnumerator UpdateHandTracking()
    {
        while (true)
        {
            if ((Time.time - tutorial_hand_hints_time) > 0)
            {
                Destroy(left_wrist_label);
                Destroy(right_wrist_label);
                StopAllCoroutines();
            }

            yield return new WaitForSeconds(0.01f);

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
    }

    public void PalmAwayRightAlways()
    {
        //Debug.Log("Hand see");
        if (right_wrist_label) right_wrist_label.GetComponent<Renderer>().enabled = true;
    }

    public void PalmAwayRight()
    {
        if (right_wrist_label)
        {
            right_wrist_label.GetComponent<Renderer>().enabled = true;
        }
    }

    public void PalmTowardsRight()
    {
        if (right_wrist_label) right_wrist_label.GetComponent<Renderer>().enabled = false;
    }

    public void PalmAwayLeft()
    {
        if (left_wrist_label)
        {
            left_wrist_label.GetComponent<Renderer>().enabled = true;
        }
    }

    public void PalmTowardsLeft()
    {
        if(left_wrist_label) left_wrist_label.GetComponent<Renderer>().enabled = false;
    }
}

