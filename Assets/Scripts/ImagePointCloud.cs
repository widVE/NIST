using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImagePointCloud : MonoBehaviour
{
    [SerializeField]
    Shader _pointShader;

    Matrix4x4 _cameraIntrinsics = Matrix4x4.identity;
    Matrix4x4 _viewProj = Matrix4x4.identity;
    Matrix4x4 _modelTransform = Matrix4x4.identity;

    Bounds _pointCloudBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 64f));
	
	const int SUBSAMPLE = 1;

    [SerializeField]
    Material _pointMaterial;

    public Material PointMaterial => _pointMaterial;
    
	//public List<Vector4> _featurePoints;
	//public List<ulong> _featurePointIDs;
	
    public Matrix4x4 CameraIntrinsics {
        get { return _cameraIntrinsics;}
        set {_cameraIntrinsics = value;}
    }

    public Matrix4x4 ViewProj {
        get { return _viewProj;}
        set {_viewProj = value;}
    }

    public Matrix4x4 ModelTransform {
        get { return _modelTransform;}
        set {_modelTransform = value;}
    }

    // Start is called before the first frame update
    void Start()
    {
		//if(_pointMaterial == null)
		//{
		//	_pointMaterial = new Material(_pointShader);
		//}
		 
        //CreatePointMesh(256, 192);
    }

    void Awake()
    {
		//if(_pointMaterial == null)
		//{
		//	_pointMaterial = new Material(_pointShader);
		//}
    }

	public void CreatePointMaterial()
	{
		_pointMaterial = new Material(_pointShader);
	}
	
	public void CreatePointMesh(string id, uint width, uint height)
	{
		//name = "ipadImage";
		GetComponent<MeshFilter>().sharedMesh = new Mesh();
		GetComponent<MeshFilter>().sharedMesh.name = id;
		
		uint resolution = width * height;
		uint subSampledResolution = width * height / SUBSAMPLE;
		
		//using currPoint here skips the zero points.
		Vector3[] verts = new Vector3[subSampledResolution];
		Color[] colors = new Color[subSampledResolution];
		
		int[] indices = new int[subSampledResolution];
		
		for(int j = 0; j < resolution; ++j)
		{
			if(j % SUBSAMPLE == 0)
			{
				int idx = j / SUBSAMPLE;

				indices[idx] = idx;
				verts[idx] = Vector3.zero;
				//verts[j].x = pointsInMemory[j].xPos;
				//verts[j].y = pointsInMemory[j].yPos;
				//verts[j].z = pointsInMemory[j].zPos;
				
				colors[idx] = Color.black;
				//colors[j].r = (float)colorsInMemory[j].red/255f;
				//colors[j].g = (float)colorsInMemory[j].green/255f;
				//colors[j].b = (float)colorsInMemory[j].blue/255f;
			}
		}
		
		GetComponent<MeshRenderer>().material = _pointMaterial;
		//GetComponent<MeshFilter>().sharedMesh.isReadable = true;
		GetComponent<MeshFilter>().sharedMesh.vertices = verts;
		GetComponent<MeshFilter>().sharedMesh.colors = colors;
		GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Points, 0, false);
		//ip.GetComponent<MeshFilter>().sharedMesh.bounds = new Bounds((maxB + minB) * 0.5f, maxB-minB);
		GetComponent<MeshFilter>().sharedMesh.bounds = _pointCloudBounds;
		GetComponent<MeshFilter>().sharedMesh.UploadMeshData(false);
		
	}
	
    // Update is called once per frame
    void Update()
    {
		if(_pointMaterial == null)
		{
			 _pointMaterial = new Material(_pointShader);
		}
		
		_pointMaterial.SetInt("_SubSample", SUBSAMPLE);
        _pointMaterial.SetMatrix("_ViewProj", _viewProj);
        _pointMaterial.SetMatrix("_ModelTransform", _modelTransform);
        _pointMaterial.SetMatrix("_CameraIntrinsics", _cameraIntrinsics);
        
        //draw procedural here - causees everything to render black for some reason on windows 2021 version, but not on Mac 2020 version
        //Graphics.DrawProcedural(_pointMaterial, _pointCloudBounds, MeshTopology.Points, 256*192, 1);//, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
    }
}
