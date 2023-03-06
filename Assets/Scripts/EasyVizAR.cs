//Ross Tredinnick - EasyVizAR - 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace EasyVizAR
{

	/*[System.Serializable]
	public class EasyVizARTransform
	{
		public Vector3 position;
		public Vector3 orientation;
	}*/
	
	[System.Serializable]
	public class Orientation
	{
		public float w;
		public float x;
		public float y;
		public float z;
	}
	
	[System.Serializable]
	public class Position
	{
		public float x;
		public float y;
		public float z;
	}
	
	[System.Serializable]
	public class Headset
	{
		public string color; 
		public int created;
		public string id;
		public string location_id;
		public string mapId;
		public string name;
		public Orientation orientation;
		public Position position;
		public int updated;
	};

	/*
	 * RegisteredHeadset is only received when creating a new headset.
	 * It contains the authentication token that we need to save for future API calls.
	 */
	[System.Serializable]
	public class RegisteredHeadset
	{
		public string color;
		public int created;
		public string id;
		public string location_id;
		public string mapId;
		public string name;
		public Orientation orientation;
		public Position position;
		public int updated;
		public string token;
	};

	/*
	 * HeadsetPositionUpdate contains a subset of the Headset class attributes
	 * that we send with position updates. Sending this instead of the full
	 * Headset object can avoid clobbering other fields such as color.
	 */
	[System.Serializable]
	public class HeadsetPositionUpdate
    {
		public string location_id;
		public Orientation orientation;
		public Position position;
	}

	[System.Serializable]
	public class Hololens2PhotoPost
	{
		public string contentType;
		public string imagePath;
		public string id;
		
		public int width;
		public int height;

		public string created_by;
		public string camera_location_id;
		public Position camera_position;
		public Orientation camera_orientation;
	}

	[System.Serializable]
	public class Hololens2PhotoPut
	{
		public string contentType;
		public string imagePath;
		public string imageUrl;
		public string id;

		public int width;
		public int height;
	}

	[System.Serializable]
	public class MapInfo
	{
		public string contentType;
		public static float created;
		public int id;
		public string imagePath;
		public string imageUrl;
		public string name; 
		public bool ready;
		public string type;
		public static float updated;
		public int version;
		public ViewBox viewBox;
	}



	[System.Serializable]
	public class ViewBox
	{
		public float height;
		public float left;
		public float top;
		public float width;
	}



	[System.Serializable]
	public class HeadsetList
	{
		public Headset[] headsets;
	}
	
	[System.Serializable]
	public class PoseChange
	{
		public Orientation orientation;
		public Position position;
		public float time;
	}
	
	[System.Serializable]
	public class PoseChanges
	{
		public PoseChange[] poseChanges;
	}

	[System.Serializable]
	public class MapField
	{
		public string id;
		public string name;
	}

	[System.Serializable]
	public class FeatureList
	{
		public Feature[] features;
	}

	[System.Serializable]
	public class Path
	{
		public Position[] points;
	}

	[System.Serializable]
	public class Feature
	{
		public string color;
		public float created;
		public string createdBy;
		public int id;
		public string name;
		//Are we using the predefined Vect3f or using our custom class
		public Position position;
		public FeatureDisplayStyle style;
		public string type;
		public float updated;
	}

    [System.Serializable]
	public class FeatureDisplayStyle
	{
		public float leftOffset;
		public string placement;
		public float radius;
		public float topOffset;
    }

	[System.Serializable]
	public class Registration
    {
		public string headset_id;
		public string auth_token;
    }

}

public class EasyVizARServer : SingletonWIDVE<EasyVizARServer>
{
	private string _authority = "easyvizar.wings.cs.wisc.edu:5000";
	private string _baseURL = "http://easyvizar.wings.cs.wisc.edu:5000/";
	private bool _hasRegistration = false;
	private EasyVizAR.Registration _registration;

	public const string JSON_TYPE = "application/json";
	public const string JPEG_TYPE = "image/jpeg";
	public const string PNG_TYPE = "image/png";

	void Start()
    {
		_hasRegistration = TryLoadRegistration(out _registration);
    }

	// Change the server base URL, which will affect all future API calls, e.g. "http://easyvizar.wing.cs.wisc.edu:5000/".
	public void SetBaseURL(string url)
    {
		if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
		{
			_authority = uri.Authority;
			_baseURL = url;

			_hasRegistration = TryLoadRegistration(out _registration);
		}
	}

	// Check if we have a previously registered headset ID.
	public bool TryGetHeadsetID(out string headset_id)
    {
		if (_hasRegistration)
        {
			headset_id = _registration.headset_id;
			return true;
        }
        else
        {
			headset_id = "invalid";
			return false;
        }
    }
	
	//pass the callBack function in from whatever script is calling this...
	public void Get(string url, string contentType, System.Action<string> callBack)
	{
		StartCoroutine(DoGET(_baseURL+url, contentType, callBack));
	}
	
	public void Post(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		StartCoroutine(DoPOST(_baseURL+url, contentType, jsonData, callBack));
	}

	public void Patch(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		StartCoroutine(DoPATCH(_baseURL+url, contentType, jsonData, callBack));
	}

	public void Delete (string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		StartCoroutine(DoDELETE(_baseURL + url, contentType, jsonData, callBack));
	}


	public void Texture(string url, string contentType, string width, System.Action<Texture> callBack)
	{
		StartCoroutine(GetTexture(_baseURL+url, contentType, width, callBack));
	}
	
	public void Put(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		StartCoroutine(DoRequest("PUT", _baseURL + url, contentType, jsonData, callBack));
	}
	
	public void PutImage(string url, string contentType, string pathToFile, System.Action<string> callBack)
	{
		
	}
	
	IEnumerator DoGET(string url, string contentType, System.Action<string> callBack)
	{
		UnityWebRequest www = UnityWebRequest.Get(url);

		www.SetRequestHeader("Content-Type", contentType);
		if (_hasRegistration)
        {
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
        }

		www.downloadHandler = new DownloadHandlerBuffer();
		
		yield return www.SendWebRequest();

		string result = "";
		if (www.result != UnityWebRequest.Result.Success)
		{
			result = "error";
			//Debug.Log(www.error);
		}
		else
		{
			result = www.downloadHandler.text;
			//Debug.Log("Form upload complete!");
		}
		
		www.Dispose();
		callBack(result);
	}
	
	IEnumerator DoPOST(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		UnityWebRequest www = new UnityWebRequest(url, "POST");

		www.SetRequestHeader("Content-Type", contentType);
		if (_hasRegistration)
		{
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
		}

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(jsonData);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

		string result = "";
        if (www.result != UnityWebRequest.Result.Success)
        {
			result = "error";
            //Debug.Log(www.error);
        }
        else
        {
			result = www.downloadHandler.text;
            //Debug.Log("Form upload complete!");
        }
		
		www.Dispose();
		callBack(result);
	}

	IEnumerator DoPATCH(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		UnityWebRequest www = new UnityWebRequest(url, "PATCH");

		www.SetRequestHeader("Content-Type", contentType);
		if (_hasRegistration)
		{
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
		}

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(jsonData);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

		string result = "";
        if (www.result != UnityWebRequest.Result.Success)
        {
			result = "error";
            //Debug.Log(www.error);
        }
        else
        {
			result = www.downloadHandler.text;
            //Debug.Log("Form upload complete!");
        }
		
		www.Dispose();
		callBack(result);
	}

	IEnumerator DoDELETE(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		UnityWebRequest www = new UnityWebRequest(url, "DELETE");

		www.SetRequestHeader("Content-Type", contentType);
		if (_hasRegistration)
		{
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
		}

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(jsonData);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		string result = "";
		if (www.result != UnityWebRequest.Result.Success)
		{
			result = "error";
			//Debug.Log(www.error);
		}
		else
		{
			result = www.downloadHandler.text;
			//Debug.Log("Form upload complete!");
		}

		www.Dispose();
		callBack(result);
	}

	public IEnumerator DoRequest(string method, string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		if (url.StartsWith("/"))
        {
			url = _baseURL + url.Substring(1);
		}

		UnityWebRequest www = new UnityWebRequest(url, method);

		www.SetRequestHeader("Content-Type", contentType);
		if (_hasRegistration)
		{
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
		}

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(jsonData);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		string result = "";
		if (www.result != UnityWebRequest.Result.Success)
		{
			result = "error";
			//Debug.Log(www.error);
		}
		else
		{
			result = www.downloadHandler.text;
			//Debug.Log("Form upload complete!");
		}

		www.Dispose();
		callBack(result);
	}


	IEnumerator GetTexture(string url, string contentType, string width, System.Action<Texture> callBack)
	{
		UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

		www.SetRequestHeader("Accept", contentType);
		www.SetRequestHeader("Width", width);
		if (_hasRegistration)
		{
			www.SetRequestHeader("Authorization", "Bearer " + _registration.auth_token);
		}

		//Debug.Log(www);
		yield return www.SendWebRequest();
		
		//Debug.Log(www.result);
		
		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(www.error);
		}
		else
		{
			
			//Using the Texture download handler we get the content from the Unity Web Request object
			Texture my_text = DownloadHandlerTexture.GetContent(www);
			callBack(my_text);
			
			//www.Dispose();
			
			//Map_lines is a list of different map display layers. The texture is assigned to the lines layer
			//of the map_layout instances. Those objects can modify the display of color and other properties of the lines
			//this may also be useful for multi layer mapping in the future.
			/*foreach (var map_layout in map_lines) 
			{
				map_layout.GetComponent<Renderer>().material.mainTexture = my_text;
			}*/

			//Code from eariler attempts, review and delete
			//Texture myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
			//Debug.Log(myTexture == null);

			//Material material = new Material(shader);
			//material.mainTexture = myTexture;
			//current_layer_prefab.GetComponent<Renderer>().material.mainTexture = myTexture;
			//current_layer_prefab.GetComponent<Renderer>().material.SetTexture("_BaseMap", my_text);
		}
	}

	// Try to load a previous registration (headset ID, auth token) from file.
	public bool TryLoadRegistration(out EasyVizAR.Registration registration)
    {
		// Extract just the host and port from the baseUrl, e.g. "easyvizar.wings.cs.wisc.edu:5000"
		// Colon is not allowed in filenames, so we replace with a hash sign.
		string filename = _authority.Replace(":", "#") + ".json";
		string filePath = System.IO.Path.Combine(Application.persistentDataPath, "registrations", filename);

		if (!File.Exists(filePath))
        {
			registration = new EasyVizAR.Registration();
			return false;
        }

		var reader = new StreamReader(filePath);
		var data = reader.ReadToEnd();
		reader.Close();

		registration = JsonUtility.FromJson<EasyVizAR.Registration>(data);
		return true;
	}

	// Save a new registration (headset ID, auth token) to file.
	public void SaveRegistration(string headset_id, string auth_token)
    {
		// Extract just the host and port from the baseUrl, e.g. "easyvizar.wings.cs.wisc.edu:5000"
		// Colon is not allowed in filenames, so we replace with a hash sign.
		string filename = _authority.Replace(":", "#") + ".json";
		string parentDir = System.IO.Path.Combine(Application.persistentDataPath, "registrations");
		string filePath = System.IO.Path.Combine(parentDir, filename);

		Directory.CreateDirectory(parentDir);

		var reg = new EasyVizAR.Registration();
		reg.headset_id = headset_id;
		reg.auth_token = auth_token;

		var data = JsonUtility.ToJson(reg);

		var writer = new StreamWriter(filePath);
		writer.Write(data);
		writer.Close();

		_registration = reg;
		_hasRegistration = true;
	}

}