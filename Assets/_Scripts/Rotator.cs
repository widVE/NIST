using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float y_rotation_degree = 0.1f;

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(0,y_rotation_degree,0);
        
    }
}
