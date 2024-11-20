using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorTransform : MonoBehaviour
{
    //This script is used to transfer the position and rotation of a source object to a target object
    public Transform source;
    public Transform target;

    // Update is called once per frame
    void Update()
    {
        //transfer the transform of the source object to the target object
        //target.position = source.position;
        target.rotation = source.rotation;

        //transfer the x and z position of the source object to the target object
        target.position = new Vector3(source.position.x, target.position.y, source.position.z);
    }
}
