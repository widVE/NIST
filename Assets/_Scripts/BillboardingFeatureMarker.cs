using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardingFeatureMarker : MonoBehaviour
{
    public GameObject cam;
    public GameObject marker_parent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        foreach (Transform child in marker_parent.transform)
        {
            var markerObject = child.gameObject.GetComponent<MarkerObject>();

            // Skip objects that are not ordinary feature markers, which all have a MarkerObject attached.
            // These may be special objects (wall signs or 3D maps) that have a fixed orientation.
            if (!markerObject)
                continue;

            child.transform.LookAt(cam.transform);
            child.rotation = Quaternion.Euler(0f, child.transform.rotation.eulerAngles.y-180, 0f);

            var distanceParent = child.transform.Find("DistanceParent");
            if (distanceParent && distanceParent.childCount > 0)
            {
                var text = distanceParent.GetChild(0);
                text.transform.LookAt(cam.transform);
                text.rotation = Quaternion.Euler(0f, text.transform.rotation.eulerAngles.y + 180, 0f);
            }
        }
    }
}
