using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

using Dummiesman;
using System.IO;
using UnityEngine.Networking;

public class ModelImportedEventArgs
{
    public string locationId;
    public string surfaceId;

    public GameObject model;
}

public class LocationModelLoader : MonoBehaviour
{
    [Tooltip("Load the model from server after a location QR code has been scanned.")]
    public bool loadOnLocationChange = true;

    [Tooltip("Load updated surfaces from the server.")]
    public bool loadUpdatedSurfaces = true;

    [Tooltip("Shader to use for rendering the loaded model.")]
    public Shader defaultShader;

    [Tooltip("Display model in editor play mode.")]
    public bool displayModelInEditor = true;

    [Tooltip("Minimum time (seconds) between updating navigation mesh. There is a performance impact to running this too often.")]
    public float updateNavMeshInterval = 60.0f;

    [Tooltip("Minimum number of newly added surfaces before considering updating NavMesh.")]
    public int newSurfaceThreshold = 8;

    [Tooltip("Minimum number of surface updates (new or updated) before considering updating NavMesh.")]
    public int updatedSurfaceThreshold = 30;

    public event EventHandler<ModelImportedEventArgs> ModelImported;

    private GameObject model;
    private string locationId = "unknown";
    private bool modelIsReady = false;

    private string urlBase = "";

    // Count updates that have occured since initially loading the model.
    private int newSurfaces = 0;
    private int updatedSurfaces = 0;

    // Store reference to newest GameObject for each surface, keyed on surface ID.
    private Dictionary<string, GameObject> surfaces = new();

    private OBJLoader loader;
    private Material defaultMaterial;

    // Queue of updated surface IDs to load
    private UniqueQueue<string> updateQueue = new();

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

        if (defaultShader == null)
        {
            defaultShader = Shader.Find("Graph/Point Surface");
        }
        defaultMaterial = new Material(defaultShader);

        loader = new OBJLoader();
        loader.SetDefaultMaterial(defaultMaterial);
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

                urlBase = $"{scheme}{ev.Server}";

                StartCoroutine(LoadLocationModel(ev.LocationID));
            }
        };
    }

    private IEnumerator LoadModel(string url, bool startActive, System.Action<GameObject> callbackOnLoaded)
    {
        Debug.Log("Loading model from: " + url);

        UnityWebRequest www = new UnityWebRequest(url);
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.SendWebRequest();

        var stream = new MemoryStream(www.downloadHandler.data);
        yield return loader.LoadAsync(stream, startActive, callbackOnLoaded);
    }

    private IEnumerator LoadLocationModel(string locationId)
    {
#if UNITY_EDITOR
        bool startActive = displayModelInEditor;
#else
        bool startActive = false;
#endif

        string url = $"{urlBase}/locations/{locationId}/model#model.obj";
        yield return LoadModel(url, startActive, (loadedObject) =>
        {
            loadedObject.name = locationId;
            loadedObject.transform.parent = transform;

            // Iterate over the components of the object and save a reference to each.
            // For location models, this iterates the individual surfaces, which each have their own surface ID.
            foreach (Transform tf in loadedObject.transform)
            {
                if (surfaces.ContainsKey(tf.name) && surfaces[tf.name] != null)
                {
                    Destroy(surfaces[tf.name]);
                }
                surfaces[tf.name] = tf.gameObject;
            }

            // Also create a reference to the parent object. This ensures that for individually loaded
            // surfaces, we store a reference to the top-level game object, rather than its child.
            if (surfaces.ContainsKey(locationId) && surfaces[locationId] != null)
            {
                Destroy(surfaces[locationId]);
            }
            surfaces[locationId] = loadedObject;

            newSurfaces = 0;
            updatedSurfaces = 0;

            model = loadedObject;
            modelIsReady = true;
            NavigationManager.Instance.InitializeNavMesh(loadedObject);

            HeadAttachedText.Instance.EnqueueMessage("Finished loading navigation map", 2.0f);

            var eventArgs = new ModelImportedEventArgs()
            {
                locationId = locationId,
                surfaceId = null,
                model = loadedObject,
            };
            ModelImported?.Invoke(this, eventArgs);

            StartCoroutine(LoadUpdatedSurfaces());
        });
    }

    private IEnumerator LoadComponentModel(string locationId, string surfaceId)
    {
        string url = $"{urlBase}/locations/{locationId}/surfaces/{surfaceId}/surface.obj";
        yield return LoadModel(url, true, (loadedObject) =>
        {
            loadedObject.name = surfaceId;
            loadedObject.transform.parent = surfaces[locationId].transform;

            var eventArgs = new ModelImportedEventArgs()
            {
                locationId = locationId,
                surfaceId = surfaceId,
                model = loadedObject,
            };
            ModelImported?.Invoke(this, eventArgs);

            // Iterate over the components of the object and save a reference to each.
            // For a surface mesh, there should be only one child.
            foreach (Transform tf in loadedObject.transform)
            {
                if (surfaces.ContainsKey(tf.name) && surfaces[tf.name] != null)
                {
                    Destroy(surfaces[tf.name]);
                }
                else
                {
                    newSurfaces++;
                }
                surfaces[tf.name] = tf.gameObject;
                tf.parent = surfaces[locationId].transform;
                updatedSurfaces++;
            }

            // Remove the wrapper object
            Destroy(loadedObject);
        });
    }

    private IEnumerator LoadUpdatedSurfaces()
    {
        var loopDelay = new WaitForSeconds(1.0f);

        while(true)
        {
            if (updateQueue.Count > 0)
            {
                string surfaceId = updateQueue.Dequeue();
                yield return LoadComponentModel(locationId, surfaceId);
            }
            else
            {
                yield return loopDelay;
            }
        }
    }

    private IEnumerator UpdateNavMesh()
    {
        var loopDelay = new WaitForSeconds(updateNavMeshInterval);

        while (true)
        {
            if (newSurfaces >= newSurfaceThreshold || updatedSurfaces >= updatedSurfaceThreshold)
            {
                // Call InitializeNavMesh instead of UpdateNavMesh because we are completely recomputing it and not just sending the changes.
                NavigationManager.Instance.InitializeNavMesh(model);

                newSurfaces = 0;
                updatedSurfaces = 0;
            }

            yield return loopDelay;
        }
    }

    public GameObject GetModel()
    {
        return model;
    }

    public void UpdateSurface(EasyVizAR.Surface surface)
    {
        if (modelIsReady && loadUpdatedSurfaces)
        {
            // We put the updated surface IDs into a work queue for loading one at a time in a coroutine.
            // This prevents us from trying to load too many surfaces at once, which hopefully helps performance.
            updateQueue.Enqueue(surface.id);
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

    public void UpdateLocalMesh(string surfaceId, MeshFilter filter)
    {
        if (modelIsReady)
        {
            if (surfaces.ContainsKey(surfaceId) && surfaces[surfaceId] != null)
            {
                Destroy(surfaces[surfaceId]);
            }
            else
            {
                newSurfaces++;
            }
            updatedSurfaces++;

            var obj = new GameObject(surfaceId);
            obj.transform.parent = model.transform;

            var newFilter = obj.AddComponent<MeshFilter>();
            newFilter.sharedMesh = filter.sharedMesh;

            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = defaultMaterial;

            surfaces[surfaceId] = obj;

            var eventArgs = new ModelImportedEventArgs()
            {
                locationId = locationId,
                surfaceId = surfaceId,
                model = obj,
            };
            ModelImported?.Invoke(this, eventArgs);
        }
    }
}
