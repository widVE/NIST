using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using VInspector;

public class NavigationPathVisualizerManager : MonoBehaviour
{
    FeatureManager feature_manager;

    //Dictionary that will store the navigation paths for each feature by its id as the key value
    //The dictionaries use behavior expects unique keys, so any matching key will overwrite the value
    public SerializedDictionary<int, EasyVizAR.MapPath> navigation_paths = new SerializedDictionary<int, EasyVizAR.MapPath>();

    //Prefab that will be used to visualize the path using the data from the sign_navigation_paths dictionary and a line renderer component
    public GameObject map_path_visualizer;

    public SerializedDictionary<int, GameObject> path_visualizers = new SerializedDictionary<int, GameObject>();

    //Root transform that will be used as the starting point for the path calculation, if not set, it will use the current transform
    public Transform navigation_root;

    // Start is called before the first frame update
    void Start()
    {
        feature_manager = FindObjectOfType<FeatureManager>();
        if (feature_manager == null)
        {
            Debug.LogError("FeatureManager not found");
        }
        if (navigation_root == null)
        {
            navigation_root = this.transform;
        }
    }

    [Button("Compute Path From Features")]
    public void ComputePathFromFeatures()
    {
        //Iterate over all the features in the featureManager.feature_dictionary
        foreach (EasyVizAR.Feature feature in feature_manager.feature_dictionary.Values)
        {
            //get the id of the feature
            int feature_id = feature.id;

            //compute the NavMeshPath from the manager location to the feature location
            NavMeshPath path_to_feature = new NavMeshPath();
            Vector3 feature_position = new Vector3((float)feature.position.x, (float)feature.position.y, (float)feature.position.z);

            NavMesh.CalculatePath(navigation_root.position, feature_position, NavMesh.AllAreas, path_to_feature);

            //create a new EasyVizAR.NewMapPath object based on the matching data fields from EasyVizAR.Feature
            EasyVizAR.MapPath EasyViz_path = new EasyVizAR.MapPath();            
            
            EasyViz_path.points = path_to_feature.corners;

            EasyViz_path.color = feature.color;

            //not sure if the map path id and target marker id should be the same as the feature id
            EasyViz_path.id = feature_id;
            EasyViz_path.target_marker_id = feature_id;

            //Not sure what the values are supposed to be for these fields
            EasyViz_path.location_id = "ERROR to Implement";
            EasyViz_path.mobile_device_id = "ERROR to Implement";
            EasyViz_path.type = "ERROR to Implement";
            EasyViz_path.label = "ERROR to Implement";

            //store the computed path in the navigation_paths dictionary using the feature id as the key
            navigation_paths[feature_id] = EasyViz_path;
        }
    }

    public void CreatePathVisualizer(int feature_id)
    {
        //get the path from the navigation_paths dictionary using the feature id as the key
        EasyVizAR.MapPath path = navigation_paths[feature_id];

        //create a new instance of the map_path_visualizer prefab
        GameObject path_visualizer = Instantiate(map_path_visualizer, this.transform);

        //change the name of the game object to the feature id
        path_visualizer.name = feature_id.ToString();

        //add the path_visualizer object to the path_visualizers list, or OVERWRITE the current value
        path_visualizers.Add(feature_id, path_visualizer);

        //get the LineRenderer component from the path_visualizer object
        LineRenderer line_renderer = path_visualizer.GetComponent<LineRenderer>();

        //Color path_color;

        if (!ColorUtility.TryParseHtmlString(path.color, out Color path_color))
        {
            Debug.LogError("Invalid color format: " + path.color);
            line_renderer.startColor = Color.magenta;
            line_renderer.endColor = Color.magenta;

        }
        else
        {
            //set the color of the line renderer to the color of the path
            line_renderer.startColor = path_color;
            line_renderer.endColor = path_color;
        }

        //set the number of points in the line renderer to the number of points in the path
        line_renderer.positionCount = path.points.Length;

        //set the positions of the line renderer to the points in the path
        for (int i = 0; i < path.points.Length; i++)
        {
            line_renderer.SetPosition(i, path.points[i]);
        }


    }

    //Enable or disable the visibility of the LineRenderer path based on the is_visible and path_id parameters
    public void SetPathVisibility(int path_id, bool is_visible)
    {
        // Check if the path with the matching ID exists in the path_visualizers dictionary, if it doesn't exist, create it
        if (!path_visualizers.ContainsKey(path_id))
        {
            // Create a new path visualizer if it doesn't exist
            CreatePathVisualizer(path_id);
        }

        // Get the LineRenderer component from the existing path's visualizer object
        LineRenderer line_renderer = path_visualizers[path_id].GetComponent<LineRenderer>();

        // Set the visibility of the line renderer based on is_visible
        line_renderer.enabled = is_visible;
    }

    [Button("Visualize all Paths")]
    public void VisualizeAllPaths()
    {
        bool is_visible = true;

        //iterate over all the paths in the sign_navigation_paths dictionary
        foreach (EasyVizAR.MapPath path in navigation_paths.Values)
        {
            //visualize the path using the VisualizePath method
            SetPathVisibility(path.id, is_visible);
        }
    }

    [Button("Hide all Paths")]
    public void HideAllPaths()
    {
        bool is_visible = false;

        //iterate over all the paths in the sign_navigation_paths dictionary
        foreach (EasyVizAR.MapPath path in navigation_paths.Values)
        {
            //visualize the path using the VisualizePath method
            SetPathVisibility(path.id, is_visible);
        }
    }

    [Button("Clear all Paths")]
    public void ClearAllPaths()
    {
        //iterate over all the path_visualizers in the path_visualizers list
        foreach (GameObject path_visualizer in path_visualizers.Values)
        {
            //destroy the path_visualizer object
            Destroy(path_visualizer);
        }

        //clear the path_visualizers list
        path_visualizers.Clear();
    }
}
