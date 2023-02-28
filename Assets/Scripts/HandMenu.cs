using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class HandMenu : MonoBehaviour
{

    bool menu_enable;
    public GameObject hand_menu;
    // Start is called before the first frame update
    void Start()
    {
        hand_menu.SetActive(false);
        menu_enable = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SummonMenu()
    {
        UnityEngine.Debug.Log("Got to SummonMenu()");

        if (!menu_enable)
        {
            UnityEngine.Debug.Log("set the menu active");
            hand_menu.SetActive(true);
            menu_enable = true;
        }
        else
        {
            UnityEngine.Debug.Log("set the menu inactive");
            hand_menu.SetActive(false);
            menu_enable = false;
        }

    }
}
