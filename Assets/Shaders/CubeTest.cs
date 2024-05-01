using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class CubeTest : MonoBehaviour
{
    public int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    int width = 320;
    int height = 288;

    public float size = .05f;

    public Texture2D inputImage = null;
    public Texture2D inputDepth = null;
    public Texture2D inputLocalPC = null;

    public Matrix4x4 matrix;
    public Matrix4x4 localMatrix;

    public Material material;

    void Start()
    {

    }
	
	void OnDestroy()
	{
		if(positionBuffer != null)
		{
			positionBuffer.Release();
			positionBuffer = null;
		}
		
		if(argsBuffer != null)
		{
			argsBuffer.Release();
			argsBuffer = null;
		}
	}
	
    public void AssignData(Texture2D color, Texture2D depth, Texture2D localPC, Matrix4x4 transform)
    {
        if(material == null) {
            material = new Material(instanceMaterial);
        }

        matrix = transform;
        
        inputImage = color;
        inputDepth = depth;
        inputLocalPC = localPC;

        material.SetTexture("_MainTex", inputImage);
        material.SetTexture("_DepthTex", inputDepth);
        material.SetTexture("_LocalPCTex", inputLocalPC);
        material.SetFloat("width", width);
        material.SetFloat("height", height);
        material.SetMatrix("_Matrix", matrix);

        localMatrix = matrix;
        localMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        material.SetMatrix("_localMatrix", localMatrix.inverse);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        UpdateBuffers();
    }

    void Update()
    {
        // Update starting position buffer
        //  if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        //      UpdateBuffers();

        if(inputDepth == null || inputImage == null || inputLocalPC == null)
        {
            return;
        }
        //// Pad input
        //if (Input.GetAxisRaw("Horizontal") != 0.0f)
        //    instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);

        // Render
        material.SetBuffer("positionBuffer", positionBuffer);
        //material.SetFloat("width", width);
        //material.SetFloat("height", height);
        //material.SetTexture("_MainTex", inputImage);
        //material.SetTexture("_DepthTex", inputDepth);
        //material.SetTexture("_LocalPCTex", inputLocalPC);
        material.SetMatrix("_Matrix", matrix);

        //  localMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 10*Time.time, 0));
        //  localMatrix = localMatrix * Matrix4x4.Scale(new Vector3(.05f, .05f, .05f));

        localMatrix = matrix;
        localMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        material.SetMatrix("_localMatrix", localMatrix.inverse);

        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, material, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
    }

    //void OnGUI()
    //{
    //    GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
    //    instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 5000000.0f);
    //}

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        instanceCount = width * height;

        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();

        positionBuffer = new ComputeBuffer(instanceCount, 16);
        Vector4[] positions = new Vector4[instanceCount];

        float xmid = width * size * .5f;
        float ymid = height * size * .5f;

        int i = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x=0;x<width; x++)
            {
                positions[i++] = new Vector4(size*x-xmid, size*y-ymid, 0, size);
            }
        }

        //for (int i = 0; i < instanceCount; i++)
        //{
        //    float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
        //    float distance = Random.Range(20.0f, 100.0f);
        //    float height = Random.Range(-2.0f, 2.0f);
        //    float size = Random.Range(0.05f, 0.25f);
        //    positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        //}
        positionBuffer.SetData(positions);
        material.SetBuffer("positionBuffer", positionBuffer);
        material.SetFloat("width", width);
        material.SetFloat("height", height);
       
        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }

        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    //void OnDisable()
    //{
    //    if (positionBuffer != null)
    //        positionBuffer.Release();
    //    positionBuffer = null;

    //    if (argsBuffer != null)
    //        argsBuffer.Release();
    //    argsBuffer = null;
    //}
}