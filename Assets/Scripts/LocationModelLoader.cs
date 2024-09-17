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

    private string locationId = "unknown";
    private GameObject model;
    private bool modelIsReady = false;

    private string urlBase = "";

    // Store reference to newest GameObject for each surface, keyed on surface ID.
    private Dictionary<string, GameObject> surfaces = new();

    private ImportOptions importOptions = new ImportOptions() 
    { 
        zUp = false,

        // Pass our layer ID to the loaded model.
        inheritLayer = true,

        // AsImpL seems to load the models rotated by 180 degrees. I think this fixes it.
        localEulerAngles = new Vector3(0, 180, 0),

        // We would like to set this to make the model invisible while loading,
        // but that seems to cause an error in AsImpL.
        //hideWhileLoading = true,
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

                var scheme = "http://";
                if (ev.UseHTTPS)
                    scheme = "https://";

                urlBase = $"{scheme}{ev.Server}/locations/{ev.LocationID}";

                string url = $"{urlBase}/model#model.obj";
                Debug.Log("Loading model from: " + url);

                ImportModelAsync(locationId, url, transform, importOptions);
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

            ImportModelAsync(surface.id, url, transform, importOptions);
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
        // We want to have the game object inherit our layer before it starts loading
        // data rather than after, which is how AsImpL does it. In that way, if our
        // layer is set to one that is not rendered, the partially loaded object will
        // never be visible.
        obj.layer = this.gameObject.layer;

        base.OnModelCreated(obj, absolutePath);
    }

    protected override void OnImported(GameObject obj, string absolutePath)
    {
        base.OnImported(obj, absolutePath);

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
            model = obj;
            modelIsReady = true;

            NavigationManager.Instance.InitializeNavMesh(model);
        }
        else
        {
            NavigationManager.Instance.UpdateNavMesh(obj);
        }
    }
}