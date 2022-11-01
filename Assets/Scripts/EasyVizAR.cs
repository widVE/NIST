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
		// 
		public int created;
		public string id;
		public string location_id;
		public string mapId;
		public string name;
		public Orientation orientation;
		public Position position;
		public int updated;
	};

	[System.Serializable]
	public class Hololens2PhotoPost
	{
		public string contentType;
		public string imagePath;
		public string id;
		
		public int width;
		public int height;
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



}

public class EasyVizARServer : SingletonWIDVE<EasyVizARServer>
{
	public string _baseURL = "http://halo05.wings.cs.wisc.edu:5000/";
	
	public const string JSON_TYPE = "application/json";
	public const string JPEG_TYPE = "image/jpeg";
	public const string PNG_TYPE = "image/png";
	
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
		
	}
	
	public void PutImage(string url, string contentType, string pathToFile, System.Action<string> callBack)
	{
		
	}
	
	IEnumerator DoGET(string url, string contentType, System.Action<string> callBack)
	{
		UnityWebRequest www = UnityWebRequest.Get(url);
		www.SetRequestHeader("Content-Type", contentType);
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

}