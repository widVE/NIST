using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

using AsImpL;

public class LocationModelLoader : ObjectImporter
{
    [Tooltip("Load the model from server after a location QR code has been scanned.")]
    public bool loadOnLocationChange = true;

    private GameObject model;
    private bool modelIsReady = false;

    public static LocationModelLoader Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("Warning: multiple instances of LocationModelLoader created when there should only be one.");
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // Wait for a QR code to be scanned to fetch features from the correct location.
        QRScanner.Instance.LocationChanged += (o, ev) =>
        {
            if (loadOnLocationChange)
            {
                modelIsReady = false;

                var scheme = "http://";
                if (ev.UseHTTPS)
                    scheme = "https://";

                string url = $"{scheme}{ev.Server}/locations/{ev.LocationID}/model#model.obj";
                Debug.Log("Loading model from: " + url);

                var options = new ImportOptions();
                options.zUp = false;

                // Pass our layer ID to the loaded model.
                options.inheritLayer = true;

                // AsImpL seems to load the models rotated by 180 degrees. I think this fixes it.
                options.localEulerAngles = new Vector3(0, 180, 0);

                // We would like to set this to make the model invisible while loading,
                // but that seems to cause an error in AsImpL.
                //options.hideWhileLoading = true;

                ImportModelAsync("model", url, transform, options);
            }
        };
    }

    public GameObject GetModel()
    {
        return model;
    }

    protected override void OnImportingComplete()
    {
        base.OnImportingComplete();

        var child = transform.Find("model");
        if (child)
        {
            model = child.gameObject;
            modelIsReady = true;

            NavigationManager.Instance.UpdateNavMesh(model);
        }
    }
}