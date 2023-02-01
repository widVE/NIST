using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigation : MonoBehaviour
{
    GameObject markerSpawnParent = null;
    // Start is called before the first frame update
    void Start()
    {
        markerSpawnParent = this.transform.parent.gameObject.GetComponent<MapIconSpawn>().iconParent;
        if (!markerSpawnParent)
        {
            Debug.Log("Navigation: cannot find the icon parent");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            Debug.Log("Got to the Navigation Touch Update");
            Ray detectRay = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            RaycastHit hit;
            if (Physics.Raycast(detectRay, out hit))
            {
                if (hit.collider.tag == "Icon")
                {
                    //if the icon on the palmup map is touched then initiate line rendering 
                    RenderNavigationPath();
                }
            }
        }
        
    }

    public void RenderNavigationPath()
    {
        //TODO: this is where I should add the line render between the user and the desire landmark/icon
        Debug.Log("Touched the icon!");
    }
}
