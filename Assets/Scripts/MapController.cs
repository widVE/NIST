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

    public bool verbose_debug = false;
    public bool mirror_axis = false;
    public string last_clicked_target = "";

    private GameObject Easy_Viz_Manager;

    private EasyVizAR.MapLayerInfoList map_layer_info_list;
    private Dictionary<int,Texture2D> map_layer_images = new Dictionary<int, Texture2D>();

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
