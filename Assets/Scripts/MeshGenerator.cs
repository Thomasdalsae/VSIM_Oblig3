using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    
    public int xCellCount = 5;  // Number of quads in the x direction
    public int zCellCount = 5;  // Number of quads in the z direction

    public TextAsset textAsset;
    public int gridSize = 5;  // Resolution in meters

   
void Start()
{
    mesh = new Mesh();
    GetComponent<MeshFilter>().mesh = mesh;
    GetCoordFromFile(textAsset);
    UpdateMesh();
}


    void GetCoordFromFile(TextAsset textAsset)
    {
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
    
            // Calculate the bounding rectangle based on the data
            Vector2 bottomLeft = new Vector2(minX, minY);
            Vector2 topRight = new Vector2(maxX, maxY);
    
            // Determine the size of the grid cells
            float cellSize = gridSize;
    
            // Calculate the number of grid cells in x and z directions
            int xCellCount = Mathf.CeilToInt((topRight.x - bottomLeft.x) / cellSize);
            int zCellCount = Mathf.CeilToInt((topRight.y - bottomLeft.y) / cellSize);
    
            // Calculate the size of the rectangle based on data and desired scale
            float rectangleSizeX = (topRight.x - bottomLeft.x) / xCellCount;
            float rectangleSizeZ = (topRight.y - bottomLeft.y) / zCellCount;
    
            // Set the desired visual scale (e.g., 1 meter in data corresponds to 1 unit in the mesh)
            float desiredVisualScale = 1.0f;
    
            // Calculate the final rectangle size
            float rectangleSize = Mathf.Max(rectangleSizeX, rectangleSizeZ) * desiredVisualScale;
    
            // Use this rectangle size for creating the mesh
            CreateShape(rectangleSize);
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading file: " + e.Message);
        }
    }

void CreateShape(float rectangleSize)
{
    // Calculate the size of each grid cell based on xCellCount and zCellCount
    float cellSizeX = rectangleSize / xCellCount;
    float cellSizeZ = rectangleSize / zCellCount;

    // Calculate the total number of vertices and triangles
    int vertexCount = (xCellCount + 1) * (zCellCount + 1);
    int triangleCount = xCellCount * zCellCount * 2 * 3;  // 2 triangles per quad, 3 vertices per triangle

    vertices = new Vector3[vertexCount];
    triangles = new int[triangleCount];

    int vert = 0;
    int tris = 0;

    for (int z = 0; z <= zCellCount; z++)
    {
        for (int x = 0; x <= xCellCount; x++)
        {
            float xPos = x * cellSizeX;
            float zPos = z * cellSizeZ;
            vertices[vert] = new Vector3(xPos, 0, zPos);

            // Define the triangles based on the current vertex and grid structure
            if (x < xCellCount && z < zCellCount)
            {
                int topLeft = vert;
                int topRight = vert + 1;
                int bottomLeft = vert + xCellCount + 1;
                int bottomRight = vert + xCellCount + 2;

                triangles[tris + 0] = topLeft;
                triangles[tris + 1] = bottomLeft;
                triangles[tris + 2] = topRight;
                triangles[tris + 3] = topRight;
                triangles[tris + 4] = bottomLeft;
                triangles[tris + 5] = bottomRight;

                tris += 6;
            }

            vert++;
        }
    }
}


    void UpdateMesh()
    {
        // This code remains the same as your existing UpdateMesh function.
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
