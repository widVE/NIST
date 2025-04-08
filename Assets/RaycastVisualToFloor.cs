using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastVisualToFloor : MonoBehaviour
{
    [SerializeField] private List<GameObject> floor_visuals;
    [SerializeField] private GameObject raycast_origin;
    [SerializeField] private float default_height = 1f;

    private GameObject floor_visuals_instance;

    private int visuals_index = 0;

    private void Start()
    {
        raycast_origin = this.gameObject;
        floor_visuals_instance = Instantiate(floor_visuals[visuals_index], raycast_origin.transform.position, Quaternion.identity);
    }

    public void CycleVisuals()
    {
        visuals_index++;

        if (visuals_index >= floor_visuals.Count)
        {
            visuals_index = 0;
        }

        Destroy(floor_visuals_instance);

        floor_visuals_instance = Instantiate(floor_visuals[visuals_index], raycast_origin.transform.position, Quaternion.identity);
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
