using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    private GameObject headset_parent;
    // Start is called before the first frame update
    void Start()
    {
        headset_parent = GameObject.FindWithTag("Headset");

    }

    // Update is called once per frame
    void Update()
    {
        //rotation
        if (headset_parent != null)
        {
            //transform.rotation = new Quaternion(0.0f, 0.0f, headset_parent.transform.Find(transform.parent.gameObject.name).rotation.z,1.0f);
            //UnityEngine.Debug.Log("the rotation: " + headset_parent.transform.Find(transform.parent.gameObject.name).rotation);// + headset_parent.transform.Find(this.name).rotation);
            
        }
        
        
    }
}
