using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit;
using System.Collections;
using UnityEngine;

public class HandTrackingSensor : MonoBehaviour
{
    public GameObject rightHand;
    public GameObject leftHand;
    public GameObject wristTransform;
    public GameObject fingertipTransform;
    public GameObject placeholderPrefab;

    private GameObject placeholderInstance = null;
    private MeshRenderer placeholderMeshRenderer;

    void Start()
    {
        // You can add any initialization code here if needed
    }

    public void Trigger()
    {
        ServiceHandJointData();
        //StartCoroutine(TrackHand());
        StartCoroutine(TrackHandService());
    }


    IEnumerator TrackHand()
    {
        yield return new WaitForSeconds(1f); // Wait for 5 seconds before tracking hand

        FindHands(); // Find the right and left hand game objects

        if (rightHand != null && leftHand != null)
        {
            FindTransforms(); // Find wrist and fingertip transforms
            Invoke("RecordOffset",5f); // Instantiate placeholder and start disabling coroutine
        }
        else
        {
            Debug.Log("Right hand or left hand not found!");
        }
    }

    IEnumerator TrackHandService()
    {
        yield return new WaitForSeconds(1f); // Wait for 5 seconds before tracking hand

        ServiceHandJointData(); // Get hand joint data

        if (wristTransform != null && fingertipTransform != null)
        {
            Invoke("RecordOffset",5f); // Update finger position every 0.1 seconds
        }
        else
        {
            Debug.Log("Wrist or fingertip transform not found!");
        }
    }

    public void ServiceHandJointData()
    {
        var hand_joint_service = CoreServices.GetInputSystemDataProvider<IMixedRealityHandJointService>();
        
        if (hand_joint_service != null)
        {
            Transform index_tip_transform = hand_joint_service.RequestJointTransform(TrackedHandJoint.IndexTip, Handedness.Left);
            fingertipTransform = index_tip_transform.gameObject;

            Transform wrist_transform = hand_joint_service.RequestJointTransform(TrackedHandJoint.Wrist, Handedness.Right);
            wristTransform = wrist_transform.gameObject;
        }
    }

    void FindHands()
    {
        rightHand = GameObject.Find("Right_RiggedHandRight(Clone)");
        leftHand = GameObject.Find("Left_RiggedHandLeft(Clone)");
    }

    void FindTransforms()
    {
        wristTransform = rightHand.transform.Find("Wrist Proxy Transform").gameObject;
        fingertipTransform = leftHand.transform.Find("IndexTip Proxy Transform").gameObject;

    }

    void RecordOffset()
    {
        if (placeholderInstance == null && placeholderPrefab != null)
        {
            placeholderInstance = Instantiate(placeholderPrefab, fingertipTransform.transform.position, Quaternion.identity, wristTransform.transform);
            placeholderMeshRenderer = placeholderInstance.GetComponent<MeshRenderer>();
            //placeholderMeshRenderer.enabled = false;

            Invoke("DisableMeshRendererAfterDelay", 5f);
        }
        else
        {
            Debug.Log("Placeholder instance or prefab is null.");
        }
    }

    void DisableMeshRendererAfterDelay()
    {
        
        if (placeholderMeshRenderer != null)
        {
            placeholderMeshRenderer.enabled = false;
        }
        else
        {
            Debug.Log("Placeholder mesh renderer is null.");
        }
    }


    void FingerUpdate()
    {
        if (placeholderInstance != null)
        {
            placeholderInstance.transform.position = fingertipTransform.transform.position;
        }
    }
}
