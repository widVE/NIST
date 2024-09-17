using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateOnManipulation : MonoBehaviour
{

    public GameObject activated_sign;
    public GameObject preveiw_sign;

    public void Awake()
    {
        //invoke toggleactivea fter a 1 second delay
        Invoke("ToggleActive", 5f);
    }

    public void ToggleActive()
    {
        activated_sign.SetActive(!activated_sign.activeSelf);
        preveiw_sign.SetActive(!preveiw_sign.activeSelf);
    }

}
