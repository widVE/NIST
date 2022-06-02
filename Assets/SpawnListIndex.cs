using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnListIndex : MonoBehaviour
{
    public List<GameObject> spawn_list = null;
    public GameObject spawn_root;

    public void spawnObjectAtIndex(int index)
    {
        GameObject maker_to_spawn = spawn_list[index];
        Instantiate(maker_to_spawn, spawn_root.transform);
    }

}
