using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothTrail : MonoBehaviour
{
    public float height;
    public float offset;
    public GameObject current_headset_prefab;
    public TrailRenderer line_breadcrumbs;
    private GameObject feet;
    private float initYPos;
    // Start is called before the first frame update
    void Start()
    {
       
        feet = new GameObject("feet");
        Vector3 headsetPos = current_headset_prefab.GetComponent<Transform>().position;
        initYPos = headsetPos.y;
        feet.GetComponent<Transform>().position = new Vector3(headsetPos.x, Mathf.Abs(headsetPos.y - height), headsetPos.z);
        line_breadcrumbs.transform.parent = feet.transform;
        line_breadcrumbs.startWidth = 0.3f;
        line_breadcrumbs.endWidth = 0.3f;





    }

    // Update is called once per frame
    void Update()
    {
        Vector3 headsetPos = current_headset_prefab.GetComponent<Transform>().localPosition;
        if (Mathf.Abs(headsetPos.y - height) <= offset)
        {
            feet.GetComponent<Transform>().position = new Vector3(headsetPos.x, Mathf.Abs(initYPos - height), headsetPos.z);
        }
        else {
            feet.GetComponent<Transform>().position = new Vector3(headsetPos.x, Mathf.Abs(headsetPos.y - height), headsetPos.z);
           
        }
       

       
        
        
       
    }
}
