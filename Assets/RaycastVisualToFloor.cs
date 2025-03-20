using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastVisualToFloor : MonoBehaviour
{
    [SerializeField] private GameObject floor_visuals;
    [SerializeField] private GameObject raycast_origin;
    [SerializeField] private float default_height = 1f;

    private GameObject floor_visuals_instance;

    private void Start()
    {
        raycast_origin = this.gameObject;
        floor_visuals_instance = Instantiate(floor_visuals, raycast_origin.transform.position, Quaternion.identity);
    }

    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(raycast_origin.transform.position, Vector3.down, out hit))
        {
            floor_visuals_instance.transform.position = new Vector3(raycast_origin.transform.position.x, hit.point.y, raycast_origin.transform.position.z);
            Debug.Log("Hit at " + hit.point.y);
        }
        else
        {
            floor_visuals_instance.transform.position = new Vector3(raycast_origin.transform.position.x, default_height, raycast_origin.transform.position.z);
            //Debug.Log("No Hit at " + hit.point.y);
        }
    }


}
