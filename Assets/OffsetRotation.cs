using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetRotation : MonoBehaviour
{
    public GameObject source_transform;

    // Update is called once per frame
    void Update()
    {
        this.transform.localEulerAngles = source_transform.transform.eulerAngles;
    }
}
