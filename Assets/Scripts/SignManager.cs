using System;
using System.Collections;
using System.Collections.Generic;
using EasyVizAR;
using Sign;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

    public UnityAction OnManipulateSign;

    private List<SignNavigationBoard_DirectionItem> directionItemList = new();

    private Dictionary<Dir, List<FeatureWithIcon>> signData = new();

    private string location;
    private int levelIndex; // TODO


    [SerializeField]
    Sprite[] typeIcons;
    Dictionary<string, Sprite> typeIconDic = new();

    private EasyVizAR.Location evLocation;

    public UnityEngine.Events.UnityAction OnFeatureListReceived;

    private FeatureManager featureManager;
    private NavigationManager navigationManager;
    List<List<Vector3>> _pointCache = new();

    private void Awake()
    {
        InitTypeIcons();
        featureManager = GameObject.Find("FeatureManager").GetComponent<FeatureManager>();
        navigationManager = GameObject.Find("NavigationManager").GetComponent<NavigationManager>();
        location = featureManager.LocationName;
        UpdateNavigationSigns();
        RefreshView();
    }

    private void Start()
    {

    }



    private void InitTypeIcons()
    {
        for (int i = 0; i < typeIcons.Length; i++)
        {
            var typeIcon = typeIcons[i];
            if (typeIcon != null)
            {
                // special fix for bad-person
                if(typeIcon.name == "bad_person")
                {
                    typeIconDic["bad-person"] = typeIcon;
                }
                else
                {
                    typeIconDic[typeIcon.name] = typeIcon;
                }
            }
        }
    }

    

    IEnumerator tempRefresh()
    {
        yield return new WaitForSeconds(20);
        OnManipulate();
        print("Refresh Done");
    }

    IEnumerator TriggerLayoutRefresh()
    {
        yield return new WaitForUpdate();
        layout.enabled = true;
    }

    public void OnManipulate()
    {
        if (OnManipulateSign != null)
        {
            OnManipulateSign();
        }
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

        StartCoroutine(TriggerLayoutRefresh());
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

    public Sprite GetTypeIcon(string type)
    {
        if (typeIconDic.TryGetValue(type, out Sprite ret))
        {
            return ret;
        }
        return null;
    }


    private void RequestLocationName(string locationID)
    {
        EasyVizARServer.Instance.Get("locations/" + locationID, EasyVizARServer.JSON_TYPE, RequestLocationNameCallback);
    }

    private void RequestLocationNameCallback(string result)
    {
        if (result != "error")
        {
            evLocation = JsonUtility.FromJson<EasyVizAR.Location>(result);
        }
        else
        {
            Debug.Log("ERROR: " + result);
        }
    }

    [ContextMenu("Update Sign")]
    private void UpdateNavigationSigns()
    {
        if (featureManager == null)
        {
            Debug.LogError("SignNavigationManager:FeatureManager is null");
            return;
        }
        // user current position
        CleanList();
        _pointCache.Clear();


        var sourcePosition = Camera.main.transform.position;
        var features = featureManager.feature_list.features;
        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            var targetPosition =
                new Vector3(feature.position.x, feature.position.y, feature.position.z);

            if (navigationManager.GetDirection(sourcePosition, targetPosition, out Dir direction))
            {
                AddFeature(direction, feature, GetTypeIcon(feature.type));
                print(direction + ":" + feature.name);
            }
        }
        RefreshView();
    }
}