using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnMarkerAction : MonoBehaviour
{
    public GameObject marker;
    public GameObject spawnRoot;
    public GameObject spawnParent;

    // this variable is for C# event --> input
    private PlayerInput player_input;
    // Start is called before the first frame update
    void Start()
    {
        //for C# event
        player_input = GetComponent<PlayerInput>();
        player_input.onActionTriggered += Player_Input_OnActionTriggered;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // for unity event
    public void SpawnMarker(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("spawned markers");
            GameObject inputFeature = Instantiate(marker, spawnRoot.transform.position, spawnRoot.transform.rotation, spawnParent.transform);
            inputFeature.name = "input_feature";

        }
    }
    public void Player_Input_OnActionTriggered(InputAction.CallbackContext context)
    {
        Debug.Log(context);
    }
}
