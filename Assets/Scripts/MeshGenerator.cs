using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;
using System.IO;
using System;
using File = System.IO.File;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    
    //Testing
    private Mesh mesh;
    
    private Vector3[] vertices;
    private int[] triangles;
    public TextAsset textAsset;

    public int xSize = 20;
    public int zSize = 20;

    public float rectangleSize = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
       GetCoordFromFile(textAsset);
        CreateShape();
        UpdateMesh();
    }

    void GetCoordFromFile(TextAsset path)
    {   //This will always be one since i have normalized the values of the txt file.
        try
        {
            string[] lines = textAsset.text.Split('\n');

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (var line in lines)
            {
                string[] parts = line.Split(' ');
                if (parts.Length >= 2)
                {
                    if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                    {
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            float width = maxX - minX;
            float height = maxX - minY;
            rectangleSize = Mathf.Min(width, height) / Mathf.Max(xSize, zSize);

            Vector2 bottomLeft = new Vector2(minX, minY);
            Vector2 bottomRight = new Vector2(maxX, minY);
            Vector2 topLeft = new Vector2(minX, maxY);
            Vector2 topRight = new Vector2(maxX, maxY);

            Debug.Log("Bottom left: " + bottomLeft);
            Debug.Log("Bottom right: " + bottomRight);
            Debug.Log("Top left: " + topLeft);
            Debug.Log("Top Right: " + topRight);
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + e.Message);
        }
    }

    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        
        for (int i =0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                vertices[i] = new Vector3(x * rectangleSize , 0, z * rectangleSize );
                i++;
            }
        }    

        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {

                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;
                vert++;
                tris += 6;
            }

            vert++;
        }

    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        mesh.RecalculateNormals();
    }

}
