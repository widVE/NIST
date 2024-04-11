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
	int depthWidth;
	
	[SerializeField]
	int depthHeight;

	[SerializeField]
	RenderTexture _colorTexture;

	Texture2D _colorTex;

	[SerializeField]
	RenderTexture _pointCloudTexture;

	Texture2D _geomTex;

	Matrix4x4 _transform = Matrix4x4.identity;

	bool _nextReady = true;
	bool _newGeom = false;
	bool _newColor = false;

	Vector3 _currentPosition = Vector3.zero;
	Quaternion _currentRotation = Quaternion.identity;

    // Start is called before the first frame update
    void Start()
    {
		_colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
 		_geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
 
        //DebugHailScan();
		//DebugSimpleCapture();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void ColorTextureCallback(Texture2D textureData)
	{
		Debug.Log("Hit color callback");

		//RenderTexture currentRT = RenderTexture.active;
		if(_colorTex == null)
		{
			Debug.Log("Making color");
			_colorTex = new Texture2D(320, 288, TextureFormat.ARGB32, false);
		}
		//RenderTexture renderTexture = new RenderTexture(320, 288, 32);
		Graphics.CopyTexture(textureData, _colorTex);
		_colorTex.Apply(false);
		_newColor = true;
		/*Graphics.Blit(textureData, _colorTexture);

		RenderTexture prev = RenderTexture.active;

		RenderTexture.active = _colorTexture;
		if(_colorTex == null)
		{
			_colorTex = new Texture2D(320, 288, TextureFormat.RGBA32, -1, false);
			_colorTex.Apply(false);
		}

		_colorTex.ReadPixels(new Rect(0, 0, _colorTexture.width, _colorTexture.height), 0, 0);
		_colorTex.Apply(false);

		_newColor = true;

		RenderTexture.active = prev;*/
	}

	void GeomTextureCallback(Texture2D textureData)
	{
		Debug.Log("Hit geom callback");

		if(_geomTex == null)
		{
			Debug.Log("Making geom");
			_geomTex = new Texture2D(320, 288, TextureFormat.RGBA32, false);
		}

		Graphics.CopyTexture(textureData, _geomTex);
		_geomTex.Apply(false);
		_newGeom = true;
		//Debug.Log(textureData.width + " " + textureData.height + " " + textureData.graphicsFormat);
		//RenderTexture currentRT = RenderTexture.active;

		//RenderTexture renderTexture = new RenderTexture(320, 288, 32);
		/*Graphics.Blit(textureData, _pointCloudTexture);

		RenderTexture prev = RenderTexture.active;

		RenderTexture.active = _pointCloudTexture;
		if(_geomTex == null)
		{
			_geomTex = new Texture2D(320, 288, TextureFormat.RGBA64, -1, false);
			_geomTex.Apply(false);
		}

		_geomTex.ReadPixels(new Rect(0, 0, _pointCloudTexture.width, _pointCloudTexture.height), 0, 0);
		_geomTex.Apply(false);

		_newGeom = true;

		RenderTexture.active = prev;*/
	}

    void GetImageCallback(string result_data)
    {
		/*using (StreamWriter outputFile = new StreamWriter(Path.Combine(Application.streamingAssetsPath, "out.txt")))
		{
			outputFile.WriteLine(result_data);
		}*/
		//Debug.Log(result_data);
		bool once = false;
		
		_nextReady = true;
		Debug.Log("Callback");

		//public string s = "[{"annotations":[{"boundary":{"height":0.5230216979980469,"left":0.3690803796052933,"top":0.46514296531677246,"width":0.29712721705436707},"confidence":0.8203831315040588,"id":141,"identified_user_id":null,"label":"person","photo_record_id":154,"sublabel":""}],"camera_location_id":"69e92dff-7138-4091-89c4-ed073035bfe6","created":1670279509.861595,"created_by":null,"device_pose_id":null,"files":[],"id":154,"imageUrl":"/photos/154/image","priority":0,"queue_name":"done","ready":true,"retention":"auto","status":"done","updated":1707769869.494515}]"";
		EasyVizAR.PhotoListReturn photo_list  = JsonUtility.FromJson<EasyVizAR.PhotoListReturn>("{\"photos\":"+result_data+"}");
		Debug.Log(photo_list.photos.Length);
		for(int i = 0; i < photo_list.photos.Length; ++i)
		{
			if(_nextReady)
			{
				Debug.Log("ID: " + photo_list.photos[i].id);
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

							Debug.Log("Index: " + i);
							Debug.Log(photo_list.photos[i].camera_orientation.x);
							Debug.Log(photo_list.photos[i].camera_orientation.y);
							Debug.Log(photo_list.photos[i].camera_orientation.z);
							Debug.Log(photo_list.photos[i].camera_orientation.w);
							Debug.Log(photo_list.photos[i].camera_position.y);
							Debug.Log(photo_list.photos[i].camera_position.y);
							Debug.Log(photo_list.photos[i].camera_position.z);
							
							if(!once && _currentPosition.magnitude > 0)
							{
								//photo_list.photos[i].files[j].name
								_newGeom = false;
								_newColor = false;

								EasyVizARServer.Instance.Texture("https://easyvizar.wings.cs.wisc.edu/photos/"+photo_list.photos[i].id+"/photo.png", "image/png", "320", ColorTextureCallback);
								EasyVizARServer.Instance.Texture("https://easyvizar.wings.cs.wisc.edu/photos/"+photo_list.photos[i].id+"/geometry.bmp", "image/bmp", "320", GeomTextureCallback);
								once = true;
								_nextReady = false;

								StartCoroutine("WaitForTextures");
							}
						}
					}
				}
			}
		}
	}

	IEnumerator WaitForTextures()
	{
		while(!_newGeom || !_newColor)
		{
			yield return null;
		}

		int numberIndex = 0;

		if(_newGeom && _newColor)
		{
			Vector3 maxB = new Vector3(-99999f, -99999f, -99999f);
			Vector3 minB = new Vector3(99999f, 99999f, 99999f);

			Matrix4x4 viewProjMat = Matrix4x4.identity;
			Matrix4x4 camIntrinsics = Matrix4x4.identity;

			Matrix4x4 scanTrans = Matrix4x4.identity;
	
			Unity.Collections.NativeArray<byte> geomBytes = _geomTex.GetRawTextureData<byte>();
			Unity.Collections.NativeArray<byte> colorBytes = _colorTex.GetRawTextureData<byte>();

			int numVerts = geomBytes.Length / 4;
			int[] indices = new int[numVerts];
		
			Vector3 [] verts = new Vector3[numVerts];
			Color[] colors = new Color[numVerts];

			Debug.Log(geomBytes.Length);
			//const int HEADER_SIZE = 54;

			int colorByteCount = 0;
			for(int j = 0; j < geomBytes.Length; j+=4)
			{
				byte b1 = geomBytes[j];
				byte g1 = geomBytes[j+1];
				byte r1 = geomBytes[j+2];
				byte a1 = geomBytes[j+3];
				ushort a = (ushort)a1;
				ushort x = (ushort)(((a & 0x03) << 8) | ((ushort)r1));
				ushort y = (ushort)(((a & 0x0C) << 6) | ((ushort)g1));
				ushort z = (ushort)(((a & 0xF0) << 4) | ((ushort)b1));
				//ushort alpha = (ushort)((ushort)a1 | (ushort)((ushort)a2 << 8));
				//int x = (int)(r1 | (r2 << 8));
				//int y = (int)(g1 | (g2 << 8));
				//int z = (int)(b1 | (b2 << 8));

				int x2 = (int)x;
				x2 = x2 - 512;
				int y2 = (int)y;
				y2 = -y2;
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
			ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTexture = _colorTex;
			//ip.transform.GetChild(0).gameObject.GetComponent<Renderer>().sharedMaterial.mainTextureScale = new Vector2(-1,-1);
		

			ImagePointCloud ipc = ip.GetComponent<ImagePointCloud>();
			
			ipc.CreatePointMaterial();
			ipc.CreatePointMesh((uint)depthWidth, (uint)depthHeight);
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
			ipc.PointMaterial.SetTexture("_ColorImage", _colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			//Debug.Log(depthTex.format);
			ipc.PointMaterial.SetTexture("_DepthImage", _geomTex);
			
			//_nextReady = true;
			_newGeom = false;
			_newColor = false;

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
		EasyVizARServer.Instance.Get("https://easyvizar.wings.cs.wisc.edu/photos?since=2024-04-11&camera_location_id=69e92dff-7138-4091-89c4-ed073035bfe6", EasyVizARServer.JSON_TYPE, GetImageCallback);
	}

	///photos/{photo_id}/{filename}
	[ContextMenu("Debug Hololens 2 Scan New")]
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
			
			Vector3 pos = scanTrans.GetPosition();
			Vector3 vScale = scanTrans.lossyScale;
			Debug.Log(vScale.ToString());
			Quaternion rot = scanTrans.rotation;

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
			ipc.CreatePointMesh((uint)depthWidth, (uint)depthHeight);
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
