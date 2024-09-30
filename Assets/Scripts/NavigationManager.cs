using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;


class MeshData
{
    public GameObject mesh;
    public int sourceIndex;
    public NavMeshBuildSource navMeshBuildSource;
    public Bounds bounds;
}


public class NavigationManager : MonoBehaviour
{
    // One of these can be set in the editor to load a mesh at startup.
    // They can also be altered during runtime by calling UpdateNavMesh.
    public GameObject local_mesh_testing;

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

    private bool is_path_found = false;
    private NavMeshPath nav_mesh_path;

    // Location of this device, which will be set after scanning a QR code.
    private string locationId = "";

    private EasyVizAR.MapPath easyViz_map_path = null;

    // All navmesh data, keyed by surface ID
    private Dictionary<string, MeshData> navMeshSources = new();

    public Dictionary<int, GameObject> mapPathLineRenderers = new();

    public static NavigationManager Instance { get; private set; }

    private Coroutine updatePathCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of NavigationManager created when there should be only one.");
        }
        else
        {
            Instance = this;
        }
    }

    // Create a NavMeshBuildSource from a Mesh object.
    private NavMeshBuildSource BuildSourceFromMesh(Mesh mesh)
    {
        var nav_mesh_source = new NavMeshBuildSource();
        nav_mesh_source.transform = transform.localToWorldMatrix;
        nav_mesh_source.shape = NavMeshBuildSourceShape.Mesh;
        nav_mesh_source.size = mesh.bounds.size;
        nav_mesh_source.sourceObject = mesh;
        return nav_mesh_source;
    }

    private NavMeshBuildSource BuildSourceFromMesh(Mesh mesh, Transform tf)
    {
        var nav_mesh_source = new NavMeshBuildSource();
        nav_mesh_source.transform = tf.localToWorldMatrix;
        nav_mesh_source.shape = NavMeshBuildSourceShape.Mesh;
        nav_mesh_source.size = mesh.bounds.size;
        nav_mesh_source.sourceObject = mesh;
        return nav_mesh_source;
    }
    public AsyncOperation InitializeNavMesh(GameObject meshGameObject)
    {
        this.local_mesh_testing = meshGameObject;

        navMeshSources = new();
        return UpdateNavMesh(meshGameObject);
    }

    public AsyncOperation UpdateNavMesh(GameObject meshGameObject)
    {
        var components = meshGameObject.GetComponentsInChildren<MeshFilter>();

        foreach (var mesh_filter in components)
        {
            // Our models may have needed rotation or other transformations after loading.
            // We will get the mesh bounds from the Renderer, which returns the bounds in
            // world space, rather than from the MeshFilter, which returns them in local
            // space.
            var renderer = mesh_filter.gameObject.GetComponent<Renderer>();

            var nav_mesh_source = BuildSourceFromMesh(mesh_filter.sharedMesh, meshGameObject.transform);

            var meshData = new MeshData()
            {
                mesh = mesh_filter.transform.gameObject,
                sourceIndex = navMeshSources.Count,
                navMeshBuildSource = nav_mesh_source,
                bounds = renderer.bounds,
            };
            navMeshSources[mesh_filter.name] = meshData;
        }

        return RebuildNavMesh();
    }

    private AsyncOperation RebuildNavMesh()
    {
        var nav_mesh_source_list = new List<NavMeshBuildSource>();
        var bounds = new Bounds();

        foreach (var meshData in navMeshSources.Values)
        {
            nav_mesh_source_list.Add(meshData.navMeshBuildSource);
            bounds.Encapsulate(meshData.bounds);
        }

        var nav_mesh_build_settings = NavMesh.GetSettingsByID(0);
        return NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, nav_mesh_build_settings, nav_mesh_source_list, bounds);
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

        nav_mesh_path = new();
        navMeshData = new();

        NavMesh.AddNavMeshData(navMeshData);

        // Potentially load a built-in mesh at start-up for testing purposes.
        if (local_mesh_testing)
            yield return UpdateNavMesh(local_mesh_testing);

        // Wait for a QR code to be scanned to fetch features from the correct location.
        QRScanner.Instance.LocationChanged += (o, ev) =>
        {
            LoadServerMapPaths(ev.LocationID);
            locationId = ev.LocationID;
        };
    }

    // Update is called once per frame
    void Update()
    {
        // Start the coroutine if it hasn't been started yet
        if (updatePathCoroutine == null)
        {
            updatePathCoroutine = StartCoroutine(UpdatePathCoroutine());
        }
    }

    // Move the logic of the Update function into a coroutine called UpdatePathCoroutine so that it can be called less frequently.
    private IEnumerator UpdatePathCoroutine()
    {
        while (true)
        {
            // If a target has been set, try to find a path from the current position to the target.
            // If a path can be found, it is displayed using the attached LineRenderer.
            // This code uses the global NavMesh, so other scripts could take advantage of that, as well.
            if (targetIsSet)
            {
                var sourcePosition = Camera.main.transform.position;
                is_path_found = NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, nav_mesh_path);
                myLineRender.positionCount = nav_mesh_path.corners.Length;
                myLineRender.SetPositions(nav_mesh_path.corners);
            }

            yield return new WaitForSeconds(0.1f);
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
        is_path_found = NavMesh.CalculatePath(startPosition, endPosition, NavMesh.AllAreas, nav_mesh_path);
        if (!is_path_found)
        {
            Debug.Log("path not found");
            return false;
        }

        Debug.Log("Path found");
        foreach (Vector3 coord in nav_mesh_path.corners)
        {
            Debug.Log(coord);
        }
   
        return GiveDirectionsToUser(nav_mesh_path.corners, locationId, deviceId, color, label);
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

    public void UpdateMapPathLineRenderers(EasyVizAR.MapPath path)
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

            // Save the latest navigation path for other game objects to query.
            easyViz_map_path = path;
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

    public void DeletePathRenderers(int mapPathId)
    {
        if (mapPathLineRenderers.ContainsKey(mapPathId))
        {
            Destroy(mapPathLineRenderers[mapPathId]);
            mapPathLineRenderers.Remove(mapPathId);
        }
    }

    private void LoadServerMapPaths(string newLocationId)
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
                    UpdateMapPathLineRenderers(path);                   
                }
            }
            else
            {
                Debug.Log(result);
            }
        });
    }

    internal bool GetDirection(Vector3 sourcePosition, Vector3 targetPosition, out SignArrowDirection direction)
    {
        direction = SignArrowDirection.bottom;

        NavMeshPath path = new();
        if (NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path))
        {
            if (path != null && path.corners.Length > 0)
            {
                //error here, the first corner will not exist in a null array, or with no path
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

    private SignArrowDirection GetDirection(Vector3 direction)
    {
        if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
        {
            if (direction.z > 0)
            {
                if (direction.x > 0)
                {
                    return SignArrowDirection.right;
                }
                else
                {
                    return SignArrowDirection.left;
                }
            }
            else
            {
                return SignArrowDirection.bottom;
            }
        }
        else
        {
            if (direction.x > 0)
            {
                return SignArrowDirection.right;
            }
            else
            {
                return SignArrowDirection.left;
            }
        }
    }

    internal bool GetDirectionDegrees(Vector3 sourcePosition, Vector3 targetPosition, Quaternion sign_rotation, out SignArrowDirection direction)
    {
        direction = SignArrowDirection.bottom;

        NavMeshPath path = new();
        if (NavMesh.CalculatePath(sourcePosition, targetPosition, NavMesh.AllAreas, path))
        {
            if (path != null && path.corners.Length > 0)
            {
                var firstCorner = path.corners[1];
                Vector3 directionToTarget = firstCorner - sourcePosition;
                Vector3 normalizedDirection = directionToTarget.normalized;
                direction = GetDirectionDegrees(normalizedDirection, sign_rotation);

                //Debug_DisplayPath(path.corners);

                return true;
            }
        }
        return false;
    }


    private SignArrowDirection GetDirectionDegrees(Vector3 direction, Quaternion sign_rotation)
    {
        //I think this works. It's flipped the x and z values axis so it's as if we were looking at it from below, but since the arctan measures from the z axis to the x axis it still sweeps in the same direction and we don't need to worry about offsetting the angle by 90 degrees if we used the z up and x right axis. Still not totally sure, but I think it checks out
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        //this might be a addition or subtraction, not sure, we're trying zero out the sign rotation so we can get the angle in the world space, so think it's subtraction
        angle -= sign_rotation.eulerAngles.y;

        float right_start = 1f;
        float right_end = 120f;
        float bottom_start = 120f;
        float bottom_end = 240f;
        float left_start = 240f;
        float left_end = 359f;

        if (angle >= right_start && angle < right_end)
        {
            return SignArrowDirection.right;
        }
        else if (angle >= bottom_start && angle < bottom_end)
        {
            return SignArrowDirection.bottom;
        }
        else if (angle >= left_start || angle < left_end)
        {
            return SignArrowDirection.left;
        }
        else
        {
            return SignArrowDirection.top;
        }
    }

    /*
     * Get the latest navigation path. These are directions specifically intended to guide the local user,
     * so it can be displayed on maps, as a world space trail, etc.
     * 
     * The return value may be null if no path is set.
     */
    public EasyVizAR.MapPath GetMyNavigationPath()
    {
        return easyViz_map_path;
    }
}
