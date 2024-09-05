using System.Collections.Generic;
using UnityEngine;

public class VolumeHeadsetLoader : MonoBehaviour
{
    public GameObject _volumeHeadsetPrefab;
    public GameObject volumetricMapParent;

    List<EasyVizARHeadset> _activeHeadsets;
    public EasyVizARHeadsetManager headsetManager_reference; 
    private Dictionary<string, GameObject> spawnedHeadsets = new Dictionary<string, GameObject>();

    private void Awake()
    {
        headsetManager_reference = GameObject.Find("MixedRealityPlayspace/AR Managment Axis/EasyVizARHeadsetManager").GetComponent<EasyVizARHeadsetManager>();
    }

    void Start()
    {
        // Start the repeating invoke to update headsets every 1 to 2 seconds
        InvokeRepeating(nameof(UpdateHeadsets), 1f, 2f);
    }

    public void SpawnVolumeHeadset(EasyVizARHeadset headset)
    {

        GameObject headset_game_object = Instantiate(_volumeHeadsetPrefab, headset.transform.position, headset.transform.rotation, volumetricMapParent.transform);
        headset_game_object.transform.localPosition = headset.transform.position;


        headset_game_object.name = headset._headsetID;

        MarkerObject marker_object = headset_game_object.GetComponent<MarkerObject>();
        if (marker_object != null)
        {
            marker_object.feature_type = "headset";
            marker_object.feature_name = headset.name;
            marker_object.world_position = headset_game_object.transform.position;
            //headsetManager_reference.MoveAndRotateIcon(headset_game_object, headset_game_object.transform);
            var icon_renderer = headset_game_object.transform.Find("Capsule")?.GetComponent<Renderer>();
            if (icon_renderer is not null)
            {
                Color myColor = headset._color;
                icon_renderer.material.SetColor("_EmissionColor", myColor);
            }
        }

        spawnedHeadsets[headset._headsetID] = headset_game_object;
    }

    public void UpdateHeadsets()
    {
        _activeHeadsets = headsetManager_reference._activeHeadsets;

        foreach (EasyVizARHeadset headset in _activeHeadsets)
        {
            if (spawnedHeadsets.ContainsKey(headset._headsetID))
            {
                // Update position if headset already exists
                GameObject existingHeadset = spawnedHeadsets[headset._headsetID];
                //headsetManager_reference.MoveAndRotateIcon(existingHeadset, existingHeadset.transform);
            }
            else
            {
                // Spawn new headset if it doesn't exist
                SpawnVolumeHeadset(headset);
            }
        }

        // Optionally, remove headsets that are no longer active
        List<string> inactiveHeadsets = new List<string>();
        foreach (string headsetID in spawnedHeadsets.Keys)
        {
            bool isActive = _activeHeadsets.Exists(h => h._headsetID == headsetID);
            if (!isActive)
            {
                inactiveHeadsets.Add(headsetID);
            }
        }

        foreach (string inactiveHeadset in inactiveHeadsets)
        {
            Destroy(spawnedHeadsets[inactiveHeadset]);
            spawnedHeadsets.Remove(inactiveHeadset);
        }
    }
}
