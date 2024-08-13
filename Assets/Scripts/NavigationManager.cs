using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavigationManager : MonoBehaviour
{
    // One of these can be set in the editor to load a mesh at startup.
    // They can also be altered during runtime by calling UpdateNavMesh.
    public Mesh mesh;
    public GameObject meshGameObject;

    // A target can be set in the editor or by calling SetTarget.
    // After a target has been set, if a path can be found, it will be displayed
    // using the attached LineRenderer.
    public bool targetIsSet = false;
    public Vector3 targetPosition;

    private NavMeshData navMeshData;

    private NavMeshSurface myNavMeshSurface;
    private LineRenderer myLineRender;

    private bool foundPath = false;
    private NavMeshPath path;

    // temp board entrance. pop up the board in a pointer click event
    public PointerReceiver receiver;

    private SignManager mSignNavigation;

    public GameObject signManagerPrefab;

    public FeatureManager featureManager;

    List<List<Vector3>> _pointCache = new();

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
    AsyncOperation UpdateNavMesh(Mesh mesh)
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
    AsyncOperation UpdateNavMesh(GameObject meshGameObject)
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
        Init();

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

    ////////////////////////////////////////////////////////////////////////////////////
    public void Init()
    {
        receiver.pointerClickedHanler += OnPointerClicked;
    }

    private void OnPointerClicked(
        Vector3 pointerPosition,
        Vector3 targetPosition,
        Quaternion targetRotation
        )
    {
        if (mSignNavigation != null)
        {
            Destroy(mSignNavigation.gameObject);
        }
        mSignNavigation =
                Instantiate(signManagerPrefab).GetComponent<SignManager>();

        mSignNavigation.SetLocation(featureManager.location.name);
        // show navigation sign
        mSignNavigation.UpdatePositionAndRotation(targetPosition, targetRotation);

        UpdateNavigationSigns();
    }

    private void UpdateNavigationSigns()
    {
        if (featureManager == null)
        {
            Debug.LogError("SignNavigationManager:FeatureManager is null");
            return;
        }
        // user current position
        mSignNavigation.CleanList();
        _pointCache.Clear();


        var sourcePosition = Camera.main.transform.position;
        var features = featureManager.feature_list.features;
        for (int i = 0; i < features.Length; i++)
        {
            var feature = features[i];
            var targetPosition =
                new Vector3(feature.position.x, feature.position.y, feature.position.z);

            if (GetDirection(sourcePosition, targetPosition, out Dir direction))
            {
                mSignNavigation.AddFeature(direction, feature, featureManager.GetTypeIcon(feature.type));
                print(direction + ":" + feature.name);
            }
        }
        mSignNavigation.RefreshView();
    }

    private bool GetDirection(Vector3 sourcePosition, Vector3 targetPosition, out Dir direction)
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
