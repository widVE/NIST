using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EasyVizarHeadset : MonoBehaviour
{
	[SerializeField]
	float _updateFrequency;
	float UpdateFrequency => _updateFrequency;
	
	[SerializeField]
	string _headsetID;
	
	float _lastTime;
	
    // Start is called before the first frame update
    void Start()
    {
		_lastTime = UnityEngine.Time.time;
		
		RegisterHeadset();
    }

    // Update is called once per frame
    void Update()
    {
		float t = UnityEngine.Time.time;
        if(t - _lastTime > _updateFrequency)
		{
			
			
			_lastTime = t;
		}
    }
	
	void RegisterCallback(string resultData)
	{
		if(resultData != "error")
		{
			EasyVizAR.Headset h = JsonUtility.FromJson<EasyVizAR.Headset>(resultData);
			//fill in any local data here from the server...
			transform.position = h.position;
			transform.rotation = new Quaternion(h.orientation.x, h.orientation.y, h.orientation.z, h.orientation.w);
		}
	}
	
	void RegisterHeadset()
	{
		//register the headset with the server, first checking if it exists there already or not...
		EasyVizarServer.Instance.Get("headsets/"+_headsetID, EasyVizarServer.JSON_TYPE, RegisterCallback);
	}
}
