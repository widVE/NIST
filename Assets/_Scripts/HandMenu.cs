using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class HandMenu : MonoBehaviour
{

    //bool menu_enable;
    public GameObject hand_menu;
    // Start is called before the first frame update
    void Start()
    {
        hand_menu.SetActive(true);
        //hand_menu.SetActive(false);
        //menu_enable = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //This function will check if the hand_menu is active or not, and then toggle it, it will also output the state to the debug console in unity
    [ContextMenu("ToggleHandMenu")]
    public void ToggleHandMenu()
    {
        if (hand_menu.activeSelf)
        {
            hand_menu.SetActive(false);
            UnityEngine.Debug.Log("Hand Menu is now OFF");
        }
        else
        {
            hand_menu.SetActive(true);
            UnityEngine.Debug.Log("Hand Menu is now ON");
        }
    }

    public void HandMenuActiveState(bool active_state)
    {
        hand_menu.SetActive(active_state);
    }
}
