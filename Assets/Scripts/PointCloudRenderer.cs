using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class PointCloudRenderer : MonoBehaviour
{
    public int maxChunkSize = 65535;
    public float pointSize = 0.005f;
    public GameObject pointCloudElem;
    public Material pointCloudMaterial;
	
	public string _lastPCFileName;
	
	Vector3[] _positions;
	Color[] _colors;
	
    List<GameObject> elems;

    void Start()
    {
        elems = new List<GameObject>();
		_positions = new Vector3[maxChunkSize];
		_colors = new Color[maxChunkSize];
		
        UpdatePointSize();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            UpdatePointSize();
			//UpdateMesh();
            transform.hasChanged = false;
        }
    }

	public void UpdateMesh()
	{
		string[] transLines = File.ReadAllLines(_lastPCFileName);

		int j = 0;
		for (int i = 0; i < transLines.Length; i++)
		{
			string[] pcVals = transLines[i].Split(' ');
			if(pcVals.Length == 6)
			{
				_positions[j].x = float.Parse(pcVals[0]);
				_positions[j].y = -float.Parse(pcVals[2]);
				_positions[j].z = -float.Parse(pcVals[1]);

				_colors[j].r = float.Parse(pcVals[3]);
				_colors[j].g = float.Parse(pcVals[4]);
				_colors[j].b = float.Parse(pcVals[5]);
				_colors[j].a = 1.0f;
				j++;
			}
			
			//pointCloudVector3[i] = new Vector3(pointCloudBuffer[3 * i], pointCloudBuffer[3 * i + 1], pointCloudBuffer[3 * i + 2]);
			/*if(_pcTest[i] != 0f && _pcTest[i+1] != 0f && _pcTest[i+2] != 0f)
			{
				s.Write(_pcTest[i].ToString("F4") + " " + _pcTest[i+1].ToString("F4")+ " " + _pcTest[i+2].ToString("F4") + " " + _pcTest[i+3].ToString("F4") + " " + _pcTest[i+4].ToString("F4")+ " " + _pcTest[i+5].ToString("F4")+ "\n");
			}*/
		}
		
		Render(_positions, _colors);
	}
    public void UpdatePointSize()
    {
        pointCloudMaterial.SetFloat("_PointSize", pointSize * transform.localScale.x);
    }

    public void Render(Vector3[] arrVertices, Color[] pointColors)
    {
        int nPoints, nChunks;
        if (arrVertices == null)
        {
            nPoints = 0;
            nChunks = 0;
        }
        else
        {
            nPoints = arrVertices.Length;
            nChunks = 1 + nPoints / maxChunkSize;
        }

        if (elems.Count < nChunks)
            AddElems(nChunks - elems.Count);
        if (elems.Count > nChunks)
            RemoveElems(elems.Count - nChunks);

        int offset = 0;
        for (int i = 0; i < nChunks; i++)
        {
            int nPointsToRender = System.Math.Min(maxChunkSize, nPoints - offset);

            ElemRenderer renderer = elems[i].GetComponent<ElemRenderer>();
            renderer.UpdateMesh(arrVertices, nPointsToRender, offset, pointColors);

            offset += nPointsToRender;
        }
    }

    void AddElems(int nElems)
    {
        for (int i = 0; i < nElems; i++)
        {
            GameObject newElem = GameObject.Instantiate(pointCloudElem);
            newElem.transform.parent = transform;
            newElem.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            newElem.transform.localRotation = Quaternion.identity;
            newElem.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            elems.Add(newElem);
        }
    }

    void RemoveElems(int nElems)
    {
        for (int i = 0; i < nElems; i++)
        {
            Destroy(elems[0]);
            elems.Remove(elems[0]);
        }
    }
}
