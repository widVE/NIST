using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

public class DistanceCalculation : MonoBehaviour
{
	public GameObject cam;
	public Vector3 cam_pos;
	public GameObject distText;
	public GameObject capsule;
	public GameObject cur_prefab;
	public bool isFeet;

	public Vector3 oldPos;
	public Vector3 newPos;

	//For displaying the headset icon on map 
	public GameObject headset_icon;
	public GameObject mapParent;
	public GameObject headset_parent;
	public EasyVizAR.HeadsetList headset_list;
	public EasyVizAR.Headset cur_headset;
	public string headset_name; // this is set in the EasyVizARHeadsetManager.cs script 

	// Start is called before the first frame update
	void Start()
    {
		cam = GameObject.Find("Main Camera");
		//mapParent = GameObject.Find("Map_Spawn_Target"); // NOTE: this is returning null when object is inactive
		headset_parent = GameObject.Find("EasyVizARHeadsetManager");
		isFeet = true;
		
		cam_pos = cam.GetComponent<Transform>().position;
		oldPos = cam_pos;
		CalcHeadsetDist();
		// get all the headset
		//GetHeadsets();
		StartCoroutine(HeadsetDistanceCalculate());

	}

	// Update is called once per frame
	void Update()
    {

    }
	// This function does 2 things: 1) calculate the distance 2) display the headset icon on palm map.
	public void CalcHeadsetDist()
	{
		cam_pos = cam.GetComponent<Transform>().position;

		TextMeshPro display_dist_text = cur_prefab.transform.Find("Headset_Dist").GetComponent<TextMeshPro>(); ;
		// if gameobject position doesn't work, then i might have to do a get() to get the position of the given headset
		float x_distance = (float)Math.Pow(capsule.transform.position.x - cam_pos.x, 2);
		float z_distance = (float)Math.Pow(capsule.gameObject.transform.position.z - cam_pos.z, 2);

		if (isFeet)
		{
			x_distance = (float)(x_distance * 3.281);
			z_distance = (float)(z_distance * 3.281);
		}
		float distance = (float)Math.Round((float)Math.Sqrt(x_distance + z_distance) * 10f) / 10f;
		if (isFeet)
		{
			display_dist_text.text = headset_name + " : " + distance.ToString() + "ft";

		}
		else
		{
			display_dist_text.text = headset_name + " : " + distance.ToString() + "m";
		}
		
		// displaying headset on map here 
		if (mapParent != null)
        {
			GameObject mapMarker = null;

			//If we don't have our headset on the map, we instantiate it, otherwise we get a reference to it
			if (!mapParent.transform.Find(cur_prefab.name))
            {
                mapMarker = Instantiate(headset_icon, mapParent.transform, false); // This is where we instantiate the headset icon on the map --> need to change the reference of the headset_icon.

            }
            else
            {
				mapMarker = mapParent.transform.Find(cur_prefab.name).gameObject;
			}

			//If our map marker is found, we manipulate it's position
			if (mapMarker != null)
			{
				mapMarker.transform.localPosition = new Vector3(capsule.transform.position.x, 0, capsule.transform.position.z);
				mapMarker.name = cur_prefab.name;
				mapMarker.transform.Find("Feature_Text").GetComponent<TextMeshPro>().text = distance.ToString() + "ft";
				//cur_prefab.GetComponent<EasyVizARHeadset>()
				//GetHeadsets();
				Color myColor = cur_prefab.GetComponent<EasyVizARHeadset>()._color;
				mapMarker.transform.Find("Quad").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);
				//TODO: add the rotation/quaterinion here --> z axis is where we would like to apply the rotation to, but I'm still figuring out how to determine the orientation
				//mapMarker.transform.rotation = Quaternion.Euler(0, 0, capsule.transform.position.z); 
			}
			else
			{
				Debug.LogWarning("Missing headset Map Marker");

			}
		}
		
	}
	
	
	//old logic for posterity & refernce
		//	// displaying headset on map here 
		//if (mapParent != null)
  //      {

		//	if (mapParent.transform.Find(cur_prefab.name))
  //          {
		//		Destroy(mapParent.transform.Find(cur_prefab.name).gameObject);
		//	}
		//	GameObject mapMarker = Instantiate(headset_icon, mapParent.transform, false);
		//	mapMarker.transform.localPosition = new Vector3(capsule.transform.position.x, 0, capsule.transform.position.z);
		//	mapMarker.name = cur_prefab.name;
		//	mapMarker.transform.Find("Feature_Text").GetComponent<TextMeshPro>().text = distance.ToString() + "ft";
		//	//cur_prefab.GetComponent<EasyVizARHeadset>()
		//	//GetHeadsets();
		//	Color myColor = cur_prefab.GetComponent<EasyVizARHeadset>()._color;
		//	mapMarker.transform.Find("Quad").GetComponent<Renderer>().material.SetColor("_EmissionColor", myColor);
		//}



	

	IEnumerator HeadsetDistanceCalculate()
	{
		while (true)
		{
			cam_pos = cam.GetComponent<Transform>().position;
			newPos = cam.GetComponent<Transform>().position;
			float change_x = (float)Math.Pow((newPos.x - oldPos.x), 2);
			float change_z = (float)Math.Pow((newPos.z - oldPos.z), 2);
			float change_dist = (float)Math.Sqrt(change_x + change_z);
			if (change_dist > 0.05)
			{
				CalcHeadsetDist();
				oldPos = newPos;
			}
			yield return new WaitForSeconds(1f);
		}
	}

}
