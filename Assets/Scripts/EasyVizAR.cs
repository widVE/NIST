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
	public class NavigationTarget
	{
		public string type;			// One of (none|point|feature|headset)
		public string target_id;    // May be set to headset_id or feature_id depending on type
		public Position position;
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
		public NavigationTarget navigation_target;
		public Orientation orientation;
		public Position position;
		public int updated;
	};

    [System.Serializable]
    public class HeadsetList
    {
        public Headset[] headsets;
    }

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
		public NavigationTarget navigation_target;
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
		public float cutting_height;
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
    public class MapLayerInfoList
	{
        public MapInfo[] layers;
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

	[System.Serializable]
	public class NewCheckIn
	{
		public string location_id;
	}

	[System.Serializable]
	public class HeadsetConfiguration
    {
		public bool enable_mesh_capture;
		public bool enable_photo_capture;
        public bool enable_extended_capture;
    }

	[System.Serializable]
	public class Location
    {
		public string id;
		public string name;
		public string description;
		public string model_path;
		public string model_url;
		public HeadsetConfiguration headset_configuration;
    }

	[System.Serializable]
	public class PhotoFileInfo
	{
		public string content_type;
		public int height;
		public string name;
		public string purpose;
		public int width;
	}

	[System.Serializable]
	public class AnnotationBox
	{
		public float height;
		public float left;
		public float top;
		public float width;
	}

	[System.Serializable]
	public class PhotoFileAnnotation
	{
		public AnnotationBox boundary;
		public float confidence;
		public int id;
		public string identified_user_id;
		public string label;
		public int photo_record_id;
		public string sublabel;
	}

	[System.Serializable]
	public class PhotoReturn
	{
		public PhotoFileAnnotation[] annotations;
		public string camera_location_id;
		public Orientation camera_orientation;
		public Position camera_position;
		
		public string created;
		public string created_by;
		public int device_pose_id;

		public PhotoFileInfo[] files;

		public int id;
		public string imageUrl;

		public int priority;
		public string queue_name;
		public bool ready;
		public string retention;
		public string status;
		public float updated;
	}

	[System.Serializable]
	public class PhotoListReturn
	{
		public PhotoReturn[] photos;
	}
}

public class EasyVizARServer : SingletonWIDVE<EasyVizARServer>
{
	private string _authority = null;
	private string _baseURL = null;
	private bool _hasRegistration = false;
	private EasyVizAR.Registration _registration;

	public const string JSON_TYPE = "application/json";
	public const string JPEG_TYPE = "image/jpeg";
	public const string PNG_TYPE = "image/png";

    public bool verbose_debug = false;

    bool _isUploadingImage = false;
	
	void Start()
    {
		//BS Commented this out on 11-27 IDK why this is here before we know the location ID
		// might havebeen for RACE CONDITION
		
		//_hasRegistration = TryLoadRegistration(out _registration);

    }

	// Return server base URL without an ending slash, e.g. "http://easyvizar.wing.cs.wisc.edu:5000"
	public string GetBaseURL()
    {
		if (_baseURL.EndsWith('/'))
			return _baseURL.Substring(0, _baseURL.Length - 1);
		else
			return _baseURL;
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

    public void TextureMapID(string url, string contentType, string width, int map_ID, System.Action<Texture,int> callBack)
    {
        StartCoroutine(GetTextureMapID(_baseURL + url, contentType, width, map_ID, callBack));
    }

    public void Put(string url, string contentType, string jsonData, System.Action<string> callBack)
	{
		StartCoroutine(DoRequest("PUT", _baseURL + url, contentType, jsonData, callBack));
	}
	
	public string GetAuthorizationHeader()
	{
		if (_hasRegistration)
		{
			return "Bearer " + _registration.auth_token;

		}
		else
        {
			return "";
        }
	}
	
	public bool PutImage(string contentType, string pathToFile, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo")
	{
		if(!_isUploadingImage)
		{
			_isUploadingImage = true;
			StartCoroutine(UploadImage(contentType, pathToFile, locationID, width, height, callBack, position, orientation, headsetID, imageType));
			return true;
		}
		
		return false;
	}
	
	public bool PutImagePair(string contentType, string pathToFile, string pathToFile2, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo", string imageType2="depth")
	{
		if(!_isUploadingImage)
		{
			_isUploadingImage = true;
			StartCoroutine(UploadImagePair(contentType, pathToFile, pathToFile2, locationID, width, height, callBack, position, orientation, headsetID, imageType, imageType2));
			return true;
		}
		
		return false;
	}
	
	public bool PutImageTriple(string contentType, string pathToFile, string pathToFile2, string pathToFile3, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo", string imageType2="depth", string imageType3="geometry")
	{
		if(!_isUploadingImage)
		{
			_isUploadingImage = true;
			StartCoroutine(UploadImageTriple(contentType, pathToFile, pathToFile2, pathToFile3, locationID, width, height, callBack, position, orientation, headsetID, imageType, imageType2, imageType3));
			return true;
		}
		
		return false;
	}
	
	public bool PutImageQuad(string contentType, string pathToFile, string pathToFile2, string pathToFile3, string pathToFile4, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo", string imageType2="depth", string imageType3="geometry", string imageType4="thermal", int colorWidth=0, int colorHeight=0)
	{
		if(!_isUploadingImage)
		{
			_isUploadingImage = true;
			StartCoroutine(UploadImageQuad(contentType, pathToFile, pathToFile2, pathToFile3, pathToFile4, locationID, width, height, callBack, position, orientation, headsetID, imageType, imageType2, imageType3, imageType4));
			return true;
		}
		
		return false;
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


    IEnumerator GetTextureMapID(string url, string contentType, string width, int map_ID, Action<Texture,int> callBack)
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
            callBack(my_text, map_ID);
        }
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
	
	IEnumerator UploadImagePair(string contentType, string path, string path2, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo", string imageType2="depth")
    {
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = width;
		h.height = height;
		h.contentType = contentType;//"image/png";
		h.camera_location_id = locationID;

		//var headset = headsetManager.LocalHeadset;
		//if (headset != null)
        {
			//var hsObject = headset.GetComponent<EasyVizARHeadset>();
			//if (hsObject != null)
            {
				h.created_by = headsetID;//hsObject._headsetID;
			}

			h.camera_position = new EasyVizAR.Position();
			h.camera_position.x = position.x;//headset.transform.position.x;
			h.camera_position.y = position.y;//headset.transform.position.y;
			h.camera_position.z = position.z;//headset.transform.position.z;

			h.camera_orientation = new EasyVizAR.Orientation();
			h.camera_orientation.x = orientation.x;
			h.camera_orientation.y = orientation.y;
			h.camera_orientation.z = orientation.z;
			h.camera_orientation.w = orientation.w;
		}
		

		UnityWebRequest www = new UnityWebRequest(GetBaseURL() + "/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");

		string ourJson = JsonUtility.ToJson(h);

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(ourJson);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr1.txt"), www.error);
		}
		else
		{
			//Debug.Log("Form upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut1.txt"), "successfully posted photo");
		}

		string resultText = www.downloadHandler.text;
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "path.txt"), path);
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "result.txt"), resultText);
		//Debug.Log(resultText);

		EasyVizAR.Hololens2PhotoPut h2 = JsonUtility.FromJson<EasyVizAR.Hololens2PhotoPut>(resultText);
		//h2.imagePath = h2Location;

		//string photoJson = JsonUtility.ToJson(h2);
		//Debug.Log(photoJson);
		//Debug.Log(h2.imageUrl);
		
		//instead let's add "photo.png" or "depth.png" to the end of the image URL...
		string iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType);
		iUrl = iUrl + ".png";

		UnityWebRequest www2 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(path);//new UploadHandlerRaw(image_as_bytes2);//
		www2.downloadHandler = new DownloadHandlerBuffer();

		yield return www2.SendWebRequest();

		if (www2.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr2.txt"), www2.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut2.txt"), iUrl);
		}

		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType2);
		iUrl = iUrl + ".png";
		
		UnityWebRequest www3 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www3.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www3.uploadHandler = new UploadHandlerFile(path2);//new UploadHandlerRaw(image_as_bytes2);//
		www3.downloadHandler = new DownloadHandlerBuffer();

		yield return www3.SendWebRequest();

		if (www3.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr3.txt"), www3.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}
		
		www.Dispose();
		www2.Dispose();
		www3.Dispose();
		
		_isUploadingImage = false;

		File.Delete(path);
		File.Delete(path2);
	}


	IEnumerator UploadImageTriple(string contentType, string path, string path2, string path3, string locationID, int width, int height, System.Action<string> callBack,
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType = "photo", string imageType2 = "depth", string imageType3 = "geometry")
	{
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = width;
		h.height = height;
		h.contentType = contentType;//"image/png";
		h.camera_location_id = locationID;

		//var headset = headsetManager.LocalHeadset;
		//if (headset != null)
		{
			//var hsObject = headset.GetComponent<EasyVizARHeadset>();
			//if (hsObject != null)
			{
				h.created_by = headsetID;//hsObject._headsetID;
			}

			h.camera_position = new EasyVizAR.Position();
			h.camera_position.x = position.x;//headset.transform.position.x;
			h.camera_position.y = position.y;//headset.transform.position.y;
			h.camera_position.z = position.z;//headset.transform.position.z;

			h.camera_orientation = new EasyVizAR.Orientation();
			h.camera_orientation.x = orientation.x;
			h.camera_orientation.y = orientation.y;
			h.camera_orientation.z = orientation.z;
			h.camera_orientation.w = orientation.w;
		}


		UnityWebRequest www = new UnityWebRequest(GetBaseURL() + "/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");

		string ourJson = JsonUtility.ToJson(h);

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(ourJson);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr1.txt"), www.error);
		}
		else
		{
			//Debug.Log("Form upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut1.txt"), "successfully posted photo");
		}

		string resultText = www.downloadHandler.text;

		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "path.txt"), path);

		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "result.txt"), resultText);
		//Debug.Log(resultText);

		EasyVizAR.Hololens2PhotoPut h2 = JsonUtility.FromJson<EasyVizAR.Hololens2PhotoPut>(resultText);
		//h2.imagePath = h2Location;

		//string photoJson = JsonUtility.ToJson(h2);
		//Debug.Log(photoJson);
		//Debug.Log(h2.imageUrl);

		//instead let's add "photo.png" or "depth.png" to the end of the image URL...
		string iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType);
		iUrl = iUrl + ".png";

		UnityWebRequest www2 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(path);//new UploadHandlerRaw(image_as_bytes2);//
		www2.downloadHandler = new DownloadHandlerBuffer();

		yield return www2.SendWebRequest();

		if (www2.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr2.txt"), www2.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut2.txt"), iUrl);
		}

		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType2);
		iUrl = iUrl + ".png";

		UnityWebRequest www3 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www3.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www3.uploadHandler = new UploadHandlerFile(path2);//new UploadHandlerRaw(image_as_bytes2);//
		www3.downloadHandler = new DownloadHandlerBuffer();

		yield return www3.SendWebRequest();

		if (www3.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr3.txt"), www3.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}

		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType3);
		iUrl = iUrl + ".png";

		UnityWebRequest www4 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www4.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www4.uploadHandler = new UploadHandlerFile(path3);//new UploadHandlerRaw(image_as_bytes2);//
		www4.downloadHandler = new DownloadHandlerBuffer();

		yield return www4.SendWebRequest();

		if (www4.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr4.txt"), www4.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}

		www.Dispose();
		www2.Dispose();
		www3.Dispose();
		www4.Dispose();

		File.Delete(path);
		File.Delete(path2);
		File.Delete(path3);

		_isUploadingImage = false;
	}
	
	
	IEnumerator UploadImageQuad(string contentType, string path, string path2, string path3, string path4, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo", string imageType2="depth", string imageType3="geometry", string imageType4="thermal", int colorWidth=0, int colorHeight=0)
    {
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = width;
		h.height = height;
		h.contentType = contentType;//"image/png";
		h.camera_location_id = locationID;

		//var headset = headsetManager.LocalHeadset;
		//if (headset != null)
        {
			//var hsObject = headset.GetComponent<EasyVizARHeadset>();
			//if (hsObject != null)
            {
				h.created_by = headsetID;//hsObject._headsetID;
			}

			h.camera_position = new EasyVizAR.Position();
			h.camera_position.x = position.x;//headset.transform.position.x;
			h.camera_position.y = position.y;//headset.transform.position.y;
			h.camera_position.z = position.z;//headset.transform.position.z;

			h.camera_orientation = new EasyVizAR.Orientation();
			h.camera_orientation.x = orientation.x;
			h.camera_orientation.y = orientation.y;
			h.camera_orientation.z = orientation.z;
			h.camera_orientation.w = orientation.w;
		}
		

		UnityWebRequest www = new UnityWebRequest(GetBaseURL() + "/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");

		string ourJson = JsonUtility.ToJson(h);

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(ourJson);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr1.txt"), www.error);
		}
		else
		{
			//Debug.Log("Form upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut1.txt"), "successfully posted photo");
		}

		string resultText = www.downloadHandler.text;
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "path.txt"), path);
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "result.txt"), resultText);
		//Debug.Log(resultText);

		EasyVizAR.Hololens2PhotoPut h2 = JsonUtility.FromJson<EasyVizAR.Hololens2PhotoPut>(resultText);
		//h2.imagePath = h2Location;

		//string photoJson = JsonUtility.ToJson(h2);
		//Debug.Log(photoJson);
		//Debug.Log(h2.imageUrl);
		
		//instead let's add "photo.png" or "depth.png" to the end of the image URL...
		string iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType);
		iUrl = iUrl + ".png";

		UnityWebRequest www2 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(path);//new UploadHandlerRaw(image_as_bytes2);//
		www2.downloadHandler = new DownloadHandlerBuffer();

		yield return www2.SendWebRequest();

		if (www2.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr2.txt"), www2.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut2.txt"), iUrl);
		}

		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType2);
		iUrl = iUrl + ".png";
		
		UnityWebRequest www3 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www3.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www3.uploadHandler = new UploadHandlerFile(path2);//new UploadHandlerRaw(image_as_bytes2);//
		www3.downloadHandler = new DownloadHandlerBuffer();

		yield return www3.SendWebRequest();

		if (www3.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr3.txt"), www3.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}
		
		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType3);
		iUrl = iUrl + ".png";
		
		UnityWebRequest www4 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www4.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www4.uploadHandler = new UploadHandlerFile(path3);//new UploadHandlerRaw(image_as_bytes2);//
		www4.downloadHandler = new DownloadHandlerBuffer();

		yield return www4.SendWebRequest();

		if (www4.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr4.txt"), www4.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}
		
		iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType4);
		iUrl = iUrl + ".png";
		
		UnityWebRequest www5 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www5.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www5.uploadHandler = new UploadHandlerFile(path4);//new UploadHandlerRaw(image_as_bytes2);//
		www5.downloadHandler = new DownloadHandlerBuffer();

		yield return www5.SendWebRequest();

		if (www5.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr4.txt"), www4.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut3.txt"), iUrl);
		}
		
		www.Dispose();
		www2.Dispose();
		www3.Dispose();
		www4.Dispose();
		www5.Dispose();

		File.Delete(path);
		File.Delete(path2);
		File.Delete(path3);
		File.Delete(path4);

		_isUploadingImage = false;
	}

	IEnumerator UploadImage(string contentType, string path, string locationID, int width, int height, System.Action<string> callBack, 
				Vector3 position, Quaternion orientation, string headsetID = "", string imageType="photo")
    {
		EasyVizAR.Hololens2PhotoPost h = new EasyVizAR.Hololens2PhotoPost();
		h.width = width;
		h.height = height;
		h.contentType = contentType;//"image/png";
		h.camera_location_id = locationID;

		//var headset = headsetManager.LocalHeadset;
		//if (headset != null)
        {
			//var hsObject = headset.GetComponent<EasyVizARHeadset>();
			//if (hsObject != null)
            {
				h.created_by = headsetID;//hsObject._headsetID;
			}

			h.camera_position = new EasyVizAR.Position();
			h.camera_position.x = position.x;//headset.transform.position.x;
			h.camera_position.y = position.y;//headset.transform.position.y;
			h.camera_position.z = position.z;//headset.transform.position.z;

			h.camera_orientation = new EasyVizAR.Orientation();
			h.camera_orientation.x = orientation.x;
			h.camera_orientation.y = orientation.y;
			h.camera_orientation.z = orientation.z;
			h.camera_orientation.w = orientation.w;
		}
		

		UnityWebRequest www = new UnityWebRequest(GetBaseURL() + "/photos", "POST");
		www.SetRequestHeader("Content-Type", "application/json");

		string ourJson = JsonUtility.ToJson(h);

		byte[] json_as_bytes = new System.Text.UTF8Encoding().GetBytes(ourJson);
		www.uploadHandler = new UploadHandlerRaw(json_as_bytes);
		www.downloadHandler = new DownloadHandlerBuffer();

		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr1.txt"), www.error);
		}
		else
		{
			//Debug.Log("Form upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut1.txt"), "successfully posted photo");
		}

		string resultText = www.downloadHandler.text;
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "path.txt"), path);
		
		System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "result.txt"), resultText);
		//Debug.Log(resultText);

		EasyVizAR.Hololens2PhotoPut h2 = JsonUtility.FromJson<EasyVizAR.Hololens2PhotoPut>(resultText);
		//h2.imagePath = h2Location;

		//string photoJson = JsonUtility.ToJson(h2);
		//Debug.Log(photoJson);
		//Debug.Log(h2.imageUrl);
		
		//instead let's add "photo.png" or "depth.png" to the end of the image URL...
		string iUrl = h2.imageUrl;
		iUrl = iUrl.Replace("image", imageType);
		iUrl = iUrl + ".png";

		UnityWebRequest www2 = new UnityWebRequest(GetBaseURL() + iUrl, "PUT");
		www2.SetRequestHeader("Content-Type", "image/png");

		//byte[] image_as_bytes2 = imageData.GetRawTextureData();//new System.Text.UTF8Encoding().GetBytes(photoJson);
		//for sending an image - above raw data technique didn't work, but sending via uploadhandlerfile below did...
		www2.uploadHandler = new UploadHandlerFile(path);//new UploadHandlerRaw(image_as_bytes2);//
		www2.downloadHandler = new DownloadHandlerBuffer();

		yield return www2.SendWebRequest();

		if (www2.result != UnityWebRequest.Result.Success)
		{
			//Debug.Log(www2.error);
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOutErr2.txt"), www2.error);
		}
		else
		{
			//Debug.Log("Photo upload complete!");
			System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "logOut2.txt"), "successfully sent photo");
		}

		www.Dispose();
		www2.Dispose();
		
		_isUploadingImage = false;
	}

	// Try to load a previous registration (headset ID, auth token) from file.
	public bool TryLoadRegistration(out EasyVizAR.Registration registration)
    {
		// Extract just the host and port from the baseUrl, e.g. "easyvizar.wings.cs.wisc.edu:5000"
		// Colon is not allowed in filenames, so we replace with a hash sign.
		string filename = _authority.Replace(":", "#") + ".json";
		string filePath = System.IO.Path.Combine(Application.persistentDataPath, "registrations", filename);
        if(verbose_debug) Debug.Log("Is there a Registration named: " + filename);

        if(verbose_debug) Debug.Log("Is there a Registration at FP: " + filePath);

        if (!File.Exists(filePath))
        {
            Debug.Log("NO Registration at FP: " + filePath);

            registration = new EasyVizAR.Registration();
			return false;
        }

        Debug.Log("YES Registration at FP: " + filePath);

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

        //Lance Questions??? Should this be here? It seems like the 				EasyVizARServer.Instance.SetBaseURL(base_url); is supposed to updated the registration boolean, but if there's no file locally it will never suceed becuse the QR scanner is only calle once in the headset

        _hasRegistration = true;
	}

}
