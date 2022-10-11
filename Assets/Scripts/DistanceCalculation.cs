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
    // Start is called before the first frame update
    void Start()
    {

		cam = GameObject.Find("Main Camera");
		//capsule = cur_prefeb.transform.Find("Capsule");

		isFeet = true;
	}

	// Update is called once per frame
	void Update()
    {
		cam_pos = cam.GetComponent<Transform>().position;

		CalcHeadsetDist();
    }

	public void CalcHeadsetDist()
	{



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
		//string name 
		if (isFeet)
		{
			display_dist_text.text = distance.ToString() + "ft";
			//display_dist_text.text = distance.ToString() + "ft";

		}
		else
		{
			display_dist_text.text = distance.ToString() + "m";
		}
	}

}
