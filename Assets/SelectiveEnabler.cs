using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectiveEnabler : MonoBehaviour
{
    //list of  activeable objects
    public GameObject[] managed_objects;
    
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
}
