using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Collections.Specialized;
using System.Diagnostics;

public class DistanceCalculation : MonoBehaviour
{
	public Camera main_camera;
	public GameObject distance_text;
	public GameObject capsule;
	
	public bool is_feet = true;

	public Vector3 old_position;

	//For displaying the headset icon on map 
	public GameObject headset_icon;
	public GameObject map_parent;
	public GameObject EasyVizARHeadsetManager;
	public EasyVizAR.HeadsetList headset_list;
	public EasyVizAR.Headset current_headset;
	public string headset_name; // this is set in the EasyVizARHeadsetManager.cs script \
	public bool is_local = false;
	public string local_headset_id = "";
    
    public bool Debug_Verbose = true;

    public GameObject current_prefab;

    private TextMeshPro distance_text_TMP;

    GameObject headset_map_marker = null;

    //GLOBAL FOR TESTING< SHOULD NOT STAY
    float distance;

    private void Awake()
    {
        main_camera = Camera.main;
    }

    // Start is called before the first frame update
    void Start()
    {
        current_prefab = this.gameObject;
        
        old_position = main_camera.transform.position;

        //mapParent = GameObject.Find("Map_Spawn_Target"); // NOTE: this is returning null when object is inactive
        EasyVizARHeadsetManager = GameObject.Find("EasyVizARHeadsetManager");
		
        //Assign the Headset ID
        if (EasyVizARServer.Instance.TryGetHeadsetID(out string headset_ID))
		{
            if (this.name.Equals(headset_ID))
            {
                is_local = true;
            }

            local_headset_id = headset_ID;
            GameObject local = GameObject.Find("LocalHeadset");
            local.transform.GetChild(0).name = headset_ID;
        }

        distance_text_TMP = current_prefab.transform.Find("Headset_Dist").GetComponent<TextMeshPro>();
        // if gameobject position doesn't work, then i might have to do a get() to get the position of the given headset

        SpawnHeasetIcon();

        StartCoroutine(HeadsetDistanceCalculate());
    }

    public void Initialize(String headset_name, GameObject map_parent)
    {
        this.headset_name = headset_name;

        this.map_parent = map_parent;
    }


    /*
     * Checks to see if the current object has been spawned as an icon on the map
     * If not it's instantiated onto the map
     */
    private void SpawnHeasetIcon()
    {
        if (Debug_Verbose) UnityEngine.Debug.Log("Map Parent" + map_parent);

        Transform headset_map_transform = null;

        if (map_parent != null)
        {
            headset_map_transform = map_parent.transform.Find(current_prefab.name);

            if (Debug_Verbose) UnityEngine.Debug.Log("Dist Calc says map marker is at " + headset_map_transform);

            //If we don't have our headset on the map, we instantiate it, otherwise we get a reference to it
            if (headset_map_transform == null)
            {
                headset_map_marker = Instantiate(headset_icon, map_parent.transform, false); // This is where we instantiate the headset icon on the map --> need to change the reference of the headset_icon.
                if (Debug_Verbose) UnityEngine.Debug.Log("Dist Calc no marker!, but made one");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        HeadsetMapIconViewUpdate();
    }
	// This function does 2 things: 1) calculate the distance 2) display the headset icon on palm map.
	/*
	 * public void CalcHeadsetDist()
	{
		camera_position = main_camera.GetComponent<Transform>().position;

		TextMeshPro display_dist_text = current_prefab.transform.Find("Headset_Dist").GetComponent<TextMeshPro>(); ;
		// if gameobject position doesn't work, then i might have to do a get() to get the position of the given headset
		float x_distance = (float)Math.Pow(capsule.transform.position.x - camera_position.x, 2);
		float z_distance = (float)Math.Pow(capsule.gameObject.transform.position.z - camera_position.z, 2);

		if (is_feet)
		{
			x_distance = (float)(x_distance * 3.281);
			z_distance = (float)(z_distance * 3.281);
		}

		float distance = (float)Math.Round((float)Math.Sqrt(x_distance + z_distance) * 10f) / 10f;

		if (is_feet)
		{
			display_dist_text.text = headset_name + " : " + distance.ToString() + "ft";
		}
		else
		{
			display_dist_text.text = headset_name + " : " + distance.ToString() + "m";
		}
		
		// displaying headset on map here --> below has nothing to do with distance calculation 
		if (map_parent != null)
        {
			GameObject mapMarker = null;

			//If we don't have our headset on the map, we instantiate it, otherwise we get a reference to it
			if (!map_parent.transform.Find(current_prefab.name))
            {
                mapMarker = Instantiate(headset_icon, map_parent.transform, false); // This is where we instantiate the headset icon on the map --> need to change the reference of the headset_icon.
				// TODO: Add your local headset icon here
            }
            else
            {
				mapMarker = map_parent.transform.Find(current_prefab.name).gameObject;
			}

			//If our map marker is found, we manipulate it's position
			if (mapMarker != null)
			{
                UnityEngine.Debug.Log(this.name + " is local?: " + is_local);

                if (is_local)
				{
                    UnityEngine.Debug.Log("get into local: " + this.name);

                    mapMarker.transform.localPosition = new Vector3(camera_position.x, 0, camera_position.z);
					
					// TODO: trying to get the rotation of the local headset --> since local headset's prefab is disabled
                    headset_parent.transform.Find(local_headset_id).position = camera_position;
                    headset_parent.transform.Find(local_headset_id).eulerAngles = main_camera.transform.eulerAngles;
					UnityEngine.Debug.Log("this is the local headset's rotation: " + headset_parent.transform.Find(local_headset_id).eulerAngles);
                    UnityEngine.Debug.Log("this is the real rotation: " + main_camera.transform.eulerAngles);


                }
                else
				{
                    mapMarker.transform.localPosition = new Vector3(capsule.transform.position.x, 0, capsule.transform.position.z);
                }

                mapMarker.name = current_prefab.name;
				mapMarker.transform.Find("Feature_Text").GetComponent<TextMeshPro>().text = distance.ToString() + "ft";
				//cur_prefab.GetComponent<EasyVizARHeadset>()
				//GetHeadsets();
				Color myColor = current_prefab.GetComponent<EasyVizARHeadset>()._color;

				//Find the icon components and set their color accordingly.
				//NOTE: Transform.find is not recursive and only searches children of calling transform
				mapMarker.transform.Find("Icon Visuals").Find("Icon").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);
                mapMarker.transform.Find("Icon Visuals").Find("Arrow").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);

                //TODO: add the rotation/quaterinion here --> z axis is where we would like to apply the rotation to, but I'm still figuring out how to determine the orientation               
                //mapMarker.transform.rotation = Quaternion.Euler(-7, capsule.transform.rotation.x, capsule.transform.rotation.z);
                
				
				//double radians = 200* Math.Atan2(capsule.transform.rotation.y, capsule.transform.rotation.w);
				//double angle = radians * (180 / Math.PI);
				//UnityEngine.Debug.Log("this is the angle: " + radians);
                //mapMarker.transform.Find("Quad").Rotate(new Vector3(0, 0, (float)radians));

            }
            else
			{
				UnityEngine.Debug.Log("Missing headset Map Marker");
			}
		}
		
	}
*/
    public void CalculateHeadsetDistance()
    {        
        double distance = Vector3.Distance(main_camera.transform.position, capsule.gameObject.transform.position);

        if (is_feet)
        {
            distance *= 3.281;
            distance_text_TMP.text = headset_name + " : " + distance.ToString("0.00") + "ft";
        }
        else
        {
            distance_text_TMP.text = headset_name + " : " + distance.ToString("0.00") + "m";
        } 
    }
    
    //Old implmentation
    public void CalculateHeadsetDistanceHand()
    {
        Vector3 camera_position = main_camera.GetComponent<Transform>().position;

        distance_text_TMP = current_prefab.transform.Find("Headset_Dist").GetComponent<TextMeshPro>(); ;
        // if gameobject position doesn't work, then i might have to do a get() to get the position of the given headset
        float x_distance = (float)Math.Pow(capsule.transform.position.x - camera_position.x, 2);
        float z_distance = (float)Math.Pow(capsule.gameObject.transform.position.z - camera_position.z, 2);

        if (is_feet)
        {
            x_distance = (float)(x_distance * 3.281);
            z_distance = (float)(z_distance * 3.281);
        }

        float distance = (float)Math.Round((float)Math.Sqrt(x_distance + z_distance));

        if (is_feet)
        {
            distance_text_TMP.text = headset_name + " : " + distance.ToString() + "ft";
        }
        else
        {
            distance_text_TMP.text = headset_name + " : " + distance.ToString() + "m";
        }
    }


    /*
        This method will move the local user's headset icon on the map based on the main camera position,
        which will have the same position and rotation as the current user's head.

     */
    public void HeadsetMapIconViewUpdate()
    {
        //If our map marker is found, we manipulate it's position
        if (headset_map_marker != null)
        {
            if (is_local)
            {
                Vector3 camera_position = main_camera.transform.position;

                if (Debug_Verbose) UnityEngine.Debug.Log("Dist Calc get into local: " + this.name);

                headset_map_marker.transform.localPosition = new Vector3(camera_position.x, 0, camera_position.z);

                // TODO: trying to get the rotation of the local headset --> since local headset's prefab is disabled
                //EasyVizARHeadsetManager.transform.Find(local_headset_id).position = camera_position;
                //EasyVizARHeadsetManager.transform.Find(local_headset_id).eulerAngles = main_camera.transform.eulerAngles;

                if (Debug_Verbose) UnityEngine.Debug.Log("this is the local headset's rotation: " + EasyVizARHeadsetManager.transform.Find(local_headset_id).eulerAngles);
                if (Debug_Verbose) UnityEngine.Debug.Log("this is the real rotation: " + main_camera.transform.eulerAngles);
            }
            else
            {
                headset_map_marker.transform.localPosition = new Vector3(capsule.transform.position.x, 0, capsule.transform.position.z);
            }

            headset_map_marker.name = current_prefab.name;
            //This should be okay to remove because it caluclulates the distance from the local headset to the origin, which isn't that useful in this context
            //headset_map_marker.transform.Find("Feature_Text").GetComponent<TextMeshPro>().text = distance.ToString() + "ft";
            
            //cur_prefab.GetComponent<EasyVizARHeadset>()
            //GetHeadsets();
            Color myColor = current_prefab.GetComponent<EasyVizARHeadset>()._color;

            //Find the icon components and set their color accordingly.
            //NOTE: Transform.find is not recursive and only searches children of calling transform
            headset_map_marker.transform.Find("Icon Visuals").Find("Icon").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);
            //headset_map_marker.transform.Find("Icon Visuals").Find("Arrow").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);

            //TODO: add the rotation/quaterinion here --> z axis is where we would like to apply the rotation to, but I'm still figuring out how to determine the orientation               
            //mapMarker.transform.rotation = Quaternion.Euler(-7, capsule.transform.rotation.x, capsule.transform.rotation.z);

            //double radians = 200* Math.Atan2(capsule.transform.rotation.y, capsule.transform.rotation.w);
            //double angle = radians * (180 / Math.PI);
            //UnityEngine.Debug.Log("this is the angle: " + radians);
            //mapMarker.transform.Find("Quad").Rotate(new Vector3(0, 0, (float)radians));

        }
        else
        {
            UnityEngine.Debug.Log("Missing headset Map Marker");
        }

    }

    IEnumerator HeadsetDistanceCalculate()
	{
		while (true)
		{
			Vector3 new_position = main_camera.transform.position;

            double change_distance = Vector3.Distance(new_position, old_position);

            if (change_distance > 0.1)
			{
                CalculateHeadsetDistance();
				old_position = new_position;
			}

			yield return new WaitForSeconds(1f);
		}
	}

}
