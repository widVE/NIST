using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

public class MeshCapture : MonoBehaviour, SpatialAwarenessHandler
{
    // Attach QRScanner GameObject so we can listen for location change events.
    [SerializeField]
    GameObject _qrScanner;

    [SerializeField]
    bool debug = false;

    private Dictionary<int, SpatialAwarenessMeshObject> updatedMeshData = new Dictionary<int, SpatialAwarenessMeshObject>();
    private int sendingInProgress = 0;

    private string _locationId = "";

    // Start is called before the first frame update
    void Start()
    {
        if (_qrScanner)
        {
            var scanner = _qrScanner.GetComponent<QRScanner>();
            scanner.LocationChanged += (o, ev) =>
            {
                _locationId = ev.LocationID;
            };
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (sendingInProgress <= 0 && _locationId != "")
        {
            // Make a copy of the updated mesh dictionary and use a coroutine to send the batch to the server.
            // The coroutine lets us slow the updates down to a more manageable rate.
            Dictionary<int, SpatialAwarenessMeshObject> copiedItems = new Dictionary<int, SpatialAwarenessMeshObject>(updatedMeshData);
            sendingInProgress = copiedItems.Count;
            updatedMeshData.Clear();

            StartCoroutine(SendAllSurfaceUpdates(copiedItems));
        }
    }

    private void OnEnable()
    {
        // Register component to listen for Mesh Observation events, typically done in OnEnable()
        CoreServices.SpatialAwarenessSystem.RegisterHandler<SpatialAwarenessHandler>(this);
    }

    private void OnDisable()
    {
        // Unregister component from Mesh Observation events, typically done in OnDisable()
        CoreServices.SpatialAwarenessSystem.UnregisterHandler<SpatialAwarenessHandler>(this);
    }

    public virtual void OnObservationAdded(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        EnqueueMeshUpdate(eventData.SpatialObject);
    }

    public virtual void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        EnqueueMeshUpdate(eventData.SpatialObject);
    }

    public virtual void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {

    }

    void EnqueueMeshUpdate(SpatialAwarenessMeshObject meshObject)
    {
        updatedMeshData[meshObject.Id] = meshObject;

        if (debug)
        {
            Debug.Log($"Received mesh {meshObject.Id}, sending in progress {sendingInProgress}, queue size {updatedMeshData.Count}");
        }
    }

    IEnumerator SendAllSurfaceUpdates(Dictionary<int, SpatialAwarenessMeshObject> meshes)
    {
        foreach (var meshObject in meshes.Values)
        {
            yield return SendSurfaceUpdate(meshObject);
            yield return new WaitForSeconds(1.0f);
        }
    }

    IEnumerator SendSurfaceUpdate(SpatialAwarenessMeshObject meshObject)
    {
        var mesh = meshObject.Filter.sharedMesh;

        string surface_id;
        string pattern = @"\S+ - (\S+)";
        Match match = Regex.Match(meshObject.Filter.name, pattern);
        if (match.Success && match.Groups.Count > 1)
        {
            // Regex match should extract system-provided surface ID from Unity object.
            surface_id = match.Groups[1].Value;
        }
        else
        {
            // Something went wrong with the regex match if we hit this branch
            // but perhaps not a good reason to discard the surface data.
            surface_id = "s" + meshObject.Id.ToString();
        }

        // Rough estimate of the PLY file size based on header block + one row per vertex + one row per triangle.
        int capacity = 1000 + 100 * mesh.vertices.Length + 10 * mesh.triangles.Length;

        StringBuilder sb = new StringBuilder(capacity);
        sb.AppendLine("ply");
        sb.AppendLine("format ascii 1.0");
        sb.AppendLine("comment Spatial Object Id: " + meshObject.Id);
        sb.AppendLine("comment Mesh Filter Name: " + meshObject.Filter.name);
        sb.AppendLine("element vertex " + mesh.vertices.Length);
        sb.AppendLine("property double x");
        sb.AppendLine("property double y");
        sb.AppendLine("property double z");
        sb.AppendLine("property double nx");
        sb.AppendLine("property double ny");
        sb.AppendLine("property double nz");
        sb.AppendLine("element face " + mesh.triangles.Length / 3);
        sb.AppendLine("property list uchar int vertex_index");
        sb.AppendLine("end_header");

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            var v = mesh.vertices[i];
            var n = mesh.vertices[i];
            sb.AppendLine($"{v.x} {v.y} {v.z} {n.x} {n.y} {n.z}");
        }

        for (int i = 0; i < mesh.triangles.Length; i+=3)
        {
            sb.AppendLine($"3 {mesh.triangles[i]} {mesh.triangles[i+1]} {mesh.triangles[i+2]}");
        }

        var path = $"/locations/{_locationId}/surfaces/{surface_id}/surface.ply";
        yield return EasyVizARServer.Instance.DoRequest("PUT", path, "application/ply", sb.ToString(), UpdateSurfaceCallback);

        sendingInProgress--;

        if (debug)
        {
            Debug.Log($"Finished sending surface {surface_id}");
        }
    }

    void UpdateSurfaceCallback(string resultData)
    {
        
    }
}
