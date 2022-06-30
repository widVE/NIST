using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleObject : MonoBehaviour
{
    public void ToggleMe()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
