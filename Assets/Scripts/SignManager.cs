using System;
using System.Collections;
using System.Collections.Generic;
using EasyVizAR;
using Sign;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Dir
{
    top=3,
    bottom=1,
    left=0,
    right=2
}

public class SignItemData
{
    public Dir dir;
    public string locationName;
    public Sprite img;
}

public class SignDataJSON
{
    public string levelIndex;
    public string location;
    public List<SignItemData> data = new List<SignItemData>();
}

public class FeatureWithIcon
{
    public Feature feature;
    public Sprite icon;

    public FeatureWithIcon(Feature feature, Sprite icon)
    {
        this.feature = feature;
        this.icon = icon;
    }
}

public class SignManager : MonoBehaviour
{
    public VerticalLayoutGroup layout;

    public TextMeshProUGUI locationLabel;
    public TextMeshProUGUI levelLabel;

    public SignNavigationBoard_DirectionItem directionItemTempalte;
    public Transform diretionItemRoot;

    private List<SignNavigationBoard_DirectionItem> directionItemList = new();

    private Dictionary<Dir, List<FeatureWithIcon>> signData = new();

    private string location;
    private int levelIndex; // TODO

    private void Start()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        layout.enabled = false;

        CleanItemList();

        locationLabel.text = location;
        levelLabel.text = levelIndex.ToString();

        //TryAddFeatureByDirection(Dir.top);
        TryAddFeatureByDirection(Dir.left);
        TryAddFeatureByDirection(Dir.right);
        TryAddFeatureByDirection(Dir.bottom);

        layout.enabled = true;
    }

    private void TryAddFeatureByDirection(Dir direction)
    {
        if (signData.TryGetValue(direction, out List<FeatureWithIcon> features))
        {
            CreateDirectionItem(direction, features);
            //Debug_DisplayDebugNode(features);
        }
    }


    private void CreateDirectionItem(Dir direction, List<FeatureWithIcon> features)
    {
        var newDirectionItem =
            Instantiate(directionItemTempalte.gameObject, diretionItemRoot)
            .GetComponent<SignNavigationBoard_DirectionItem>();
        directionItemList.Add(newDirectionItem);
        newDirectionItem.gameObject.SetActive(true);
        newDirectionItem.UpdateFeatureData(direction, features);
    }

    private void CleanItemList()
    {
        for (int i = 0; i < directionItemList.Count; i++)
        {
            Destroy(directionItemList[i].gameObject);
        }
        directionItemList.Clear();
    }

    internal void UpdatePositionAndRotation(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        spawnPosition.y = 0.69f;
        transform.position = spawnPosition;
        var euler = spawnRotation.eulerAngles;
        euler.x = 0f;
        transform.eulerAngles = euler;
    }

    public void CleanList()
    {
        signData.Clear();
    }

    public void AddFeature(Dir direction, Feature feature, Sprite sprite)
    {
        if (signData.TryGetValue(direction, out List<FeatureWithIcon> featureList))
        {
            featureList.Add(new FeatureWithIcon(feature, sprite));
        }
        else
        {
            var newFeatureList = new List<FeatureWithIcon>();
            newFeatureList.Add(new FeatureWithIcon(feature, sprite));
            signData.Add(direction, newFeatureList);
        }
    }

    public void SetLocation(string slocation)
    {
        location = slocation;
    }
}