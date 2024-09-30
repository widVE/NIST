using System.Collections;
using System.Collections.Generic;
using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

using AsImpL;

public class ModelImportedEventArgs
{
    public GameObject model;
}

public class LocationModelLoader : ObjectImporter
{
    [Tooltip("Load the model from server after a location QR code has been scanned.")]
    public bool loadOnLocationChange = true;

    public event EventHandler<ModelImportedEventArgs> ModelImported;

    private GameObject model;
    private string locationId = "unknown";
    private bool modelIsReady = false;

    private string urlBase = "";

    // Store reference to newest GameObject for each surface, keyed on surface ID.
    private Dictionary<string, GameObject> surfaces = new();

    private ImportOptions importOptions = new ImportOptions() 
    { 
        zUp = false,

        // Pass our layer ID to the loaded model.
        //inheritLayer = true,

        // AsImpL seems to load the models rotated by 180 degrees. I think this fixes it.
        localEulerAngles = new Vector3(0, 180, 0),

        // We would like to set this to make the model invisible while loading,
        // but that seems to cause an error in AsImpL.
        //hideWhileLoading = true,

        // Rainbow shader
        defaultShaderName = "Graph/Point Surface",
    };

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

        model = new GameObject("model");
        model.transform.parent = this.transform;
    }

    void Start()
    {
        // Wait for a QR code to be scanned to fetch features from the correct location.
        QRScanner.Instance.LocationChanged += (o, ev) =>
        {
            if (loadOnLocationChange)
            {
                modelIsReady = false;
                locationId = ev.LocationID;
                model.name = ev.LocationID;

                var scheme = "http://";
                if (ev.UseHTTPS)
                    scheme = "https://";

                urlBase = $"{scheme}{ev.Server}/locations/{ev.LocationID}";

                string url = $"{urlBase}/model#model.obj";
                Debug.Log("Loading model from: " + url);

                ImportModelAsync("main", url, model.transform, importOptions);
            }
        };
    }

    public GameObject GetModel()
    {
        return model;
    }

    public void UpdateSurface(EasyVizAR.Surface surface)
    {
        if (modelIsReady)
        {
            string url = $"{urlBase}/surfaces/{surface.id}/surface.obj";
            Debug.Log("Loading model from: " + url);

            ImportModelAsync(surface.id, url, model.transform, importOptions);
        }
    }

    public void DeleteSurface(string surface_id)
    {
        if (surfaces.ContainsKey(surface_id))
        {
            Destroy(surfaces[surface_id]);
            surfaces.Remove(surface_id);
        }
    }

    protected override void OnModelCreated(GameObject obj, string absolutePath)
    {
        // Make the object disappear while loading.
        // We could use obj.SetActive(false), but that causes a bug in the loader code.
        obj.transform.localScale = new Vector3(0, 0, 0);

        base.OnModelCreated(obj, absolutePath);
    }

    protected override void OnImported(GameObject obj, string absolutePath)
    {
        base.OnImported(obj, absolutePath);

        // Disable rendering once the object has been loaded.
        obj.SetActive(false);

        // Restore the object scale before using it for the NavMesh.
        obj.transform.localScale = new Vector3(1, 1, 1);

        // Iterate over the components of the object and save a reference to each.
        // For location models, this iterates the individual surfaces, which each have their own surface ID.
        foreach (Transform tf in obj.transform)
        {
            if (surfaces.ContainsKey(tf.name))
            {
                Debug.Log("Destroy object " + tf.name);
                Destroy(surfaces[tf.name]);
            }
            surfaces[tf.name] = tf.gameObject;
        }

        // Also create a reference to the parent object. This ensures that for individually loaded
        // surfaces, we store a reference to the top-level game object, rather than its child.
        surfaces[obj.name] = obj;

        if (!modelIsReady)
        {
            modelIsReady = true;
            NavigationManager.Instance.InitializeNavMesh(obj);
        }
        else
        {
            NavigationManager.Instance.UpdateNavMesh(obj);
        }

        var eventArgs = new ModelImportedEventArgs()
        {
            model = obj,
        };
        ModelImported?.Invoke(this, eventArgs);
    }

}