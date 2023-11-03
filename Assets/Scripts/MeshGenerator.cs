using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    public float cubeSize = 0.005f;
    public GameObject cubePrefab;

    public int xGridCellCount = 5; // Number of grid cells in the x direction
    public int zGridCellCount = 5; // Number of grid cells in the z direction

    public TextAsset textAsset;
    public int gridSize = 5; // Resolution in meters

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
        List<Vector3> Coords = new List<Vector3>();

        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;

        foreach (var line in lines)
        {
            string[] parts = line.Split(' ');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y) &&
                    float.TryParse(parts[2], out float z))
                {
                    // Update min and max values and add the vertex to Coords
                    minX = Mathf.Min(minX, x);
                    minZ = Mathf.Min(minZ, z);
                    maxX = Mathf.Max(maxX, x);
                    maxZ = Mathf.Max(maxZ, z);
                    Vector3 temp = new Vector3(x, y, z);
                    Coords.Add(temp);
                }
            }
        }

        // Calculate the number of grid cells in x and z directions
        xGridCellCount = Mathf.CeilToInt((maxX - minX) / gridSize);
        zGridCellCount = Mathf.CeilToInt((maxZ - minZ) / gridSize);

        // Calculate the size of the rectangle based on data and desired scale
        float rectangleSizeX = (maxX - minX) / xGridCellCount;
        float rectangleSizeZ = (maxZ - minZ) / zGridCellCount;

        // Calculate the final rectangle size
        float rectangleSize = Mathf.Max(rectangleSizeX, rectangleSizeZ) * visualScale;

        // Use this rectangle size for creating the mesh
        CreateShape(Coords);
    }
    catch (Exception e)
    {
        Debug.LogError("Error reading file: " + e.Message);
    }
}


void CreateShape(List<Vector3> Coords)
{
    // Calculate cell sizes based on the provided xGridCellCount and zGridCellCount
    float cellSizeX = (Coords.Max(v => v.x) - Coords.Min(v => v.x)) / xGridCellCount;
    float cellSizeZ = (Coords.Max(v => v.z) - Coords.Min(v => v.z)) / zGridCellCount;

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
            // Calculate the position of each vertex based on the adjusted cell sizes
            float xPos = x * cellSizeX + Coords.Min(v => v.x);
            float zPos = z * cellSizeZ + Coords.Min(v => v.z);

            // Calculate the center position of the square
            float centerX = xPos + (cellSizeX / 2);
            float centerZ = zPos + (cellSizeZ / 2);

            // Calculate the average height for this square based on your data
            float totalHeight = 0f;
            int pointCount = 0;

            // Iterate through data points once and only consider those inside the current square
            foreach (Vector3 point in Coords)
            {
                if (point.x >= xPos && point.x < (xPos + cellSizeX) &&
                    point.z >= zPos && point.z < (zPos + cellSizeZ))
                {
                    totalHeight += point.y;
                    pointCount++;
                }
            }

            float averageHeight = (pointCount > 0) ? totalHeight / pointCount : 0f;

            vertices[vert] = new Vector3(centerX, averageHeight, centerZ);

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

                // Place the blue cube at the center of the square with the average height
                InstantiateBlueCube(new Vector3(centerX, averageHeight, centerZ));

                // Instantiate red cubes at the corners of the square
                InstantiateRedCube(new Vector3(xPos, 0, zPos)); // Bottom-left corner
                InstantiateRedCube(new Vector3(xPos + cellSizeX, 0, zPos)); // Bottom-right corner
                InstantiateRedCube(new Vector3(xPos, 0, zPos + cellSizeZ)); // Top-left corner
                InstantiateRedCube(new Vector3(xPos + cellSizeX, 0, zPos + cellSizeZ)); // Top-right corner

                tris += 6;
            }

            vert++;
        }
    }

    UpdateMesh();
}












void InstantiateRedCube(Vector3 position)
{
    // You can assign a red cube prefab to the cubePrefab field
    if (cubePrefab != null)
    {
        GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = Color.red; // Set the cube color to red
    }
    else
    {
        Debug.LogError("Cube prefab is not assigned.");
    }
}



   
  
void InstantiateBlueCube(Vector3 position)
{
    if (cubePrefab != null)
    {
        GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
        cube.GetComponent<Renderer>().material.color = Color.blue; // Set the cube color to blue
    }
    else
    {
        Debug.LogError("Cube prefab is not assigned.");
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
