using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnMarkerAction : MonoBehaviour
{
    public GameObject marker;
    public GameObject spawnRoot;
    public GameObject spawnParent;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void SpawnMarker()
    {
        Debug.Log("spawned markers");
        GameObject inputFeature = Instantiate(marker, spawnRoot.transform.position, spawnRoot.transform.rotation, spawnParent.transform);
        inputFeature.name = "input_feature";
    }
}
