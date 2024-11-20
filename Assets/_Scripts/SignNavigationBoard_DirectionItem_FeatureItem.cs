using EasyVizAR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignNavigationBoard_DirectionItem_FeatureItem : MonoBehaviour
{
    public TMPro.TextMeshProUGUI nameLabel;
    public UnityEngine.UI.Image icon;
    internal void UpdateFeatureData(FeatureWithIcon data)
    {
        nameLabel.text = data.feature.name;
        icon.sprite = data.icon;
    }
}
