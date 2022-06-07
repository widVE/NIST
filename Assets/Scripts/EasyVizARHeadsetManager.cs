using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.QR;
using Microsoft.MixedReality.Toolkit.UI;

public class EasyVizARHeadsetManager : MonoBehaviour
{
	[SerializeField]
	string _locationId = "none";
	public string LocationID 
	{
		get { return _locationId; }
		set { _locationId = value; }
	}
	
	[SerializeField]
	GameObject _headsetPrefab;	//prefab for loading other headsets...
	
	[SerializeField]
	string _localHeadsetName;
	public string LocalHeadsetName => _localHeadsetName;
	
	List<EasyVizARHeadset> _activeHeadsets = new List<EasyVizARHeadset>();
	
	bool _headsetsCreated = false;
	
	[SerializeField]
	Material _localMaterial;
	
	[SerializeField]
	bool _visualizePreviousLocal = false;
	
	[SerializeField]
	bool _makeUniqueLocalHeadset = false;
	
	[SerializeField]
	List<GameObject> _mapObjects = new List<GameObject>();

	// I had to add these to get the slider to work. I wanted to pass the slider value as an argument
	// to this script, but I couldn't figure out how to reference that value in the event handler. This
	// way I at least have a reference to the sliders and can access their values directly
	public GameObject horizontal_offset_slider;
	public GameObject vertical_offset_slider;

	// Start is called before the first frame update
	void Start()
    {
		//CreateAllHeadsets();
    }

    // Update is called once per frame
    void Update()
    {

    }
	
	void OnEnable()
	{
		
	}
	
	void OnDisable()
	{
		
	}
	
	[ContextMenu("CreateAllHeadsets")]
	public void CreateAllHeadsets()
	{
		if(!_headsetsCreated)
		{
			CreateLocalHeadset();
			CreateHeadsets();
			
			_headsetsCreated = true;
		}
	}
	
	public void DisplayMapCallback(Texture resultTexture)
	{
		//Debug.Log("In map callback");
		foreach (var map_layout in _mapObjects) 
		{
			map_layout.GetComponent<Renderer>().material.mainTexture = resultTexture;
		}
	}
	
	public void DisplayHandMap()
	{
		EasyVizARServer.Instance.Texture("locations/" + _locationId + "/layers/1/image", "image/png", "1200", DisplayMapCallback); 
	}

	//These don't work correctly. The funcitons are being called all the time while the user interacts with the slider
	//This is causing an error due to rounding I think. The funcitons are getting the position of the current trail
	//renderer and keeping one component of it, this is causing drift on the values taht are supposed to stay the same

	public void BreadcrumbsOffsetVertical()
	{
		//Getting the data from the pinch sliders as a value to offset.
		PinchSlider offset_slider_vertical = vertical_offset_slider.GetComponent<PinchSlider>();

		//if (verbose_debug_log) Debug.Log(offset_slider_vertical.SliderValue);
		
		// MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
		// figure out how to change it's range so I'm scaling the value here.
		// it should give a new raw range of -0.5 to 0.5

		float default_slider_middle = 0.5f;

		float slider_value_zero_offset_vertical = offset_slider_vertical.SliderValue - default_slider_middle;

		// ideally I think I want a linear scaling that allows the user to set the upper
		// and lower bounds of the values
		// I'm going to just multiply by 5 because 2.5 meters seems reasonable
		float scale_factor = 5;

		float scaled_slider_value_zero_offset_vertical = slider_value_zero_offset_vertical * scale_factor;

		//For every headset in the active list get it's position and line renderer
		//update the vertical componenet based on the offset relative to the headset position.
		foreach (EasyVizARHeadset headset_user in _activeHeadsets)
        {
			//I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
			//as to how offsets of the parent of the line rendere itself work
			var trail_root = headset_user.gameObject.transform.GetChild(0);
			LineRenderer r = trail_root.GetComponent<LineRenderer>();

			if (r != null)
			{
				//Offset relative to the head position, add the offset from the center
				var head_position = headset_user.GetComponent<Transform>().position;
				var trail_local_position = trail_root.GetComponent<Transform>().localPosition;

				
				//Create the new position vector from the vertical and horizontal scaled offsets
				var scaled_root_position = new Vector3(trail_local_position.x, scaled_slider_value_zero_offset_vertical, 0) + head_position;

				// Set the line renderer's parent to the new scaled offset position.
				// I THINK? check here again if line rendering is not as expected
				trail_root.GetComponent<Transform>().position = scaled_root_position;			
			}
		}
	}

	public void BreadcrumbsOffsetHorizontal()
	{
		//Getting the data from the pinch sliders as a value to offset.
		PinchSlider offset_slider_horizontal = horizontal_offset_slider.GetComponent<PinchSlider>();

		//if (verbose_debug_log) Debug.Log(offset_slider_vertical.SliderValue);

		// MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
		// figure out how to change it's range so I'm scaling the value here.
		// it should give a new raw range of -0.5 to 0.5

		float default_slider_middle = 0.5f;

		float slider_value_zero_offset_horizontal = offset_slider_horizontal.SliderValue - default_slider_middle;

		// ideally I think I want a linear scaling that allows the user to set the upper
		// and lower bounds of the values
		// I'm going to just multiply by 5 because 2.5 meters seems reasonable
		float scale_factor = 5;

		float scaled_slider_value_zero_offset_horizontal = slider_value_zero_offset_horizontal * scale_factor;

		//For every headset in the active list get it's position and line renderer
		//update the vertical componenet based on the offset relative to the headset position.
		foreach (EasyVizARHeadset headset_user in _activeHeadsets)
		{
			//I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
			//as to how offsets of the parent of the line rendere itself work
			var trail_root = headset_user.gameObject.transform.GetChild(0);
			LineRenderer r = trail_root.GetComponent<LineRenderer>();

			if (r != null)
			{
				//Offset relative to the head position, add the offset from the center
				var head_position = headset_user.GetComponent<Transform>().position;
				var trail_local_position = trail_root.GetComponent<Transform>().localPosition;

				// I don't think this part makes sense, I was trying to preserve the y value, but this isn't doing that
				// it's just getting the head position y, not the trail offset y. This is the problem with this method.
				//The other approach does work, but causes drift due to rounding. I just want to modify this one
				// component of the vector and then set the new offset position

				Vector3 scaled_horizontal_position = new Vector3(scaled_slider_value_zero_offset_horizontal, 0, 0) + head_position;
				float scaled_horizontal_position_y = scaled_horizontal_position.y;
				Debug.Log("scaled_horizontal_position_y" + scaled_horizontal_position_y);


				//Trying something else, direct modification of the vector component
				//THIS IS NOT WORKING
				// I don't know why, but the pre and post offset valuesa re the same :(
				Vector3 trail_position = trail_root.GetComponent<Transform>().position;
				Debug.Log("pre offset" + trail_position);

				trail_position[0] =  scaled_horizontal_position_y;
				trail_root.GetComponent<Transform>().position = trail_position;

				Debug.Log("post offset" + trail_position);

				/*

				//Create the new position vector from the vertical and horizontal scaled offsets
				var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, trail_local_position.y, 0) + head_position;

				// Set the line renderer's parent to the new scaled offset position.
				// I THINK? check here again if line rendering is not as expected
				trail_root.GetComponent<Transform>().position = scaled_root_position;
				*/
			}
		}
	}

	/*
     * We want to offset the breacrumbs realtive to the head to let the user adjust the position
     * 
     * 
     */
	public void ChangeBreadcrumbsOffset()
	{
		//Getting the data from the pinch sliders as a value to offset.
		PinchSlider offset_slider_vertical = vertical_offset_slider.GetComponent<PinchSlider>();
		PinchSlider offset_slider_horizontal = horizontal_offset_slider.GetComponent<PinchSlider>();

		// MAGIC NUMBER, this comes from the pinch slider's middle value. I couldn't
		// figure out how to change it's range so I'm scaling the value here.
		// it should give a new raw range of -0.5 to 0.5

		float default_slider_middle = 0.5f;

		float slider_value_zero_offset_vertical = offset_slider_vertical.SliderValue - default_slider_middle;
		float slider_value_zero_offset_horizontal = offset_slider_horizontal.SliderValue - default_slider_middle;

		// ideally I think I want a linear scaling that allows the user to set the upper
		// and lower bounds of the values
		// I'm going to just multiply by 5 because 2.5 meters seems reasonable
		float scale_factor = 5;

		float scaled_slider_value_zero_offset_vertical = slider_value_zero_offset_vertical * scale_factor;
		float scaled_slider_value_zero_offset_horizontal = slider_value_zero_offset_horizontal * scale_factor;

		foreach (EasyVizARHeadset headset_user in _activeHeadsets)
		{
			//I'm not super sure I'm getting the correct component here. The line renderer is a bit confusing
			//as to how offsets of the parent of the line rendere itself work
			var trail_root = headset_user.gameObject.transform.GetChild(0);
			LineRenderer r = trail_root.GetComponent<LineRenderer>();

			if (r != null)
			{
				//Offset relative to the head position, add the offset from the center
				var head_position = headset_user.GetComponent<Transform>().position;
				var trail_local_position = trail_root.GetComponent<Transform>().localPosition;
				//Create the new position vector from the vertical and horizontal scaled offsets

				var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, scaled_slider_value_zero_offset_vertical, 0) + head_position;
				/*

				//Create the new position vector from the vertical and horizontal scaled offsets
				var scaled_root_position = new Vector3(scaled_slider_value_zero_offset_horizontal, trail_local_position.y, 0) + head_position;

				// Set the line renderer's parent to the new scaled offset position.
				// I THINK? check here again if line rendering is not as expected
				
				*/
				// Set the root to the new scaled offset position
				trail_root.GetComponent<Transform>().position = scaled_root_position;
			}
		}				
	}

	void CreateLocalHeadset()
	{
		if(!_visualizePreviousLocal)
		{
			GameObject localHeadset = Instantiate(_headsetPrefab, transform);
			if(localHeadset != null)
			{
				EasyVizARHeadset h = localHeadset.GetComponent<EasyVizARHeadset>();
				if(_localMaterial != null)
				{
					localHeadset.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
				}
				
				if(_makeUniqueLocalHeadset)
				{
					string s = System.DateTime.Now.ToString();
					
					h.CreateLocalHeadset(_localHeadsetName+"_"+s, _locationId, !_visualizePreviousLocal);
				}
				else
				{
					h.CreateLocalHeadset(_localHeadsetName, _locationId, !_visualizePreviousLocal);
				}
			}
		}
	}
	
	void CreateHeadsetsCallback(string resultData)
	{
		Debug.Log(resultData);
		
		if(resultData != "error" && resultData.Length > 2)
		{
			//parse list of headsets for this location and create...
			//the key to parsing the array - the text we add here has to match the name of the variable in the array wrapper class (headsets).
			EasyVizAR.HeadsetList h = JsonUtility.FromJson<EasyVizAR.HeadsetList>("{\"headsets\":" + resultData + "}");
			for(int i = 0; i < h.headsets.Length; ++i)
			{
				if(h.headsets[i].name != _localHeadsetName || _visualizePreviousLocal)
				{
					if(h.headsets[i].name == _localHeadsetName)
					{
						GameObject s = Instantiate(_headsetPrefab, transform);
						EasyVizARHeadset hs = s.GetComponent<EasyVizARHeadset>();
						if(hs != null)
						{
							s.name = h.headsets[i].name;
							if(_localMaterial != null)
							{
								s.transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().material = _localMaterial;
							}
							hs.AssignValuesFromJson(h.headsets[i]);
							hs.IsLocal = true;
							hs.LocationID = h.headsets[i].location_id;
							_activeHeadsets.Add(hs);
						}
					}
					else
					{
						GameObject s = Instantiate(_headsetPrefab, transform);
						EasyVizARHeadset hs = s.GetComponent<EasyVizARHeadset>();
						if(hs != null)
						{
							s.name = h.headsets[i].name;
							hs.AssignValuesFromJson(h.headsets[i]);
							_activeHeadsets.Add(hs);
						}
					}
				}
			}
		}
	}
	
	void CreateHeadsets()
	{
		//list headsets from server for our location, create a prefab of each...
		EasyVizARServer.Instance.Get("headsets?location_id="+_locationId, EasyVizARServer.JSON_TYPE, CreateHeadsetsCallback);
	}
}
