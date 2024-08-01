using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavigationManager : MonoBehaviour
{
    public GameObject qrScanner;

    // One of these can be set in the editor to load a mesh at startup.
    // They can also be altered during runtime by calling UpdateNavMesh.
    public Mesh mesh;
    public GameObject meshGameObject;

    // A target can be set in the editor or by calling SetTarget.
    // After a target has been set, if a path can be found, it will be displayed
    // using the attached LineRenderer.
    public bool targetIsSet = false;
    public Vector3 targetPosition;

    public Material pathMaterial;

    public Color navigationStartColor = Color.cyan;
    public Color navigationEndColor = Color.magenta;

    private NavMeshData navMeshData;

    private NavMeshSurface myNavMeshSurface;
    private LineRenderer myLineRender;

    private bool foundPath = false;
    private NavMeshPath path;

    // Location of this device, which will be set after scanning a QR code.
    private string locationId = "";

    private Dictionary<int, GameObject> mapPathLineRenderers = new();

    private NavMeshBuildSource BuildSourceFromMesh(Mesh mesh)
    {
        var src = new NavMeshBuildSource();
        src.transform = transform.localToWorldMatrix;
        src.shape = NavMeshBuildSourceShape.Mesh;
        src.size = mesh.bounds.size;
        src.sourceObject = mesh;
        return src;
    }

    // Update the global NavMesh from a Mesh object.
    // This could be called with an externally-sourced mesh to load a saved map for navigation.
    public AsyncOperation UpdateNavMesh(Mesh mesh)
    {
        this.mesh = mesh;

        var source = BuildSourceFromMesh(mesh);

        var sourceList = new List<NavMeshBuildSource>();
        sourceList.Add(source);

        var settings = NavMesh.GetSettingsByID(0);
        var bounds = mesh.bounds;

        return NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, sourceList, bounds);
    }

    // Update the global NavMesh from a GameObject containing one or more meshes.
    // This could be called with an externally-sourced mesh to load a saved map for navigation.
    public AsyncOperation UpdateNavMesh(GameObject meshGameObject)
    {
        this.meshGameObject = meshGameObject;

        var sourceList = new List<NavMeshBuildSource>();

        var bounds = new Bounds();
        var components = meshGameObject.GetComponentsInChildren<MeshFilter>();

        foreach (var mf in components)
        {
            var source = BuildSourceFromMesh(mf.sharedMesh);
            sourceList.Add(source);

            bounds.Encapsulate(mf.sharedMesh.bounds);
        }

        var settings = NavMesh.GetSettingsByID(0);

        return NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, sourceList, bounds);
    }

    // Set a navigation target.
    // This will result in a visible navigation cue if a path can be found.
    void SetTarget(Vector3 position)
    {
        targetPosition = position;
        targetIsSet = true;
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        myNavMeshSurface = GetComponent<NavMeshSurface>();
        myLineRender = GetComponent<LineRenderer>();

        path = new();
        navMeshData = new();

        NavMesh.AddNavMeshData(navMeshData);

        // Potentially load a built-in mesh at start-up for testing purposes.
        if (mesh)
            yield return UpdateNavMesh(mesh);
        else if (meshGameObject)
            yield return UpdateNavMesh(meshGameObject);

        // Wait for a QR code to be scanned to fetch features from the correct location.
        if (qrScanner)
        {
            var scanner = qrScanner.GetComponent<QRScanner>();
            scanner.LocationChanged += (o, ev) =>
            {
                LoadMapPaths(ev.LocationID);
                locationId = ev.LocationID;
            };
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If a target has been set, try to find a path from the current position to the target.
        // If a path can be found, it is displayed using the attached LineRenderer.
        // This code uses the global NavMesh, so other scripts could take advantage of that, as well.
        if (targetIsSet)
        {
            var sourcePosition = Camera.main.transform.position;
            foundPath = NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path);
            myLineRender.positionCount = path.corners.Length;
            myLineRender.SetPositions(path.corners);
        }
    }

    /*
     * Give visual navigation directions to another user.
     * 
     * This function uses the local navigation mesh to calculate a path between startPosition and endPosition.
     * If a path can be found, we send the path to the EasyVizAR server to be forwarded to the appropriate
     * headset. It is important that the local NavMesh has been constructed before calling this, eg. UpdateNavMesh
     * has been called at least once.
     * 
     * locationId: Location for which the path was calculated.
     * deviceId: Headset or other device that should receive the path.
     * color: Path display color, which might be shown on command dashboards.
     *        It is recommended to use the target headset color, so multiple paths look visually distinct on the dashboard.
     *        Since the target user headset only has one path to display, it may ignore this color and display a hot-cold gradient instead.
     * label: Label text for the path. It is recommended to generate meaningful text like "Directions for <headset name>"
     *        or "Directions to <waypoint name>". This text will appear on the dashboard and may be displayed in the headset.
     */
    public bool GiveDirectionsToUser(Vector3 startPosition, Vector3 endPosition, string locationId, string deviceId, string color, string label)
    {
        foundPath = NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, path);
        if (!foundPath)
            return false;

        EasyVizAR.NewMapPath mapPath = new();
        mapPath.location_id = locationId;
        mapPath.mobile_device_id = deviceId;
        mapPath.type = "navigation";
        mapPath.color = color;
        mapPath.label = label;
        mapPath.points = path.corners;

        //Serialize the feature into JSON
        var data = JsonUtility.ToJson(mapPath);

        string url = $"locations/{locationId}/map-paths?mobile_device_id={deviceId}&type=navigation";
        EasyVizARServer.Instance.Put(url, EasyVizARServer.JSON_TYPE, data, delegate (string result)
        {
            // callback for server request
        });

        return true;
    }

    public void UpdateMapPath(EasyVizAR.MapPath path)
    {
        GameObject lineObject;
        LineRenderer lr;

        if (mapPathLineRenderers.ContainsKey(path.id))
        {
            // Reuse existing LineRenderer
            lineObject = mapPathLineRenderers[path.id];
            lr = lineObject.GetComponent<LineRenderer>();
        }
        else
        {
            // Create new LineRenderer
            lineObject = new GameObject();
            lineObject.name = $"map-path-{path.id}";
            lineObject.transform.parent = transform;

            lr = lineObject.AddComponent<LineRenderer>();
            lr.material = pathMaterial;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;

            // These settings may improve rendering performance.
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;

            mapPathLineRenderers[path.id] = lineObject;
        }

        if (path.type == "navigation" && path.mobile_device_id != "")
        {
            lr.startColor = navigationStartColor;
            lr.endColor = navigationEndColor;
        }
        else if (ColorUtility.TryParseHtmlString(path.color, out Color color))
        {
            lr.startColor = color;
            lr.endColor = color;
        }
        else
        {
            lr.startColor = Color.magenta;
            lr.endColor = Color.magenta;
        }

        lr.positionCount = path.points.Length;
        lr.SetPositions(path.points);
    }

    private void LoadMapPaths(string newLocationId)
    {
        // Remove any existing map paths assuming we are changing location
        foreach (var path in mapPathLineRenderers.Values)
        {
            Destroy(path);
        }
        mapPathLineRenderers.Clear();

        EasyVizARServer.Instance.TryGetHeadsetID(out string deviceId);

        // This retrieves paths for the given deviceId and paths with null deviceId (intended for everyone).
        string url = $"locations/{newLocationId}/map-paths?envelope=map_paths&inflate_vectors=T&mobile_device_id={deviceId}";
        EasyVizARServer.Instance.Get(url, EasyVizARServer.JSON_TYPE, delegate (string result)
        {
            if (result != "error")
            {
                var mapPathList = JsonUtility.FromJson<EasyVizAR.MapPathList>(result);
                foreach (var path in mapPathList.map_paths)
                {
                    UpdateMapPath(path);
                }
            }
            else
            {
                Debug.Log(result);
            }
        });
    }
}