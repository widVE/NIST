using System;
using System.Collections;
using System.Collections.Generic;
using Sign;
using TMPro;
using UnityEngine;

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


public class SignManager : MonoBehaviour
{
    private SignDataJSON jsondata;


    public TextMeshPro LevelInedexTextMeshPro;
    public TextMeshPro LocationTextMeshPro;

    public GameObject go;

    public Sprite[] imgs;
    
    private void Start()
    {
        TestInitA();
        RefreshView();
    }

    private void RefreshView()
    {
        LevelInedexTextMeshPro.text = jsondata.levelIndex;
        LocationTextMeshPro.text = jsondata.location;

        var parentChildCount = go.transform.parent.childCount;

        for (int i = 1; i < parentChildCount; i++)
        {
            Destroy(go.transform.parent.GetChild(i).gameObject);
        }

        foreach (var signItemData in jsondata.data)
        {
            var instantiate = Instantiate(go, go.transform.parent);
            instantiate.transform.localPosition = Vector3.zero;

            instantiate.transform.localRotation = Quaternion.identity;

            instantiate.transform.localScale = Vector3.one;
            instantiate.gameObject.SetActive(true);
            instantiate.GetComponent<SignItemView>().SetData((signItemData));
        }
    }

    private void TestInitA()
    {
        jsondata = new SignDataJSON();
        jsondata.levelIndex = "1";
        jsondata.location = "building1";
        jsondata.data.Add(new SignItemData() { dir = Dir.right, locationName = "laboratory" ,img = imgs[0]});
        jsondata.data.Add(new SignItemData() { dir = Dir.left, locationName = "WC",img = imgs[3] });
        jsondata.data.Add(new SignItemData() { dir = Dir.top, locationName = "fireExtinguisher" ,img = imgs[1]});
        jsondata.data.Add(new SignItemData() { dir = Dir.bottom, locationName = "elevator" ,img = imgs[2]});
    }

    private void TestInitB()
    {
        jsondata = new SignDataJSON();
        jsondata.levelIndex = "2";
        jsondata.location = "building2";
        jsondata.data.Add(new SignItemData() { dir = Dir.top, locationName = "laboratory2" });
        jsondata.data.Add(new SignItemData() { dir = Dir.bottom, locationName = "WC2" });
        jsondata.data.Add(new SignItemData() { dir = Dir.left, locationName = "elevator2" });
        jsondata.data.Add(new SignItemData() { dir = Dir.right, locationName = "fireExtinguisher2" });

    }

    private void TestInitC()
    {
        jsondata = new SignDataJSON();
        jsondata.levelIndex = "3";
        jsondata.location = "building3";
        jsondata.data.Add(new SignItemData() { dir = Dir.top, locationName = "laboratory3" });
        jsondata.data.Add(new SignItemData() { dir = Dir.bottom, locationName = "WC3" });
        jsondata.data.Add(new SignItemData() { dir = Dir.left, locationName = "elevator3" });
        jsondata.data.Add(new SignItemData() { dir = Dir.right, locationName = "fireExtinguisher3" });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TestInitA();
            RefreshView();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestInitB();
            RefreshView();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            TestInitC();
            RefreshView();
        }
    }
}