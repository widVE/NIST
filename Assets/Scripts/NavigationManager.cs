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
    public GameObject mapPathGameObject;

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
    private LineRenderer mapLineRenderer;

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
    public void SetTarget(Vector3 position)
    {
        targetPosition = position;
        targetIsSet = true;
    }

    // Start is called before the first frame update
    IEnumerator Start()
    {
        myNavMeshSurface = GetComponent<NavMeshSurface>();
        myLineRender = GetComponent<LineRenderer>();

        if (mapPathGameObject)
            mapLineRenderer = mapPathGameObject.GetComponent<LineRenderer>();

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
        {
            Debug.Log("path not found");
            return false;
        }

        Debug.Log("Path found");
        foreach (Vector3 coord in path.corners)
        {
            Debug.Log(coord);
        }
   
        return GiveDirectionsToUser(path.corners, locationId, deviceId, color, label);
    }

    /*
     * Give visual navigation directions to another user.
     * 
     * This function takes a given list of Vector3 points and sends them to the EasyVizAR server
     * to then be forwarded to the appropriate headset.
     * 
     * locationId: Location for which the path was calculated.
     * deviceId: Headset or other device that should receive the path.
     * color: Path display color, which might be shown on command dashboards.
     *        It is recommended to use the target headset color, so multiple paths look visually distinct on the dashboard.
     *        Since the target user headset only has one path to display, it may ignore this color and display a hot-cold gradient instead.
     * label: Label text for the path. It is recommended to generate meaningful text like "Directions for <headset name>"
     *        or "Directions to <waypoint name>". This text will appear on the dashboard and may be displayed in the headset.
     */
    public static bool GiveDirectionsToUser(Vector3[] points, string locationId, string deviceId, string color, string label)
    {
        EasyVizAR.NewMapPath mapPath = new();
        mapPath.location_id = locationId;
        mapPath.mobile_device_id = deviceId;
        mapPath.type = "navigation";
        mapPath.color = color;
        mapPath.label = label;
        mapPath.points = points;


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

        EasyVizARServer.Instance.TryGetHeadsetID(out string myDeviceId);

        if (path.type == "navigation" && path.mobile_device_id == myDeviceId)
        {
            // If this is a navigation path intended for me, then show the nice color gradient.
            lr.startColor = navigationStartColor;
            lr.endColor = navigationEndColor;

            // Maybe allow other holograms (eg. markers) to render over the line, but
            // this navigation line should be higher priority than any of the other lines.
            lr.sortingOrder = -10;

            // Also display our navigation path on the hand-attached map.
            if (mapLineRenderer)
            {
                mapLineRenderer.positionCount = path.points.Length;
                mapLineRenderer.SetPositions(path.points);
            }
        }
        else if (ColorUtility.TryParseHtmlString(path.color, out Color color))
        {
            // Otherwise, try to show the suggested line color, e.g. for other users' navigation paths.
            lr.startColor = color;
            lr.endColor = color;

            lr.sortingOrder = -100;
        }
        else
        {
            // Magenta in the case that nothing else works.
            lr.startColor = Color.magenta;
            lr.endColor = Color.magenta;

            lr.sortingOrder = -1000;
        }

        lr.positionCount = path.points.Length;
        lr.SetPositions(path.points);
    }

    public void DeleteMapPath(int mapPathId)
    {
        if (mapPathLineRenderers.ContainsKey(mapPathId))
        {
            Destroy(mapPathLineRenderers[mapPathId]);
            mapPathLineRenderers.Remove(mapPathId);
        }
    }

    private void LoadMapPaths(string newLocationId)
    {
        // Remove any existing map paths assuming we are changing location
        foreach (var path in mapPathLineRenderers.Values)
        {
            Destroy(path);
        }
        mapPathLineRenderers.Clear();

        // This retrieves paths for the given deviceId and paths with null deviceId (intended for everyone).
        string url = $"locations/{newLocationId}/map-paths?envelope=map_paths";

        EasyVizARServer.Instance.Get(url, EasyVizARServer.JSON_TYPE, delegate (string result)
        {
            Debug.Log("EasyVizARServer.Instance.Get");
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

    internal bool GetDirection(Vector3 sourcePosition, Vector3 targetPosition, out Dir direction)
    {
        direction = Dir.bottom;

        NavMeshPath path = new();
        if (NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path))
        {
            if (path != null && path.corners.Length > 0)
            {
                var firstCorner = path.corners[1];
                Vector3 directionToTarget = firstCorner - sourcePosition;
                Vector3 normalizedDirection = directionToTarget.normalized;
                direction = GetDirection(normalizedDirection);

                //Debug_DisplayPath(path.corners);

                return true;
            }
        }
        return false;
    }

    private Dir GetDirection(Vector3 direction)
    {
        if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
        {
            if (direction.z > 0)
            {
                if (direction.x > 0)
                {
                    return Dir.right;
                }
                else
                {
                    return Dir.left;
                }
            }
            else
            {
                return Dir.bottom;
            }
        }
        else
        {
            if (direction.x > 0)
            {
                return Dir.right;
            }
            else
            {
                return Dir.left;
            }
        }
    }

}
