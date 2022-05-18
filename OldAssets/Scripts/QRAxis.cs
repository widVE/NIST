using MRTKExtensions.QRCodes;
using UnityEngine;

public class QRAxis : MonoBehaviour
{
    [SerializeField]
    private QRTrackerController trackerController;

    private void Start()
    {
        trackerController.PositionSet += PoseFound;
    }

    private void PoseFound(object sender, Pose pose)
    {
        var childObj = transform.GetChild(0);
		if(childObj != null)
		{
			childObj.SetPositionAndRotation(pose.position, pose.rotation);
			childObj.gameObject.SetActive(true);
		}
    }
}