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

    public int xGridCellCount = 5;  // Number of grid cells in the x direction
    public int zGridCellCount = 5;  // Number of grid cells in the z direction

    public TextAsset textAsset;
    public int gridSize = 5;  // Resolution in meters

    // Desired visual scale
    public float visualScale = 1.0f;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        if (textAsset != null)
        {
            GetCoordFromFile(textAsset);
            UpdateMesh();
        }
        else
        {
            Debug.LogError("No TextAsset provided.");
        }
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

            // Calculate the final rectangle size
            float rectangleSize = Mathf.Max(rectangleSizeX, rectangleSizeZ) * visualScale;

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
        float cellSizeX = rectangleSize / xGridCellCount;
        float cellSizeZ = rectangleSize / zGridCellCount;

        int vertexCount = (xGridCellCount + 1) * (zGridCellCount + 1);
        int triangleCount = xGridCellCount * zGridCellCount * 2 * 3;

        vertices = new Vector3[vertexCount];
        triangles = new int[triangleCount];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z <= zGridCellCount; z++)
        {
            for (int x = 0; x <= xGridCellCount; x++)
            {
                float xPos = x * cellSizeX;
                float zPos = z * cellSizeZ;
                vertices[vert] = new Vector3(xPos, 0, zPos);

                if (x < xGridCellCount && z < zGridCellCount)
                {
                    int topLeft = vert;
                    int topRight = vert + 1;
                    int bottomLeft = vert + xGridCellCount + 1;
                    int bottomRight = vert + xGridCellCount + 2;

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
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
