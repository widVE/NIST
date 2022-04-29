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
	public class Headset
	{
		public int created;
		public string id;
		public string locationId;
		public string name;
		public Vector3 position;
		public Vector4 orientation;
		public int updated;
		//using below nests the transform, but the server doesn't expect that...
		//public EasyVizARTransform transform;
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
}

public class EasyVizARServer : SingletonWIDVE<EasyVizARServer>
{
	const string _baseURL = "http://halo05.wings.cs.wisc.edu:5000/";
	
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
}