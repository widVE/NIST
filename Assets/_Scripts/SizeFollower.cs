using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SizeFollower : MonoBehaviour
{
    public RectTransform target;
    private RectTransform selfTransform;
    private void Start()
    {
        selfTransform = GetComponent<RectTransform>();
    }
    void Update()
    {
        selfTransform.anchoredPosition = target.anchoredPosition;
        selfTransform.sizeDelta = target.sizeDelta;
    }
}
