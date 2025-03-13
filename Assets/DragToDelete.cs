using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragToDelete : MonoBehaviour
{
    public GameObject trash_target_prefab;
    public GameObject trash_target_spawned;

    public bool delete_armed = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Function called when the object is dragged, this will instantiate the trash target prefab and store the reference to it in trash_target_spawned, this also arms the delete boolean, this will be called by an event
    public void OnDrag()
    {
        if (trash_target_prefab != null)
        {
            trash_target_spawned = Instantiate(trash_target_prefab);
            delete_armed = true;
        }
    }
}
