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
            child.transform.LookAt(cam.transform);
            child.rotation = Quaternion.Euler(0f, child.transform.rotation.eulerAngles.y-180, 0f);
            if (child.transform.Find(string.Format("DistanceParent")).childCount > 0)
            {
                var text = child.transform.Find(string.Format("DistanceParent")).GetChild(0);
                text.transform.LookAt(cam.transform);
                text.rotation = Quaternion.Euler(0f, text.transform.rotation.eulerAngles.y + 180, 0f);

            }


        }
    }
}
