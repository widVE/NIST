using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

public class FloorFinderRayCast : MonoBehaviour
{
    Transform floor_point;
    
    Ray surface_ray;
    RaycastHit hit;

    public Transform user_head;
    
    //This will hold a transform ahead of the user based on the direcation they're looking and multiplied by a distance offset.
    Vector3 user_forward;
    [SerializeField] float forward_offset = 1;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // We create the rotation driven offset by getting the current position and adding it to the scaled forward vector
        user_forward = user_head.forward * forward_offset + user_head.position;

        surface_ray = new Ray(user_forward, Vector3.down);
        
        if (Physics.Raycast(surface_ray, out hit))
        {
            Debug.Log(hit.point);
            //floor_point.position = hit.point;
            //transform.position = new Vector3(user_head.position.x, hit.point.y, user_head.position.z + forward_offset);
            transform.position = new Vector3(user_forward.x, hit.point.y, user_forward.z);
        }
        // If you don't hit a ground you can take half height of user
        // This won't work the way we want it to because our QR is our zero, and that's not necessairly the case because it could be above the ground or on a different floor of the building
        // Raycasting to the ground would give us a way to figure out the difference between the user and the floor
        else
        {
            float half_height = user_head.transform.position.y / 2;
            transform.position = new Vector3(user_head.position.x, half_height, user_head.position.z);
        }
    }
}
