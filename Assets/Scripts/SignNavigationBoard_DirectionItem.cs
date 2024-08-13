using EasyVizAR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignNavigationBoard_DirectionItem : MonoBehaviour
{
    public UnityEngine.UI.Image directionIcon;
    public int placeholderTrigger = 3;
    public GameObject[] placeholders;

    public SignNavigationBoard_DirectionItem_FeatureItem featureItemTemplate;

    public Transform featureItemRoot;
    
    private List<SignNavigationBoard_DirectionItem_FeatureItem> itemList = new();

    private Vector3[] directionRotate = {
        new Vector3(0,0,0), 
        new Vector3(0,0,90), 
        new Vector3(0,0,180),
        new Vector3(0,0,270) 
    };

    internal void UpdateFeatureData(Dir direction, List<FeatureWithIcon> features)
    {
        CleanItemList();

        directionIcon.transform.localEulerAngles = directionRotate[(int)direction];

        for(int index = 0; index < placeholders.Length; index++)
        {
            placeholders[index].SetActive(features.Count > (placeholderTrigger * (index + 1)));
        }

        features.Sort((a, b) => {
            return a.feature.type.CompareTo(b.feature.type);
        });

        for (int i = 0; i < features.Count; i++)
        {
            var feature = features[i];
            var newFeatureItem = 
                Instantiate(featureItemTemplate.gameObject, featureItemRoot)
                .GetComponent<SignNavigationBoard_DirectionItem_FeatureItem>();

            newFeatureItem.gameObject.SetActive(true);
            newFeatureItem.UpdateFeatureData(feature);
            itemList.Add(newFeatureItem);
        }
    }

    private void CleanItemList()
    {
        for(int i = 0; i < itemList.Count; i++)
        {
            Destroy(itemList[i].gameObject);
        }
        itemList.Clear();
    }
}
