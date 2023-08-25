using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LiDARVis : MonoBehaviour
{
	//directory of input ptx files as output from the scanning app
	public string inputDirectory;

	public GameObject ipadDebugPrefab;
		
	[SerializeField]
	int depthWidth;
	
	[SerializeField]
	int depthHeight;

    // Start is called before the first frame update
    void Start()
    {
        //DebugHailScan();
		//DebugSimpleCapture();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
			
			//load depth texture...
			Texture2D depthTex = Resources.Load<Texture2D>(sPNGD);
			Texture2D colorTex = Resources.Load<Texture2D>(sPNG);

			GameObject ip = Instantiate(ipadDebugPrefab);

			Matrix4x4 zScale = Matrix4x4.identity;
			Vector4 col2 = zScale.GetColumn(2);
			col2 = -col2;
			zScale.SetColumn(2, col2);
			
			//scanTrans = zScale * scanTrans;
		
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
			ipc.CameraIntrinsics = camIntrinsics;
			//ipc.ViewProj = viewProjMat;
			ipc.ModelTransform = scanTrans;
			
			ipc.PointMaterial.SetMatrix("_ViewProj", viewProjMat);
			ipc.PointMaterial.SetMatrix("_ModelTransform", scanTrans);
			ipc.PointMaterial.SetMatrix("_CameraIntrinsics", camIntrinsics);
			ipc.PointMaterial.SetTexture("_ColorImage", colorTex);
			ipc.PointMaterial.SetFloat("_ResolutionX", depthWidth);
			ipc.PointMaterial.SetFloat("_ResolutionY", depthHeight);
			ipc.PointMaterial.SetTexture("_DepthImage", depthTex);
			
		}	
	}
}
