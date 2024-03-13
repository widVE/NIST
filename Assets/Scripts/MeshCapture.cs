using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Unity.Collections;
using UnityEngine;

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialAwareness;
using Microsoft.MixedReality.Toolkit.Utilities;

// Simplify type
using SpatialAwarenessHandler = Microsoft.MixedReality.Toolkit.SpatialAwareness.IMixedRealitySpatialAwarenessObservationHandler<Microsoft.MixedReality.Toolkit.SpatialAwareness.SpatialAwarenessMeshObject>;

/*
 * Setup instructions:
 * https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/spatial-awareness/usage-guide?view=mrtkunity-2022-05
 * 
 * Example code:
 * https://github.com/microsoft/MixedRealityCompanionKit/blob/master/RealtimeStreaming/Samples/Unity/Assets/MixedRealityToolkit.Examples/Demos/SpatialAwareness/Scripts/DemoSpatialMeshHandler.cs
 */

public struct MeshConversionResult
{
    public int InternalId;
    public string SurfaceId;
    public string LocationId;

    public string MeshData;
}

public class MeshCapture : MonoBehaviour, SpatialAwarenessHandler
{
    // Attach QRScanner GameObject so we can listen for location change events.
    [SerializeField]
    GameObject _qrScanner;

    // Attach HeadsetManager GameObject so we can listen for headset configuration change events.
    [SerializeField]
    GameObject _headsetManager;

    [SerializeField]
    [Tooltip("Enable debugging messages and initializing to test location.")]
    bool debug = false;

    [SerializeField]
    [Tooltip("Game object that determines the world coordinate system, ie. the one that changes when QR code is scanned.")]
    GameObject coordinateSystemSource;

    private string _locationId = null;

    // We might not need this reference to the mesh observer depending on whether
    // we use the observer to iterate through its meshes or if we go through the
    // mesh change notifications.
    private IMixedRealitySpatialAwarenessMeshObserver meshObserver;

    private Queue<MeshConversionResult> resultQueue = new Queue<MeshConversionResult>();

    // Start is called before the first frame update
    void Start()
    {
        if(debug)
        {
            // Initialize to a testing location
            _locationId = "628b00d4-ac6b-41bb-8e13-1d7d50eceeb9";
        }

        if (_qrScanner)
        {
            var scanner = _qrScanner.GetComponent<QRScanner>();
            scanner.LocationChanged += (o, ev) =>
            {
                _locationId = ev.LocationID;
            };
        }

        if (_headsetManager)
        {
            var manager = _headsetManager.GetComponent<EasyVizARHeadsetManager>();
            manager.HeadsetConfigurationChanged += (o, ev) =>
            {
                gameObject.SetActive(ev.Configuration.enable_mesh_capture);
            };
        }

        // Use CoreServices to quickly get access to the IMixedRealitySpatialAwarenessSystem
        var spatialAwarenessService = CoreServices.SpatialAwarenessSystem;

        // Cast to the IMixedRealityDataProviderAccess to get access to the data providers
        var dataProviderAccess = spatialAwarenessService as IMixedRealityDataProviderAccess;

        // Initialize to the first available mesh observer, then search for our preferred observer.
        meshObserver = CoreServices.GetSpatialAwarenessSystemDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        foreach (var provider in dataProviderAccess.GetDataProviders<IMixedRealitySpatialAwarenessMeshObserver>())
        {
#if UNITY_EDITOR
            // This observer can be used in Unity Editor for testing.
            if (provider.Name == "Spatial Object Mesh Observer")
            {
                meshObserver = provider;
                break;
            }
#endif

            // This is the observer we expect to find on the HoloLens 2.
            if (provider.Name == "OpenXR Spatial Mesh Observer")
            {
                meshObserver = provider;
            }
        }

        Debug.Log("Found spatial mesh observer: " + meshObserver.Name);

        StartCoroutine(SenderLoop());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnEnable()
    {
        // Register component to listen for Mesh Observation events, typically done in OnEnable()
        CoreServices.SpatialAwarenessSystem.RegisterHandler<SpatialAwarenessHandler>(this);
    }

    // I added a try catch block because I was getting a null reference error back during this call
    private void OnDisable()
    {
        // Unregister component from Mesh Observation events, typically done in OnDisable()
        try
        {
            CoreServices.SpatialAwarenessSystem.UnregisterHandler<SpatialAwarenessHandler>(this);
        }
        catch (System.Exception)
        {
            throw;
        }    
    }

    public void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        EnqueueMeshUpdate(eventData.SpatialObject);
    }

    public void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        EnqueueMeshUpdate(eventData.SpatialObject);
    }

    public void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {

    }

    IEnumerator SenderLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            MeshConversionResult result;
            while (resultQueue.TryDequeue(out result))
            {
                var path = $"/locations/{result.LocationId}/surfaces/{result.SurfaceId}/surface.ply";
                yield return EasyVizARServer.Instance.DoRequest("PUT", path, "application/ply", result.MeshData, UpdateSurfaceCallback);
            }
        }
    }

    async void EnqueueMeshUpdate(SpatialAwarenessMeshObject meshObject)
    {
        // It is not useful to send meshes if the location is unknown.
        if (_locationId is null)
        {
            return;
        }

        MeshConversionResult result;
        result.InternalId = meshObject.Id;
        result.LocationId = _locationId;

        string pattern = @"\S+ - (\S+)";
        Match match = Regex.Match(meshObject.Filter.name, pattern);
        if (match.Success && match.Groups.Count > 1)
        {
            // Regex match should extract system-provided surface ID from Unity object.
            result.SurfaceId = match.Groups[1].Value;
        }
        else
        {
            // Something went wrong with the regex match if we hit this branch
            // but perhaps not a good reason to discard the surface data.
            result.SurfaceId = "s" + meshObject.Id.ToString();
        }

        // AcquireReadOnlyMeshData is supposed to give us a thread-safe data structure.
        // The using block makes sure it is properly cleaned up when we are done.
        using (var meshDataArray = Mesh.AcquireReadOnlyMeshData(meshObject.Filter.sharedMesh))
        {
            Matrix4x4 transformation;
            if (coordinateSystemSource)
            {
                transformation = coordinateSystemSource.transform.localToWorldMatrix;
            }
            else
            {
                // This path would prevent crashing but is probably not going to work as expected.
                // The meshes will end up being aligned relative to the starting position when the app started,
                // not relative to the QR code.
                transformation = transform.localToWorldMatrix;
            }

            // Goal here is to offload the heavy computation to a worker thread. Is it working?
            result.MeshData = await Task.Run<string>(() => ExportMeshDataAsPly(meshDataArray[0], transformation));
        }

        resultQueue.Enqueue(result);
    }

    public string ExportMeshDataAsPly(Mesh.MeshData mesh, Matrix4x4 transformation)
    {
        int num_vertices = mesh.vertexCount;
        int num_indices = mesh.GetSubMesh(0).indexCount;
        int num_triangles = num_indices / 3;

        var vertices = new NativeArray<Vector3>(num_vertices, Allocator.TempJob);
        var normals = new NativeArray<Vector3>(num_vertices, Allocator.TempJob);
        var indices = new NativeArray<int>(num_indices, Allocator.TempJob);

        mesh.GetVertices(vertices);
        mesh.GetNormals(normals);
        mesh.GetIndices(indices, 0);

        // Rough estimate of the PLY file size based on header block + one row per vertex + one row per triangle.
        int capacity = 1000 + 100 * num_vertices + 10 * num_triangles;

        StringBuilder sb = new StringBuilder(capacity);
        sb.AppendLine("ply");
        sb.AppendLine("format ascii 1.0");
        sb.AppendLine("comment System: unity");
        sb.AppendLine($"comment T1: {transformation[0, 0]} {transformation[0, 1]} {transformation[0, 2]}  {transformation[0, 3]}");
        sb.AppendLine($"comment T2: {transformation[1, 0]} {transformation[1, 1]} {transformation[1, 2]}  {transformation[1, 3]}");
        sb.AppendLine($"comment T3: {transformation[2, 0]} {transformation[2, 1]} {transformation[2, 2]}  {transformation[2, 3]}");
        sb.AppendLine($"comment T4: {transformation[3, 0]} {transformation[3, 1]} {transformation[3, 2]}  {transformation[3, 3]}");
        sb.AppendLine("element vertex " + num_vertices);
        sb.AppendLine("property double x");
        sb.AppendLine("property double y");
        sb.AppendLine("property double z");
        sb.AppendLine("property double nx");
        sb.AppendLine("property double ny");
        sb.AppendLine("property double nz");
        sb.AppendLine("element face " + num_triangles);
        sb.AppendLine("property list uchar int vertex_index");
        sb.AppendLine("end_header");

        for (int i = 0; i < num_vertices; i++)
        {
            var v = transformation.MultiplyPoint(vertices[i]);
            var n = transformation.MultiplyPoint(normals[i]);
            sb.AppendLine($"{v.x} {v.y} {v.z} {n.x} {n.y} {n.z}");
        }
       
        for (int i = 0; i < num_indices; i += 3)
        {
            sb.AppendLine($"3 {indices[i]} {indices[i + 1]} {indices[i + 2]}");
        }

        vertices.Dispose();
        normals.Dispose();
        indices.Dispose();

        return sb.ToString();
    }

    void UpdateSurfaceCallback(string resultData)
    {
        
    }
}
