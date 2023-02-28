using System.Collections;
using System.Collections.Generic;
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
    // Name of the mesh observer from MRTK that we want to use.
    public string meshObserverName = "Spatial Object Mesh Observer";

    private IMixedRealitySpatialAwarenessMeshObserver observer;

    // Start is called before the first frame update
    void Start()
    {
        /*
        // Use CoreServices to quickly get access to the IMixedRealitySpatialAwarenessSystem
        var spatialAwarenessService = CoreServices.SpatialAwarenessSystem;

        // Cast to the IMixedRealityDataProviderAccess to get access to the data providers
        var dataProviderAccess = spatialAwarenessService as IMixedRealityDataProviderAccess;

        var meshObserver = dataProviderAccess.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>();

        // Get the SpatialObjectMeshObserver specifically
        observer = dataProviderAccess.GetDataProvider<IMixedRealitySpatialAwarenessMeshObserver>(meshObserverName);
        */
    }

    // Update is called once per frame
    void Update()
    {
        /*
        if (!observer.IsRunning)
        {
            Debug.Log("Observer is not running");
            return;
        }
        */

        //foreach (var meshObject in observer.Meshes)
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
        SendSurfaceUpdate(eventData);
    }

    public virtual void OnObservationUpdated(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        SendSurfaceUpdate(eventData);
    }

    public virtual void OnObservationRemoved(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        // Do stuff
    }

    void SendSurfaceUpdate(MixedRealitySpatialAwarenessEventData<SpatialAwarenessMeshObject> eventData)
    {
        var mesh = eventData.SpatialObject.Filter.sharedMesh;

        string body = "ply\n" +
            "format ascii 1.0\n" +
            $"comment Event ID: {eventData.Id}\n" +
            $"comment Event Source: {eventData.EventSource.SourceName}\n" +
            $"comment Event Time: {eventData.EventTime.ToString()}\n" +
            $"comment Spatial Object Id: {eventData.SpatialObject.Id}\n" +
            $"comment Mesh Filter Name: {eventData.SpatialObject.Filter.name}\n" +
            $"element vertex {mesh.vertices.Length}\n" +
            "property double x\n" +
            "property double y\n" +
            "property double z\n" +
            "property double nx\n" +
            "property double ny\n" +
            "property double nz\n" +
            $"element face {mesh.triangles.Length / 3}\n" +
            "property list uchar int vertex_index\n" +
            "end_header";

        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            var v = mesh.vertices[i];
            var n = mesh.vertices[i];
            body += $"\n{v.x} {v.y} {v.z} {n.x} {n.y} {n.z}";
        }

        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            if (i % 3 == 0)
            {
                body += $"\n3 {mesh.triangles[i]}";
            }
            else
            {
                body += $" {mesh.triangles[i]}";
            }
        }

        var path = $"locations/628b00d4-ac6b-41bb-8e13-1d7d50eceeb9/surfaces/{System.Convert.ToUInt32(eventData.Id)}/surface.ply";
        EasyVizARServer.Instance.Put(path, "application/ply", body, UpdateSurfaceCallback);
    }

    void UpdateSurfaceCallback(string resultData)
    {
        
    }
}
