using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapUserTrackingOffset : MonoBehaviour
{
    //Game objects for the starting offset, the main camera, and the map frame offset, use underscores instead of camel case
    public GameObject starting_offset;
    public GameObject main_camera;
    public GameObject map_frame_offset;

    // Start is called before the first frame update
    void Start()
    {
        //get the main camera if null
        if (!main_camera) main_camera = GameObject.Find("Main Camera");               
    }

    // Update is called once per frame
    void Update()
    {
        var offset = new Vector3(-main_camera.transform.position.x, -main_camera.transform.position.z, 0) - starting_offset.transform.localPosition;
        map_frame_offset.transform.localPosition = offset;
    }
}
