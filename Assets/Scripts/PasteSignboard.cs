using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PasteSignboard : MonoBehaviour
{
    public GameObject signboardPrefab;

    public float maxPlacementDistance = 6.0f;
    public int meshLayer = 31; // 31 = spatial awareness layer

    public bool triggerOnSpace = true;
    public bool triggerOnClick = true;

    void Update()
    {
        if ((triggerOnSpace && Input.GetKeyDown(KeyCode.Space)) || (triggerOnClick && Input.GetMouseButtonDown(0)))
        {
            PlaceSignboard();
        }
    }

    void PlaceSignboard()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int layerMask = 1 << meshLayer;
        if (Physics.Raycast(ray, out hit, maxPlacementDistance, layerMask))
        {
            float hitAngle = Vector3.Angle(hit.normal, -ray.direction); // always between 0 - 180
            Quaternion rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);

            Vector3 adjustedPosition = hit.point + hit.normal * 0.3f;

            Instantiate(signboardPrefab, adjustedPosition, rotation, this.transform);
        }
    }
}
