using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImagePointCloud : MonoBehaviour
{
    [SerializeField]
    Shader _pointShader;

    Matrix4x4 _cameraIntrinsics;
    Matrix4x4 _viewProj;
    Matrix4x4 _modelTransform;

    Bounds _pointCloudBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 64f));

    [SerializeField]
    Material _pointMaterial;

    public Material PointMaterial => _pointMaterial;
    
	public List<Vector4> _featurePoints;
	public List<ulong> _featurePointIDs;
	
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
		if(_pointMaterial == null)
		{
			_pointMaterial = new Material(_pointShader);
		}
		 
        CreatePointMesh(256, 192);
    }

    void Awake()
    {
		if(_pointMaterial == null)
		{
			_pointMaterial = new Material(_pointShader);
		}
    }

	public void CreatePointMaterial()
	{
		_pointMaterial = new Material(_pointShader);
	}
	
	public void CreatePointMesh(uint width, uint height)
	{
		name = "ipadImage";
		GetComponent<MeshFilter>().sharedMesh = new Mesh();
		GetComponent<MeshFilter>().sharedMesh.name = "ipadImage";
		
		uint resolution = width * height;
		
		//using currPoint here skips the zero points.
		Vector3[] verts = new Vector3[resolution];
		Color[] colors = new Color[resolution];
		
		int[] indices = new int[resolution];
		
		for(int j = 0; j < resolution; ++j)
		{
			indices[j] = j;
			verts[j] = Vector3.zero;
			//verts[j].x = pointsInMemory[j].xPos;
			//verts[j].y = pointsInMemory[j].yPos;
			//verts[j].z = pointsInMemory[j].zPos;
			
			colors[j] = Color.black;
			//colors[j].r = (float)colorsInMemory[j].red/255f;
			//colors[j].g = (float)colorsInMemory[j].green/255f;
			//colors[j].b = (float)colorsInMemory[j].blue/255f;
		}
		
		GetComponent<MeshFilter>().sharedMesh.vertices = verts;
		GetComponent<MeshFilter>().sharedMesh.colors = colors;
		GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		GetComponent<MeshFilter>().sharedMesh.SetIndices(indices, MeshTopology.Points, 0, false);
		//ip.GetComponent<MeshFilter>().sharedMesh.bounds = new Bounds((maxB + minB) * 0.5f, maxB-minB);
		GetComponent<MeshFilter>().sharedMesh.bounds = _pointCloudBounds;
		GetComponent<MeshFilter>().sharedMesh.UploadMeshData(false);
		GetComponent<MeshRenderer>().material = _pointMaterial;
	}
	
    // Update is called once per frame
    void Update()
    {
		if(_pointMaterial == null)
		{
			 _pointMaterial = new Material(_pointShader);
		}
		
        _pointMaterial.SetMatrix("_ViewProj", _viewProj);
        _pointMaterial.SetMatrix("_ModelTransform", _modelTransform);
        _pointMaterial.SetMatrix("_CameraIntrinsics", _cameraIntrinsics);
        
        //draw procedural here - causees everything to render black for some reason on windows 2021 version, but not on Mac 2020 version
        //Graphics.DrawProcedural(_pointMaterial, _pointCloudBounds, MeshTopology.Points, 256*192, 1);//, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0);
    }
}
