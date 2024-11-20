using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class CustomOBJImporter : MonoBehaviour
{
    public string objFilePath; // Path to the OBJ file relative to the StreamingAssets folder

    void Start()
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, objFilePath);
        Mesh mesh = LoadOBJ(fullPath);

        if (mesh != null)
        {
            // Assign the mesh to the MeshFilter component
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            // Add a MeshRenderer and assign a material with a vertex color shader
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Custom/VertexColorLitShader"));
        }
    }

    Mesh LoadOBJ(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"File not found at path: {path}");
            return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        string[] lines = File.ReadAllLines(path);
        CultureInfo culture = CultureInfo.InvariantCulture;

        foreach (string line in lines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
                continue;

            switch (parts[0])
            {
                case "v":
                    // Vertex position and color
                    if (parts.Length >= 7)
                    {
                        // Position
                        float x = float.Parse(parts[1], culture);
                        float y = float.Parse(parts[2], culture);
                        float z = float.Parse(parts[3], culture);
                        vertices.Add(new Vector3(x, y, z));

                        // Color (assuming RGB in range [0,1])
                        float r = float.Parse(parts[4], culture);
                        float g = float.Parse(parts[5], culture);
                        float b = float.Parse(parts[6], culture);
                        colors.Add(new Color(r, g, b));
                    }
                    else
                    {
                        Debug.LogWarning("Vertex definition does not contain color information.");
                    }
                    break;

                case "vn":
                    // Vertex normal
                    float nx = float.Parse(parts[1], culture);
                    float ny = float.Parse(parts[2], culture);
                    float nz = float.Parse(parts[3], culture);
                    normals.Add(new Vector3(nx, ny, nz));
                    break;

                case "vt":
                    // Texture coordinate
                    float u = float.Parse(parts[1], culture);
                    float v = float.Parse(parts[2], culture);
                    uvs.Add(new Vector2(u, v));
                    break;

                case "f":
                    // Face indices
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] indicesParts = parts[i].Split('/');
                        int vertexIndex = int.Parse(indicesParts[0], culture) - 1;
                        indices.Add(vertexIndex);
                    }
                    break;
            }
        }

        // Build the mesh
        Mesh mesh = new Mesh();
        mesh.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.SetVertices(vertices);

        if (colors.Count == vertices.Count)
            mesh.SetColors(colors);

        if (uvs.Count > 0)
            mesh.SetUVs(0, uvs);

        if (normals.Count == vertices.Count)
            mesh.SetNormals(normals);
        else
            mesh.RecalculateNormals();

        mesh.SetTriangles(indices, 0);
        mesh.RecalculateBounds();

        return mesh;
    }
}
