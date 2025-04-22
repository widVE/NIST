using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOnManipulation : MonoBehaviour
{

    public GameObject activated_sign;
    public GameObject preveiw_sign;
    public float time_to_activate = 2f;

    public void Awake()
    {
        //invoke toggleactivea fter a 1 second delay
        Invoke("ToggleActive", time_to_activate);
    }

    public void ToggleActive()
    {
        activated_sign.SetActive(!activated_sign.activeSelf);
        preveiw_sign.SetActive(!preveiw_sign.activeSelf);
    }

}
