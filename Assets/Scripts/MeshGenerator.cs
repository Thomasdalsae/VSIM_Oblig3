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
    public TextAsset textAsset;

    public int xSize = 20;
    public int zSize = 20;
    public float rectangleSize = 1.0f;

    // Create a flag to control Gizmo drawing
    private bool shouldDrawGizmos = false;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        // Initialize vertices and triangles arrays
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        triangles = new int[xSize * zSize * 6];

        GetCoordFromFile(textAsset);
        CreateShape();
        UpdateMesh();
    }

    void GetCoordFromFile(TextAsset path)
    {
        try
        {
            string[] lines = textAsset.text.Split('\n');

            // Initialize these variables to sensible defaults
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

            // Calculate width and height
            float width = maxX - minX;
            float height = maxY - minY;
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

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                vertices[i] = new Vector3(x * rectangleSize, 0, z * rectangleSize);
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

    // This method calculates the average heights
    void CalculateAverageHeights()
    {
        if (vertices == null || xSize < 1 || zSize < 1)
        {
            return;
        }
    
        int squareCount = xSize * zSize;
        float[] averageHeights = new float[squareCount];
    
        for (int i = 0; i < squareCount; i++)
        {
            int xStart = i % xSize;
            int zStart = i / xSize;
    
            float totalHeight = 0;
            int pointCount = 0;
    
            for (int z = zStart; z < zStart + 1 && z < zSize; z++)
            {
                for (int x = xStart; x < xStart + 1 && x < xSize; x++)
                {
                    int dataPointIndex = x + z * (xSize + 1);
    
                    if (dataPointIndex >= 0 && dataPointIndex < vertices.Length)
                    {
                        totalHeight += vertices[dataPointIndex].y;
                        pointCount++;
                    }
                }
            }
    
            averageHeights[i] = pointCount > 0 ? totalHeight / pointCount : 0;
        }
      
        // Draw Gizmos when the flag is set
        if (shouldDrawGizmos)
        {
            DrawGizmos(averageHeights);
        }
    }

    // This method is called in the Unity Editor when the scene view is being drawn
    void OnDrawGizmos()
    {
        // Set the flag to true to enable Gizmo drawing
        shouldDrawGizmos = true;
        CalculateAverageHeights();
        // Reset the flag to false to avoid Gizmo drawing during play mode
        shouldDrawGizmos = false;
    }

    void DrawGizmos(float[] averageHeights)
        {
            for (int i = 0; i < zSize; i++)
            {
                for (int j = 0; j < xSize; j++)
                {
                    int squareIndex = j + i * xSize;
                    Vector3 center = new Vector3(j * rectangleSize + rectangleSize / 2, averageHeights[squareIndex], i * rectangleSize + rectangleSize / 2);
                    Gizmos.DrawSphere(center, 0.005f);
                }
            }
        }
}