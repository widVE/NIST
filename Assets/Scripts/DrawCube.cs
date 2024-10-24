using UnityEngine;
using System.Collections;
using System.IO;

public class DrawCube : MonoBehaviour 
{
    public int instanceCount = 100000;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
	private ComputeBuffer colorBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
	
	public string _pcFileName = "";
	string _lastPCFileName = "";
	Vector4[] positions;
	Vector4[] colors;
	
    void Start() 
	{
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		positionBuffer = new ComputeBuffer(instanceCount, 16);
		colorBuffer = new ComputeBuffer(instanceCount, 16);
		positions = new Vector4[instanceCount];
		colors = new Vector4[instanceCount];
		
		for(int i = 0; i < instanceCount; ++i)
		{
			positions[i] = Vector4.zero;
			colors[i] = Vector4.zero;
		}
		
		positionBuffer.SetData(positions);
		colorBuffer.SetData(colors);
		
		instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
		instanceMaterial.SetBuffer("colorBuffer", colorBuffer);

        //UpdateBuffers();
    }
	
	void OnDestroy()
	{
        if (positionBuffer != null)
            positionBuffer.Release();
		
        positionBuffer = null;
		
		if(colorBuffer != null)
			colorBuffer.Release();
		
		colorBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
		
        argsBuffer = null;
	}

    void Update() 
	{
		if(_pcFileName != "")
		{
			// Update starting position buffer
			if (_lastPCFileName != _pcFileName)//cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
			{
				_lastPCFileName = _pcFileName;
				UpdateBuffers();
			}

			// Pad input
			//if (Input.GetAxisRaw("Horizontal") != 0.0f)
			//    instanceCount = (int)Mathf.Clamp(instanceCount + Input.GetAxis("Horizontal") * 40000, 1.0f, 5000000.0f);

			// Render
			Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
		}
    }

    /*void OnGUI() {
        GUI.Label(new Rect(265, 25, 200, 30), "Instance Count: " + instanceCount.ToString());
        instanceCount = (int)GUI.HorizontalSlider(new Rect(25, 20, 200, 30), (float)instanceCount, 1.0f, 5000000.0f);
    }*/

    void UpdateBuffers() 
	{
		//Debug.Log("Updating buffers");
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

		string[] transLines = File.ReadAllLines(_lastPCFileName);

		int j = 0;
		for (int i = 0; i < transLines.Length; i++)
		{
			string[] pcVals = transLines[i].Split(' ');
			if(pcVals.Length == 6)
			{
				positions[j].x = float.Parse(pcVals[0]);
				positions[j].y = float.Parse(pcVals[1]);
				positions[j].z = float.Parse(pcVals[2]);
				positions[j].w = 1.0f;
				colors[j].x = float.Parse(pcVals[3]);
				colors[j].y = float.Parse(pcVals[4]);
				colors[j].z = float.Parse(pcVals[5]);
				colors[j].w = 1.0f;
				j++;
			}
			
			//pointCloudVector3[i] = new Vector3(pointCloudBuffer[3 * i], pointCloudBuffer[3 * i + 1], pointCloudBuffer[3 * i + 2]);
			/*if(_pcTest[i] != 0f && _pcTest[i+1] != 0f && _pcTest[i+2] != 0f)
			{
				s.Write(_pcTest[i].ToString("F4") + " " + _pcTest[i+1].ToString("F4")+ " " + _pcTest[i+2].ToString("F4") + " " + _pcTest[i+3].ToString("F4") + " " + _pcTest[i+4].ToString("F4")+ " " + _pcTest[i+5].ToString("F4")+ "\n");
			}*/
		}
		
		instanceCount = j;
		/*for(int i = transLines.Length; i < instanceCount; ++i)
		{
			positions[i] = Vector4.zero;
			colors[i] = Vector4.zero;
		}*/
								
        //for (int i = 0; i < instanceCount; i++) {
            /*float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(0.05f, 0.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);*/
        //}
		
        positionBuffer.SetData(positions);
		colorBuffer.SetData(colors);
		
        // Indirect args
        if (instanceMesh != null) {
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
}