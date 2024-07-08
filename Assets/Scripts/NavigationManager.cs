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
}
