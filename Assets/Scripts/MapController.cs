using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using EasyVizAR;

public class MapController : MonoBehaviour
{
    public GameObject currHeadset;
    public GameObject iconParent;
    public List <GameObject> map_scale;
    public List<GameObject> map_lines;
    public GameObject mapCollection;
    public GameObject feature_parent;
    public GameObject navigationPathView;

    public bool verbose_debug = false;
    public bool mirror_axis = false;
    public string last_clicked_target = "";

    [Tooltip("Map features further away than the culling distance will be hidden or docked on the edge of the map.")]
    public float featureCullingDistance = 10.0f;

    private GameObject Easy_Viz_Manager;

    private EasyVizAR.MapLayerInfoList map_layer_info_list;
    private Dictionary<int,Texture2D> map_layer_images = new Dictionary<int, Texture2D>();

    private EasyVizAR.MapPath currentNavigationPath = null;

    public Texture2D[] map_layer_images_array = new Texture2D[10];
    public int texture_index = 0;

    [SerializeField] String map_location_ID;
    [SerializeField] int map_resolution = 1200;

    // Start is called before the first frame update, but is not called
    // for disabled game objects until they are enabled.
    void Start()
    {
        Easy_Viz_Manager = EasyVizARHeadsetManager.EasyVizARManager.gameObject;
        //map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
    }

    private void Awake()
    {
        //map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
    }

    // Display the updated map whenever the game object is enabled. Not quite where
    // we want to end up, we want to get a signal from the web server, or a controller
    // to update, but this should refresh the map each time we summon it.
    void OnEnable()
    {
        map_location_ID = currHeadset.GetComponent<EasyVizARHeadsetManager>().LocationID;
        GetMapImage();
        MapAspectRatioAndOrigin();

        // Start this coroutine every time the map is enabled because it automatically stops when the map is hidden.
        // The map path is now updated by the UpdateIconCullingLoop coroutine, so we only need the one coroutine.
        //StartCoroutine(UpdateNavigationPathLoop());
        StartCoroutine(UpdateIconCullingLoop());
    }

    // Update is called once per frame
    void Update()
    {

    }

    [ContextMenu("Map Image")]
    void GetMapImage()
    {
        EasyVizARServer.Instance.Texture("locations/" + map_location_ID + "/layers/1/image", "image/png", map_resolution.ToString(), GetMapImageCallback);
    }

    public void GetMapImage(int map_ID)
    {
        EasyVizARServer.Instance.TextureMapID("locations/" + map_location_ID + "/layers/" + map_ID + "/image", "image/png", map_resolution.ToString(), map_ID, GetMapImageIDCallback);
    }

    //How do I pass the index of the map layer to the callback?
    private void GetMapImageCallback(Texture map_image)
    {
        foreach (var map_layout in map_lines)
        {
            map_layout.GetComponent<Renderer>().material.mainTexture = map_image;
        }
    }

    private void GetMapImageIDCallback(Texture texture, int map_ID)
    {
        //If the map layer image is already in the dictionary, update the texture, or add the new key value pair
        //I'm not sure if casting to Texture2D is actually necessary, but the Unity documentation says to use it to represent textures in scripting
        if (map_layer_images.ContainsKey(map_ID))
        {
            //Updating the texture requires the metadata of the image to scale correctly
            //Texture2D updated_map_image = new Texture2D(texture.width, texture.height);
            Texture2D updated_map_image = (Texture2D)texture;
            map_layer_images[map_ID] = updated_map_image;

            map_layer_images_array[texture_index] = updated_map_image;
            texture_index++;
        }
        else
        {
            //Texture2D new_map_image = new Texture2D(texture.width, texture.height);
            Texture2D new_map_image = (Texture2D)texture;
            map_layer_images.Add(map_ID, new_map_image);

            map_layer_images_array[texture_index] = new_map_image;
            texture_index++;

        }

        if (verbose_debug) Debug.Log("Map ID Callback: " + map_ID);
    }


    //This probably doesn't need to have a web request anymore because we are storing the map info locally now. Though this will need to be updated to account for both single layer and multi-layer maps
    [ContextMenu("Map Aspect Ratio and Icon Origin")]
    public void MapAspectRatioAndOrigin()
    {
        EasyVizARServer.Instance.Get("locations/" + map_location_ID + "/layers/1/", EasyVizARServer.JSON_TYPE, MapAspectRatioAndOriginCallback);
        //Debug.Log("Got into DisplayPNGMap()");
    }

    public void MapAspectRatioAndOriginCallback(string results)
    {

        if (results != "error")
        {
            if (verbose_debug) Debug.Log("Map Callback png SUCCESS: " + results);
            var resultJSON = JsonUtility.FromJson<EasyVizAR.MapInfo>(results);
            float mapTop = resultJSON.viewBox.top;
            float mapLeft = resultJSON.viewBox.left;
            float mapHeight = resultJSON.viewBox.height;
            float mapWidth = resultJSON.viewBox.width;

            //enlarging the map to the scale listed from the server (width and height)
            foreach (GameObject map in map_scale) map.transform.localScale = new Vector3(mapWidth / 10, mapHeight / 10, 1);
            float icon_origin_x = (mapWidth / 2.0f + mapLeft);
            float icon_origin_y = mapHeight / 2.0f + mapTop;
            if (mirror_axis) icon_origin_x *= -1;
            if (mirror_axis) icon_origin_y *= -1;

            //float icon_origin_x = (0 - mapLeft) / mapWidth;
            //loat icon_origin_y = (0 - mapTop) / mapHeight;

            //Debug.Log("origin x and y: " + icon_origin_x + ", " + icon_origin_y);
            float icon_z_offset = -0.12f;

            iconParent.transform.localPosition = new Vector3(icon_origin_x, icon_origin_y, icon_z_offset); // the scale may need to be adjusted
        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }

    private void UpdateNavigationPath()
    {
        if (navigationPathView == null)
            return;

        var renderer = navigationPathView.GetComponent<LineRenderer>();
        if (renderer == null)
            return;

        currentNavigationPath = NavigationManager.Instance.GetMyNavigationPath();
        if (currentNavigationPath == null)
        {
            renderer.positionCount = 0;
        }
        else
        {
            bool[] include = new bool[currentNavigationPath.points.Length];
            int includeCount = 0;
            for (int i = 0; i < include.Length; i++)
            {
                var point = currentNavigationPath.points[i];
                var distance = Vector3.Distance(Camera.main.transform.position, point);
                if (distance < featureCullingDistance)
                {
                    include[i] = true;
                    includeCount++;
                }
            }

            // This step finds places where the path transitions between visible and non-visible,
            // then marks an extra point as visible. The result is that the closest point which
            // should be culled is still rendered, giving the user a visual indication that the
            // the path continues beyond what is currently shown.
            for (int i = 1; i < include.Length; i++)
            {
                if (include[i] && !include[i-1])
                {
                    include[i - 1] = true;
                    includeCount++;
                }
            }
            for (int i = include.Length-2; i > 0; i--)
            {
                if (include[i] && !include[i+1])
                {
                    include[i + 1] = true;
                    includeCount++;
                }
            }

            renderer.positionCount = includeCount;
            if (includeCount > 0)
            {
                float minY = 0.0f;
                int j = 0;

                for (int i = 0; i < include.Length && j < includeCount; i++)
                {
                    if (include[i])
                    {
                        var point = currentNavigationPath.points[i];

                        if (point.y < minY)
                            minY = point.y;

                        renderer.SetPosition(j, point);
                        j++;
                    }
                }

                // Find the minimum Y value among the path points,
                // and set the line renderer height such that the path lies above the map surface.
                navigationPathView.transform.localPosition = new Vector3(0.0f, -minY, 0.0f);
            }
        }
    }

    private IEnumerator UpdateNavigationPathLoop()
    {
        while (true)
        {
            // Wait for a change to the navigation path, then update the line renderer on the map.
            yield return new WaitUntil(() => { return NavigationManager.Instance.GetMyNavigationPath() != currentNavigationPath; });
            UpdateNavigationPath();
        }
    }

    private IEnumerator UpdateIconCullingLoop()
    {
        while (true)
        {
            yield return null;

            UpdateNavigationPath();

            foreach (Transform feature in iconParent.transform)
            {
                yield return null;

                var lineRenderer = feature.GetComponent<LineRenderer>();
                if (lineRenderer)
                {
                    // The map path line renderer is updated by the call to UpdateNavigationPath, so we can skip it here.
                    continue;
                }

                var markerObject = feature.GetComponent<MarkerObject>();
                if (markerObject && markerObject.feature_type != "headset")
                {
                    var distance = Vector3.Distance(Camera.main.transform.position, markerObject.world_position);

                    if (distance > featureCullingDistance)
                    {
                        // Push features outside the culling distance into the margin of the map.
                        var direction = markerObject.world_position - Camera.main.transform.position;
                        var position = 1.2f * featureCullingDistance * direction.normalized + Camera.main.transform.position;
                        feature.localPosition = position;
                    }
                    else
                    {
                        // Otherwise, leave the feature where it should be.
                        feature.localPosition = markerObject.world_position;
                    }

                    continue;
                }

                // Anything that is not a line renderer or feature marker, e.g. other user headsets, here.
                {
                    var distance = Vector3.Distance(Camera.main.transform.position, feature.localPosition);
                    feature.gameObject.SetActive(distance < 10);
                    continue;
                }
            }
        }
    }
    
    //Get list of map layers, and extract the metadata for each layer
    [ContextMenu("Map Layer Loop Test")]
    void ImageLoopTest()
    {
        GetMapLayers();
        StartCoroutine(ImageLoop());
    }

    IEnumerator ImageLoop()
    {
        yield return new WaitForSeconds(4);

        while (true)
        {
            foreach(Texture2D image_only in map_layer_images.Values)
            { 
                foreach (GameObject map_layout in map_lines)
                {
                    map_layout.GetComponent<Renderer>().material.mainTexture = image_only;
                }
                yield return new WaitForSeconds(2);
            }
        }   
    }

    void GetMapLayers()
    {
        //Get list of map layers
        EasyVizARServer.Instance.Get("locations/" + map_location_ID + "/layers/?envelope=layers", EasyVizARServer.JSON_TYPE, GetMapLayersCallback);
    }

    void GetMapLayersCallback(string results)
    {
        if(results != "error")
        {
            //Store the layer info
            map_layer_info_list = JsonUtility.FromJson<EasyVizAR.MapLayerInfoList>(results);

            for (int i = 0; i < map_layer_info_list.layers.Length; i++)
            {
                //Get the metadata for each layer
                if(verbose_debug) Debug.Log("Map Layer Info ID" + map_layer_info_list.layers[i].id);
            }


            GetMapLayerImages();
        }
        else
        {
            Debug.Log("ERROR: " + results);
        }
    }

    //Iterate though list of map layers and request each image for the layer
    void GetMapLayerImages()
    {
        //Get the image for each layer
        //TODO use ID's not indexes



        for (int i = 0; i < map_layer_info_list.layers.Length; i++)
        {
            GetMapImage(map_layer_info_list.layers[i].id);

            //Somehow store the image in a list of images
        }

        //SetMapLayerImages();
    }

    //Assign each image to the appropriate map layer on map visualization and aspect ratio and origin of each map layer
    private void SetMapLayerImages()
    {
        throw new NotImplementedException();
    }
}
