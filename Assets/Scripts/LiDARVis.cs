#define MESH_BASED
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


public class LiDARVis : MonoBehaviour
{
	public string inputDirectory;

	public GameObject ipadDebugPrefab;
	
	[SerializeField]
	QRScanner _qrScanner;
	
	[SerializeField]
	int depthWidth;
	
	[SerializeField]
	int depthHeight;

	Texture2D _colorTex;

	Texture2D _geomTex;

	Texture2D _depthTex;

	Matrix4x4 _transform = Matrix4x4.identity;

	bool _nextReady = false;
	bool _newGeom = false;
	bool _newColor = false;
	bool _newDepth = false;

	Vector3 _currentPosition = Vector3.zero;
	Quaternion _currentRotation = Quaternion.identity;

	GameObject _currentParent = null;

    // Start is called before the first frame update
    void Start()
    {
		_colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
 		_geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
 		_depthTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
		
		_qrScanner.LocationChanged += (o, ev) =>
		{
			Debug.Log("Calling ImageGetTest");
			ImageGetTest();
		};
		
		//ImageGetTest();
			
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void DepthTextureCallback(Texture2D textureData)
	{
		if(_depthTex == null)
		{
			_depthTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
		}

		if(textureData.width == depthWidth && textureData.height == depthHeight) {
			Graphics.CopyTexture(textureData, _depthTex);
		}

		_depthTex.Apply(false);
		_newDepth = true;
	}

	void ColorTextureCallback(Texture2D textureData)
	{
		if(_colorTex == null)
		{
			_colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
		}
	
		if(textureData.width == depthWidth && textureData.height == depthHeight) {
			Graphics.CopyTexture(textureData, _colorTex);
		}

		_colorTex.Apply(false);
		_newColor = true;
	}

	void GeomTextureCallback(Texture2D textureData)
	{
		if(_geomTex == null)
		{
			_geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
		}

		if(textureData.width == depthWidth && textureData.height == depthHeight) {
			Graphics.CopyTexture(textureData, _geomTex);
		}

		_geomTex.Apply(false);
		_newGeom = true;
	}

	IEnumerator LoadPointClouds(string result_data)
	{
		if(result_data.Length > 0)
		{
			Debug.Log(result_data);
			
			EasyVizAR.PhotoListReturn photo_list  = JsonUtility.FromJson<EasyVizAR.PhotoListReturn>("{\"photos\":"+result_data+"}");
			Debug.Log(photo_list.photos.Length);
			int numLoaded = 0;
			int offset = 0;

			while(numLoaded < photo_list.photos.Length)
			{
				int i = offset + numLoaded;

				if(_nextReady)
				{
					//Debug.Log("ID: " + photo_list.photos[i].id);
					//Debug.Log(photo_list.photos[i].imageUrl);
					if(photo_list.photos[i].files != null)
					{
						//Debug.Log("Num photos: " + photo_list.photos[i].files.Length);
						for(int j = 0; j < photo_list.photos[i].files.Length; ++j)
						{
							//Debug.Log(photo_list.photos[i].files[j].name);
							if(photo_list.photos[i].files[j].name == "geometry.bmp")
							{
								_currentPosition.x = photo_list.photos[i].camera_position.x;
								_currentPosition.y = photo_list.photos[i].camera_position.y;
								_currentPosition.z = photo_list.photos[i].camera_position.z;
								
								_currentRotation.x = photo_list.photos[i].camera_orientation.x;
								_currentRotation.y = photo_list.photos[i].camera_orientation.y;
								_currentRotation.z = photo_list.photos[i].camera_orientation.z;
								_currentRotation.w = photo_list.photos[i].camera_orientation.w;

								/*Debug.Log("Index: " + i);
								Debug.Log(photo_list.photos[i].camera_orientation.x);
								Debug.Log(photo_list.photos[i].camera_orientation.y);
								Debug.Log(photo_list.photos[i].camera_orientation.z);
								Debug.Log(photo_list.photos[i].camera_orientation.w);
								Debug.Log(photo_list.photos[i].camera_position.x);
								Debug.Log(photo_list.photos[i].camera_position.y);
								Debug.Log(photo_list.photos[i].camera_position.z);*/ 
								
								if(_currentPosition.magnitude > 0)
								{
									//photo_list.photos[i].files[j].name
									_newGeom = false;
									_newColor = false;
									_newDepth = false;

									EasyVizARServer.Instance.Texture("photos/"+photo_list.photos[i].id+"/photo.png", "image/png", "320", ColorTextureCallback);
									EasyVizARServer.Instance.Texture("photos/"+photo_list.photos[i].id+"/geometry.bmp", "image/bmp", "320", GeomTextureCallback);
									EasyVizARServer.Instance.Texture("photos/"+photo_list.photos[i].id+"/depth.bmp", "image/bmp", "320", DepthTextureCallback);

									_nextReady = false;

									//StartCoroutine(WaitForTexturesCube(photo_list.photos[i].id.ToString()));
									StartCoroutine(WaitForTexturesGPU(photo_list.photos[i].id.ToString()));
									numLoaded++;
								}
							}
						}
					}
				}
				else
				{
					yield return new WaitForSeconds(0.1f);
				}
			}
		}
	}

    void GetImageCallback(string result_data)
    {
		/*using (StreamWriter outputFile = new StreamWriter(Path.Combine(Application.streamingAssetsPath, "out.txt")))
		{
			outputFile.WriteLine(result_data);
		}*/
		//Debug.Log(result_data);

		_nextReady = true;

		_currentParent = new GameObject("H2_3DScan");
		_currentParent.transform.parent = gameObject.transform;
		_currentParent.transform.position = Vector3.zero;
		Quaternion q = Quaternion.identity;
		q.eulerAngles = new Vector3(-90f, 0f, 0f);
		_currentParent.transform.rotation = q;
		//Debug.Log("Callback");
		StartCoroutine(LoadPointClouds(result_data));

		//public string s = "[{"annotations":[{"boundary":{"height":0.5230216979980469,"left":0.3690803796052933,"top":0.46514296531677246,"width":0.29712721705436707},"confidence":0.8203831315040588,"id":141,"identified_user_id":null,"label":"person","photo_record_id":154,"sublabel":""}],"camera_location_id":"69e92dff-7138-4091-89c4-ed073035bfe6","created":1670279509.861595,"created_by":null,"device_pose_id":null,"files":[],"id":154,"imageUrl":"/photos/154/image","priority":0,"queue_name":"done","ready":true,"retention":"auto","status":"done","updated":1707769869.494515}]"";
	}

	IEnumerator WaitForTexturesGPU(string id)
	{
		while(!_newGeom || !_newColor || !_newDepth)
		{
			yield return null;
		}

		int numberIndex = 0;

		if(_newGeom && _newColor && _newDepth && 
			_colorTex.width == depthWidth && _colorTex.height == depthHeight && 
			_geomTex.width == depthWidth && _geomTex.height == depthHeight &&
			_depthTex.width == depthWidth && _depthTex.height == depthHeight)
		{
			Matrix4x4 scanTrans = Matrix4x4.TRS(_currentPosition, _currentRotation, Vector3.one);
			
			GameObject ip = Instantiate(ipadDebugPrefab);

			ip.name = id;

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			scanTrans = zScale * scanTrans;
			ip.transform.GetChild(0).transform.localPosition = scanTrans.GetColumn(3);
		
			Vector3 scaleX = scanTrans.GetColumn(0);
			
			Vector3 scaleY = scanTrans.GetColumn(1);
			
			Vector3 scaleZ = scanTrans.GetColumn(2);

			ip.transform.GetChild(0).transform.localRotation = Quaternion.LookRotation(scaleZ, scaleY);
			
			ip.transform.GetChild(0).name = id + "_photo";
			
			ip.transform.SetParent(_currentParent.transform, false);
			
			Texture2D colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_colorTex, colorTex);

			Texture2D geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_geomTex, geomTex);

			Texture2D depthTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_depthTex, depthTex);

			//Debug.Log("Setting texture for " + i);
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/TextureCullOff"));//"));
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = colorTex;
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh(id, (uint)depthWidth, (uint)depthHeight);

			//ipc.CameraIntrinsics = camIntrinsics;
			//ipc.ViewProj = viewProjMat;
			ipc.ModelTransform = scanTrans;
			
			//ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			//ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			ipc.PointMaterial.SetTexture("_DepthImage", geomTex);
			ipc.PointMaterial.SetTexture("_LocalPCImage", depthTex);
			
			
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;
		}
		else
		{
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;	
		}
	}

	IEnumerator WaitForTextures(string id)
	{
		while(!_newGeom || !_newColor || !_newDepth)
		{
			yield return null;
		}

		int numberIndex = 0;

		if(_newGeom && _newColor && _newDepth && 
			_colorTex.width == depthWidth && _colorTex.height == depthHeight && 
			_geomTex.width == depthWidth && _geomTex.height == depthHeight &&
			_depthTex.width == depthWidth && _depthTex.height == depthHeight)
		{
			Vector3 maxB = new Vector3(-99999f, -99999f, -99999f);
			Vector3 minB = new Vector3(99999f, 99999f, 99999f);

			Matrix4x4 viewProjMat = Matrix4x4.identity;
			Matrix4x4 camIntrinsics = Matrix4x4.identity;

			Matrix4x4 scanTrans = Matrix4x4.identity;
	
			Unity.Collections.NativeArray<byte> geomBytes = _geomTex.GetRawTextureData<byte>();
			Unity.Collections.NativeArray<byte> depthBytes = _depthTex.GetRawTextureData<byte>();
			Unity.Collections.NativeArray<byte> colorBytes = _colorTex.GetRawTextureData<byte>();

			int numVerts = geomBytes.Length / 4;
			int[] indices = new int[numVerts];
		
			Vector3 [] verts = new Vector3[numVerts];
			Color[] colors = new Color[numVerts];

			//Debug.Log(geomBytes.Length);
			//const int HEADER_SIZE = 54;

			int colorByteCount = 0;
			for(int j = 0; j < geomBytes.Length; j+=4)
			{
				byte b1 = geomBytes[j];
				byte g1 = geomBytes[j+1];
				byte r1 = geomBytes[j+2];
				byte a1 = geomBytes[j+3];

				//sigma buffer is in a1

				byte b2 = depthBytes[j];
				byte g2 = depthBytes[j+1];
				byte r2 = depthBytes[j+2];
				byte a2 = depthBytes[j+3];
				
				//cube size is in a2 / b2

				ushort a = (ushort)a1;
				ushort x1 = (ushort)((b1 & 0x000F) << 8);
				ushort y1 = (ushort)((b1 & 0x00F0) << 4);
				ushort z1 = (ushort)((g2 & 0x00FF) << 8);

				ushort x = (ushort)(x1 | ((ushort)r1));
				ushort y = (ushort)(y1 | ((ushort)g1));
				ushort z = (ushort)(z1 | ((ushort)r2));

				//ushort alpha = (ushort)((ushort)a1 | (ushort)((ushort)a2 << 8));
				//int x = (int)(r1 | (r2 << 8));
				//int y = (int)(g1 | (g2 << 8));
				//int z = (int)(b1 | (b2 << 8));

				int x2 = (int)x;
				x2 = x2 - 2048;
				int y2 = (int)y;
				y2 = y2 - 2048;
				int z2 = (int)z;
				//alpha = alpha - 32768;

				float fX = (float)x2 / 1000.0f;
				float fY = (float)y2 / 1000.0f;
				float fZ = (float)z2 / 1000.0f;

				if(x2 != 0 && y2 != 0 && z2 != 0)
				{
					//Debug.Log(r1 + " " + r2 + " " + g1 + " " + g2 + " " + b1 + " " + b2  + " " + a1 + " " + a2);
					//Debug.Log(x + " " + y + " " + z + " " + alpha);
					//Debug.Log(fX + " " + fY + " " + fZ);
				}

				verts[j/4] = new Vector3(fX, fY, fZ);
				colors[j/4] = new Color((float)colorBytes[colorByteCount+1]/255.0f, (float)colorBytes[colorByteCount+2]/255.0f, (float)colorBytes[colorByteCount+3]/255.0f, 1.0f);
				/*if(red != 0 || green != 0 || blue != 0)
				{
					Debug.Log(red);
					Debug.Log(green);
					Debug.Log(blue);
					Debug.Log(alpha);
				}*/
				indices[j/4] = j/4;
				colorByteCount+=4;
			}


			scanTrans = Matrix4x4.TRS(_currentPosition, _currentRotation, Vector3.one);
			//Debug.Log(scanTrans.ToString());
			//scanTrans = scanTrans.transpose;
			//Vector4 vP = Vector4.zero;
			//vP.x = _currentPosition.x;
			//vP.y = _currentPosition.y;
			//vP.z = _currentPosition.z;
			//vP.w = 1f;
			//scanTrans.SetColumn(3, vP);

			//ipadDebugPrefab.name = id;

			GameObject ip = Instantiate(ipadDebugPrefab);

			ip.name = id;

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			scanTrans = zScale * scanTrans;

			//Debug.Log(scanTrans.ToString());

			/*for(int i = 0; i < numVerts; ++i)
			{
				Vector4 v = Vector4.zero;
				v.x = verts[i].x;
				v.y = verts[i].y;
				v.z = verts[i].z;
				v.w = 1f;
				v = scanTrans * v;
				verts[i].x = v.x;
				verts[i].y = v.y;
				verts[i].z = v.z;
			}*/
		
			ip.transform.GetChild(0).transform.localPosition = scanTrans.GetColumn(3);
		
			Vector3 scaleX = scanTrans.GetColumn(0);
			
			Vector3 scaleY = scanTrans.GetColumn(1);
			
			Vector3 scaleZ = scanTrans.GetColumn(2);

			ip.transform.GetChild(0).transform.localRotation = Quaternion.LookRotation(scaleZ, scaleY);
			
			ip.transform.GetChild(0).name = id + "_photo";
			
			ip.transform.SetParent(_currentParent.transform, false);
			
			Texture2D colorTex = new Texture2D(320, 288, TextureFormat.ARGB32, false);
			Graphics.CopyTexture(_colorTex, colorTex);

			//Debug.Log("Setting texture for " + i);
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/TextureCullOff"));//"));
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = colorTex;
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh(id, (uint)depthWidth, (uint)depthHeight);
#if MESH_BASED
			ipc.GetComponent<MeshFilter>().sharedMesh.vertices = verts;
			ipc.GetComponent<MeshFilter>().sharedMesh.colors = colors;
			ipc.GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			ipc.GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Points, 0, false);
			ipc.GetComponent<MeshFilter>().sharedMesh.UploadMeshData(false);
#endif
			ipc.CameraIntrinsics = camIntrinsics;
			ipc.ViewProj = viewProjMat;
			ipc.ModelTransform = scanTrans;
			
			ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			ipc.PointMaterial.SetTexture("_DepthImage", _geomTex);
			
			
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;
		}
		else
		{
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;
		}
	}

	IEnumerator WaitForTexturesCube(string id)
	{
		while(!_newGeom || !_newColor || !_newDepth)
		{
			yield return null;
		}

		int numberIndex = 0;

		if(_newGeom && _newColor && _newDepth && 
			_colorTex.width == depthWidth && _colorTex.height == depthHeight && 
			_geomTex.width == depthWidth && _geomTex.height == depthHeight &&
			_depthTex.width == depthWidth && _depthTex.height == depthHeight)
		{
			Matrix4x4 scanTrans = Matrix4x4.TRS(_currentPosition, _currentRotation, Vector3.one);
			
			GameObject ip = Instantiate(ipadDebugPrefab);

			ip.name = id;

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			scanTrans = zScale * scanTrans;
			ip.transform.GetChild(0).transform.localPosition = scanTrans.GetColumn(3);
		
			Vector3 scaleX = scanTrans.GetColumn(0);
			
			Vector3 scaleY = scanTrans.GetColumn(1);
			
			Vector3 scaleZ = scanTrans.GetColumn(2);

			ip.transform.GetChild(0).transform.localRotation = Quaternion.LookRotation(scaleZ, scaleY);
			
			ip.transform.GetChild(0).name = id + "_photo";
			
			ip.transform.SetParent(_currentParent.transform, false);
			
			Texture2D colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_colorTex, colorTex);

			Texture2D geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_geomTex, geomTex);

			Texture2D depthTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
			Graphics.CopyTexture(_depthTex, depthTex);

			//Debug.Log("Setting texture for " + i);
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/TextureCullOff"));//"));
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = colorTex;
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			CubeTest c = ip.GetComponent<CubeTest>();
			c.AssignData(colorTex, geomTex, depthTex, scanTrans);

			/*ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh(id, (uint)depthWidth, (uint)depthHeight);

			//ipc.CameraIntrinsics = camIntrinsics;
			//ipc.ViewProj = viewProjMat;
			ipc.ModelTransform = scanTrans;
			
			//ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			//ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			ipc.PointMaterial.SetTexture("_DepthImage", geomTex);
			ipc.PointMaterial.SetTexture("_LocalPCImage", depthTex);*/
			
			
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;
		}
		else
		{
			_newGeom = false;
			_newColor = false;
			_newDepth = false;
			_nextReady = true;
			numberIndex++;	
		}
	}

	[ContextMenu("GetFromServer")]
	void ImageGetTest()
	{
		/*string s = "{\"photos\":[{\"annotations\":[{\"boundary\":{\"height\":0.5230216979980469,\"left\":0.3690803796052933,\"top\":0.46514296531677246,\"width\":0.29712721705436707},\"confidence\":0.8203831315040588,\"id\":141,\"identified_user_id\":null,\"label\":\"person\",\"photo_record_id\":154,\"sublabel\":\"\"}],\"camera_location_id\":\"69e92dff-7138-4091-89c4-ed073035bfe6\",\"created\":1670279509.861595,\"created_by\":null,\"device_pose_id\":null,\"files\":[],\"id\":154,\"imageUrl\":\"/photos/154/image\",\"priority\":0,\"queue_name\":\"done\",\"ready\":true,\"retention\":\"auto\",\"status\":\"done\",\"updated\":1707769869.494515}]}";
		Debug.Log(s);
		EasyVizAR.PhotoListReturn photo_list  = JsonUtility.FromJson<EasyVizAR.PhotoListReturn>(s);
		for(int i = 0; i < photo_list.photos.Length; ++i)
		{
			Debug.Log(photo_list.photos[i].camera_location_id);
		}*/
		StartCoroutine(DelayGet(3f));
	}
	
	IEnumerator DelayGet(float duration) {
		yield return new WaitForSeconds(duration);
		EasyVizARServer.Instance.Get("photos?since=2024-04-29&camera_location_id=1cc48e8d-890d-413a-aa66-cabaaa6e5458", EasyVizARServer.JSON_TYPE, GetImageCallback);
	}

	///photos/{photo_id}/{filename}
	[ContextMenu("Debug Hololens 2 Scan")]
	void DebugHololens2Capture()
	{
		string[] scansRGB = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/rgb/", "*.png");
		string[] scansDepth = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/depth/", "*.png");
		string[] scansTrans = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/trans/", "*.txt");

		Vector3 maxB = new Vector3(-99999f, -99999f, -99999f);
		Vector3 minB = new Vector3(99999f, 99999f, 99999f);
		int zeroCount = 0;
		
		int numScans = scansRGB.Length;

		Matrix4x4 viewProjMat = Matrix4x4.identity;
		Matrix4x4 camIntrinsics = Matrix4x4.identity;

		int numberIndex = 0;

		for(int i = 0; i < numScans; ++i)
		{
			string sPNG = scansRGB[i];
			sPNG = sPNG.Replace("Assets/Resources/", "");
			sPNG = sPNG.Replace(".png", "");
			
			//Debug.Log(sPNG);

			string sPNGD = scansDepth[i];
			sPNGD = sPNGD.Replace("Assets/Resources/", "");
			sPNGD = sPNGD.Replace(".png", "");

			//Debug.Log(sPNGD);

			string[] scanTransLines = File.ReadAllLines(scansTrans[i]);
			
			Matrix4x4 scanTrans = Matrix4x4.identity;
			
			string mat1D = scanTransLines[0];
			string[] mat1ValsD = mat1D.Split(' ');
			scanTrans[0] = float.Parse(mat1ValsD[0]);
			scanTrans[1] = float.Parse(mat1ValsD[1]);
			scanTrans[2] = float.Parse(mat1ValsD[2]);
			scanTrans[3] = float.Parse(mat1ValsD[3]);
			
			string mat2D = scanTransLines[1];
			string[] mat2ValsD = mat2D.Split(' ');
			scanTrans[4] = float.Parse(mat2ValsD[0]);
			scanTrans[5] = float.Parse(mat2ValsD[1]);
			scanTrans[6] = float.Parse(mat2ValsD[2]);
			scanTrans[7] = float.Parse(mat2ValsD[3]);
			
			string mat3D = scanTransLines[2];
			string[] mat3ValsD = mat3D.Split(' ');
			scanTrans[8] = float.Parse(mat3ValsD[0]);
			scanTrans[9] = float.Parse(mat3ValsD[1]);
			scanTrans[10] = float.Parse(mat3ValsD[2]);
			scanTrans[11] = float.Parse(mat3ValsD[3]);
			
			string mat4D = scanTransLines[3];
			string[] mat4ValsD = mat4D.Split(' ');
			scanTrans[12] = float.Parse(mat4ValsD[0]);
			scanTrans[13] = float.Parse(mat4ValsD[1]);
			scanTrans[14] = float.Parse(mat4ValsD[2]);
			scanTrans[15] = float.Parse(mat4ValsD[3]);
			
			/*Debug.Log(scanTrans.ToString());

			Matrix4x4 depthTrans = Matrix4x4.identity;

			for(int k = 0; k < 4; ++k)
			{
				string[] vals = scanTransLines[k].Split(" ");
				for(int j = 0; j < 4; ++j)
				{
					depthTrans[k*4+j] = float.Parse(vals[j]);
				}
			}

			Debug.Log(depthTrans.ToString());*/
			
			//Vector3 pos = scanTrans.GetPosition();
			//Vector3 vScale = scanTrans.lossyScale;
			//Debug.Log(vScale.ToString());
			//Quaternion rot = scanTrans.rotation;
			
			

			//Matrix4x4 mTest = Matrix4x4.TRS(pos, rot, Vector3.one);
			//Debug.Log("Test:");
			//Debug.Log(mTest.ToString());

			//load depth texture...
			Texture2D depthTex = Resources.Load<Texture2D>(sPNGD);
			Texture2D colorTex = Resources.Load<Texture2D>(sPNG);
#if MESH_BASED
			//check for RGBA16 values here...
			Unity.Collections.NativeArray<byte> geomBytes = depthTex.GetRawTextureData<byte>();
			Unity.Collections.NativeArray<byte> colorBytes = colorTex.GetRawTextureData<byte>();

			int numVerts = geomBytes.Length / 8;
			int[] indices = new int[numVerts];
		
			Vector3 [] verts = new Vector3[numVerts];
			Vector3 [] normals = new Vector3[numVerts];

			Color[] colors = new Color[numVerts];
			//Debug.Log(geomBytes.Length);
			int colorByteCount = 0;
			for(int j = 0; j < geomBytes.Length; j+=8)
			{
				byte r1 = geomBytes[j];
				byte r2 = geomBytes[j+1];
				byte g1 = geomBytes[j+2];
				byte g2 = geomBytes[j+3];
				byte b1 = geomBytes[j+4];
				byte b2 = geomBytes[j+5];
				byte a1 = geomBytes[j+6];
				byte a2 = geomBytes[j+7];

				int x = (int)(r1 | (r2 << 8));
				int y = (int)(g1 | (g2 << 8));
				int z = (int)(b1 | (b2 << 8));
				ushort alpha = (ushort)(a1 | (a2 << 8));

				x = x - 32768;
				y = y - 32768;
				z = z - 32768;
				
				float fX = (float)x / 1000.0f;
				float fY = (float)y / 1000.0f;
				float fZ = (float)z / 1000.0f;

				//if(i == 0)
				//{
					//Debug.Log(fX + " " + fY + " " + fZ);
				//}

				verts[j/8] = new Vector3(fX, fY, fZ);
				colors[j/8] = new Color((float)colorBytes[colorByteCount]/255.0f, (float)colorBytes[colorByteCount+1]/255.0f, (float)colorBytes[colorByteCount+2]/255.0f, 1.0f);
				normals[j/8] = new Vector3(0f, 1f, 0f);
				/*if(red != 0 || green != 0 || blue != 0)
				{
					Debug.Log(red);
					Debug.Log(green);
					Debug.Log(blue);
					Debug.Log(alpha);
				}*/
				indices[j/8] = j/8;
				colorByteCount+=4;
			}
#endif
			
			GameObject ip = Instantiate(ipadDebugPrefab);

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			scanTrans = zScale * scanTrans;
		
			ip.transform.GetChild(0).transform.localPosition = scanTrans.GetColumn(3);
		
			Vector3 scaleX = scanTrans.GetColumn(0);
			
			Vector3 scaleY = scanTrans.GetColumn(1);
			
			Vector3 scaleZ = scanTrans.GetColumn(2);

			ip.transform.GetChild(0).transform.localRotation = Quaternion.LookRotation(scaleZ, scaleY);
			
			ip.transform.GetChild(0).name = "hololens2_" + numberIndex;
			
			
			//Debug.Log("Setting texture for " + i);
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/TextureCullOff"));//"));
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = colorTex;
			//ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh("ipadImage", (uint)depthWidth, (uint)depthHeight);
#if MESH_BASED
			ipc.GetComponent<MeshFilter>().sharedMesh.vertices = verts;
			ipc.GetComponent<MeshFilter>().sharedMesh.colors = colors;
			ipc.GetComponent<MeshFilter>().sharedMesh.normals = normals;
			ipc.GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			ipc.GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Points, 0, false);
			ipc.GetComponent<MeshFilter>().sharedMesh.UploadMeshData(true);
#endif
			ipc.CameraIntrinsics = camIntrinsics;
			//ipc.ViewProj = viewProjMat;
			//ipc.ModelTransform = scanTrans;
			
			ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			//Debug.Log(depthTex.format);
			ipc.PointMaterial.SetTexture("_DepthImage", depthTex);
			
		}	
	}

	[ContextMenu("Debug Hololens 2 Scan New")]
	void DebugHololens2CaptureNew()
	{
		//string[] scansRGB = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/rgb/", "*.png");
		string[] scansDepth = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/localPC/", "*.bmp");
		//string[] scansTrans = Directory.GetFiles("Assets/Resources/"+inputDirectory+"/trans/", "*.txt");

		Vector3 maxB = new Vector3(-99999f, -99999f, -99999f);
		Vector3 minB = new Vector3(99999f, 99999f, 99999f);
		int zeroCount = 0;
		
		int numScans = scansDepth.Length;

		Matrix4x4 viewProjMat = Matrix4x4.identity;
		Matrix4x4 camIntrinsics = Matrix4x4.identity;

		int numberIndex = 0;

		for(int i = 0; i < numScans; ++i)
		{
			string sPNGD = scansDepth[i];
			sPNGD = sPNGD.Replace("Assets/Resources/", "");
			sPNGD = sPNGD.Replace(".bmp", "");

			string sPNG = sPNGD.Replace("_localPC", "_color");
			sPNG = sPNG.Replace("/localPC/", "/rgb/");

			string scanTransS = scansDepth[i];
			scanTransS = scanTransS.Replace("/localPC/", "/trans/");
			scanTransS = scanTransS.Replace("_localPC.bmp", "_trans.txt");
			//scanTransS = scanTransS + ".txt";
			//sPNG = sPNG.Replace("Assets/Resources/", "");
			//sPNG = sPNG.Replace(".png", "");
			
			Debug.Log(sPNG);
			Debug.Log(sPNGD);
			Debug.Log(scanTransS);

			string[] scanTransLines = File.ReadAllLines(scanTransS);
			
			Matrix4x4 scanTrans = Matrix4x4.identity;
			
			string mat1D = scanTransLines[0];
			string[] mat1ValsD = mat1D.Split(' ');
			scanTrans[0] = float.Parse(mat1ValsD[0]);
			scanTrans[1] = float.Parse(mat1ValsD[1]);
			scanTrans[2] = float.Parse(mat1ValsD[2]);
			scanTrans[3] = float.Parse(mat1ValsD[3]);
			
			string mat2D = scanTransLines[1];
			string[] mat2ValsD = mat2D.Split(' ');
			scanTrans[4] = float.Parse(mat2ValsD[0]);
			scanTrans[5] = float.Parse(mat2ValsD[1]);
			scanTrans[6] = float.Parse(mat2ValsD[2]);
			scanTrans[7] = float.Parse(mat2ValsD[3]);
			
			string mat3D = scanTransLines[2];
			string[] mat3ValsD = mat3D.Split(' ');
			scanTrans[8] = float.Parse(mat3ValsD[0]);
			scanTrans[9] = float.Parse(mat3ValsD[1]);
			scanTrans[10] = float.Parse(mat3ValsD[2]);
			scanTrans[11] = float.Parse(mat3ValsD[3]);
			
			string mat4D = scanTransLines[3];
			string[] mat4ValsD = mat4D.Split(' ');
			scanTrans[12] = float.Parse(mat4ValsD[0]);
			scanTrans[13] = float.Parse(mat4ValsD[1]);
			scanTrans[14] = float.Parse(mat4ValsD[2]);
			scanTrans[15] = float.Parse(mat4ValsD[3]);
			
			//Debug.Log(scanTrans.ToString());

			/*Matrix4x4 depthTrans = Matrix4x4.identity;

			for(int k = 0; k < 4; ++k)
			{
				string[] vals = scanTransLines[k].Split(" ");
				for(int j = 0; j < 4; ++j)
				{
					depthTrans[k*4+j] = float.Parse(vals[j]);
				}
			}

			Debug.Log(depthTrans.ToString());*/
			
			//Vector3 pos = scanTrans.GetPosition();
			//Vector3 vScale = scanTrans.lossyScale;
			//Debug.Log(vScale.ToString());
			//Quaternion rot = scanTrans.rotation;
			
			

			//Matrix4x4 mTest = Matrix4x4.TRS(pos, rot, Vector3.one);
			//Debug.Log("Test:");
			//Debug.Log(mTest.ToString());

			//load depth texture...
			Texture2D depthTex = Resources.Load<Texture2D>(sPNGD);
			Texture2D colorTex = Resources.Load<Texture2D>(sPNG);
#if MESH_BASED
			//check for RGBA16 values here...
			Unity.Collections.NativeArray<byte> geomBytes = depthTex.GetRawTextureData<byte>();
			Unity.Collections.NativeArray<byte> colorBytes = colorTex.GetRawTextureData<byte>();

			int numVerts = geomBytes.Length / 4;
			int[] indices = new int[numVerts];
		
			Vector3 [] verts = new Vector3[numVerts];
			Vector3 [] normals = new Vector3[numVerts];

			Color[] colors = new Color[numVerts];
			//Debug.Log(geomBytes.Length);
			int colorByteCount = 0;
			for(int j = 0; j < geomBytes.Length; j+=4)
			{
				byte r1 = geomBytes[j];
				byte g1 = geomBytes[j+1];
				byte b1 = geomBytes[j+2];
				byte a1 = geomBytes[j+3];
				ushort a = (ushort)a1;
				ushort x = (ushort)(((a & 0x07) << 8) | ((ushort)r1));
				ushort y = (ushort)(((a & 0x18) << 5) | ((ushort)g1));
				ushort z = (ushort)(((a & 0xE0) << 3) | ((ushort)b1));
				
				
				int x2 = (int)x;
				if(x2 != 0)
				{
					x2 = x2 - 1024;
				}
				
				int y2 = (int)y;
				y2 = -y2;
				int z2 = (int)z;
				//alpha = alpha - 32768;

				//Debug.Log(x2 + " " + y2 + " " + z2);

				float fX = (float)x2 / 1000.0f;
				float fY = (float)y2 / 1000.0f;
				float fZ = (float)z2 / 1000.0f;

				//if(i == 0)
				//{
					//Debug.Log(fX + " " + fY + " " + fZ);
				//}

				verts[j/4] = new Vector3(fX, fY, fZ);
				colors[j/4] = new Color((float)colorBytes[colorByteCount]/255.0f, (float)colorBytes[colorByteCount+1]/255.0f, (float)colorBytes[colorByteCount+2]/255.0f, 1.0f);
				normals[j/4] = new Vector3(0f, 1f, 0f);
				/*if(red != 0 || green != 0 || blue != 0)
				{
					Debug.Log(red);
					Debug.Log(green);
					Debug.Log(blue);
					Debug.Log(alpha);
				}*/
				indices[j/4] = j/4;
				colorByteCount+=4;
			}
#endif
			
			GameObject ip = Instantiate(ipadDebugPrefab);

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			scanTrans = zScale * scanTrans;
		
			ip.transform.GetChild(0).transform.localPosition = scanTrans.GetColumn(3);
		
			Vector3 scaleX = scanTrans.GetColumn(0);
			
			Vector3 scaleY = scanTrans.GetColumn(1);
			
			Vector3 scaleZ = scanTrans.GetColumn(2);

			ip.transform.GetChild(0).transform.localRotation = Quaternion.LookRotation(scaleZ, scaleY);
			
			ip.transform.GetChild(0).name = "hololens2_" + numberIndex;
			
			
			//Debug.Log("Setting texture for " + i);
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/TextureCullOff"));//"));
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = colorTex;
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh("ipadImage", (uint)depthWidth, (uint)depthHeight);
#if MESH_BASED
			ipc.GetComponent<MeshFilter>().sharedMesh.vertices = verts;
			ipc.GetComponent<MeshFilter>().sharedMesh.colors = colors;
			ipc.GetComponent<MeshFilter>().sharedMesh.normals = normals;
			ipc.GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			ipc.GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Points, 0, false);
			ipc.GetComponent<MeshFilter>().sharedMesh.UploadMeshData(true);
#endif
			ipc.CameraIntrinsics = camIntrinsics;
			//ipc.ViewProj = viewProjMat;
			//ipc.ModelTransform = scanTrans;
			
			ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			//Debug.Log(depthTex.format);
			ipc.PointMaterial.SetTexture("_DepthImage", depthTex);
			
		}	
	}
}
