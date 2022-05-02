using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyVizARHeadset : MonoBehaviour
{
	[SerializeField]
	float _updateFrequency;
	
	[SerializeField]
	string _headsetName;
	public string Name => _headsetName;
	
	[SerializeField]
	bool _isLocal = true;
	public bool IsLocal => _isLocal;
	
	string _headsetID;
	
	float _lastTime;
	
	bool _isRegisteredWithServer = false;
	
	//if local, we set this to the MainCamera (the Hololens 2's camera)
	Camera _mainCamera;
	
    // Start is called before the first frame update
    void Start()
    {
		_lastTime = UnityEngine.Time.time;
		Debug.Log("In start");
		//CreateHeadset();
		//RegisterHeadset();
    }

	public void CreateLocalHeadset(string headsetName)
	{
		_isLocal = true;
		_mainCamera = Camera.main;
		_headsetName = headsetName;
		CreateHeadset();
	}
	
    // Update is called once per frame
    void Update()
    {
		if(_isLocal)
		{
			if(_mainCamera)
			{
				transform.position = _mainCamera.transform.position;
				transform.rotation = _mainCamera.transform.rotation;
			}
			
			float t = UnityEngine.Time.time;
			if(t - _lastTime > _updateFrequency)
			{
				if(_isRegisteredWithServer)
				{
					//PostPosition();
				}
				_lastTime = t;
			}
		}
    }
	
	public void AssignValuesFromJson(EasyVizAR.Headset h)
	{
		transform.position = h.position;
		transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);
		_headsetID = h.id;
		_headsetName = h.name;
	}
	
	void RegisterHeadset()
	{
		//register the headset with the server, first checking if it exists there already or not...
		EasyVizARServer.Instance.Get("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, RegisterCallback);
	}
	
	void RegisterCallback(string resultData)
	{
		if(resultData != "error")
		{
			//Debug.Log(resultData);
			EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);
			//fill in any local data here from the server...
			AssignValuesFromJson(h);
		}
		else
		{
			//headset doesn't yet exist, let's create a new one...
			CreateHeadset();
		}
	}
	
	void CreateHeadset()
	{
		EasyVizAR.Headset h = new EasyVizAR.Headset();
		h.position = transform.position;
		h.orientation = new Vector4(transform.rotation[0], transform.rotation[1], transform.rotation[2], transform.rotation[3]);
		h.name = _headsetName;
		
		EasyVizARServer.Instance.Post("headsets", EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), CreateCallback);
	}
	
	void CreateCallback(string resultData)
	{
		if(resultData != "error")
		{
			_isRegisteredWithServer = true;
			Debug.Log("Successfully connected headset");
			EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);
			transform.position = h.position;
			transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);
			_headsetID = h.id;
			_headsetName = h.name;
		}
		else
		{
			Debug.Log("Received an error when creating headset");
		}
	}
	
	void PostPosition()
	{
		EasyVizAR.Headset h = new EasyVizAR.Headset();
		h.position = transform.position;
		h.orientation = new Vector4(transform.rotation[0], transform.rotation[1], transform.rotation[2], transform.rotation[3]);
		h.name = _headsetName;
		
		EasyVizARServer.Instance.Patch("headsets/"+_headsetID, EasyVizARServer.JSON_TYPE, JsonUtility.ToJson(h), PostPositionCallback);
	}
	
	
	void PostPositionCallback(string resultData)
	{
		//Debug.Log(resultData);
		
		if(resultData != "error")
		{
			
		}
		else
		{
			
		}
	}
}
