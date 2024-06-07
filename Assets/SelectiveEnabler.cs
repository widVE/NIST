using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectiveEnabler : MonoBehaviour
{
    //list of  activeable objects
    public GameObject[] managed_objects;
    public GameObject[] managed_colliders;
    public GameObject[] managed_renderers;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //enable only the argument object and disable all others
    public void EnableObject(GameObject activated_object)
    {
        foreach (GameObject game_object in managed_objects)
        {
            if (game_object == activated_object)
            {
                game_object.SetActive(true);
            }
            else
            {
                game_object.SetActive(false);
            }
        }
    }


    // same as EnableObject, but this time it only affects the Unity BoxCollider component
    public void EnableObjectCollider(GameObject activated_object)
    {
        foreach (GameObject game_object in managed_colliders)
        {
            if (game_object == activated_object)
            {
                game_object.GetComponent<BoxCollider>().enabled = true;
            }
            else
            {
                game_object.GetComponent<BoxCollider>().enabled = false;
            }
        }
    }

    // same as EnableObject, but this time it only affects the Unity MeshRenderer component
    public void EnableObjectRenderer(GameObject activated_object)
    {
        foreach (GameObject game_object in managed_renderers)
        {
            if (game_object == activated_object)
            {
                game_object.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                game_object.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }
    
    // same as rendered but turns it off
    public void DisableObjectRenderer(GameObject activated_object)
    {
        foreach (GameObject game_object in managed_renderers)
        {
            if (game_object == activated_object)
            {
                game_object.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }
}
