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

   
void CalculateAverageHeightForSquares(List<Vector3> Coords)
{
    for (int z = 0; z < zGridCellCount; z++)
    {
        for (int x = 0; x < xGridCellCount; x++)
        {
            // Define the boundaries of the current square
            float minX = x * (visualScale * gridSize);
            float maxX = Mathf.Min((x + 1) * (visualScale * gridSize), (xGridCellCount * visualScale * gridSize));
            float minZ = z * (visualScale * gridSize);
            float maxZ = Mathf.Min((z + 1) * (visualScale * gridSize), (zGridCellCount * visualScale * gridSize));

            float totalHeight = 0.0f;
            int pointCount = 0;

            // Calculate the average height of data points inside the square
            for (int i = 0; i < Coords.Count; i++)
            {
                Vector3 point = Coords[i];

                if (point.x >= minX && point.x < maxX && point.z >= minZ && point.z < maxZ)
                {
                    totalHeight += point.y;
                    pointCount++;
                }
            }

            if (pointCount > 0)
            {
                float averageHeight = totalHeight / pointCount;
                float xPos = (x * visualScale * gridSize) + (visualScale * gridSize * 0.5f);
                float zPos = (z * visualScale * gridSize) + (visualScale * gridSize * 0.5f);

                // Instantiate a cube at the center of the square with the average height
                Vector3 cubePosition = new Vector3(xPos, averageHeight, zPos);
                //InstantiateCube(cubePosition);
            }
        }
    }
}


void GetCoordFromFile(TextAsset textAsset)
{
    try
    {
        string[] lines = textAsset.text.Split('\n');
        List<Vector3> Coords = new List<Vector3>();
        List<Vector3> CenterPoints = new List<Vector3>();

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

        // Calculate the bounding rectangle based on the data
        Vector2 bottomLeft = new Vector2(minX, minZ);
        Vector2 topRight = new Vector2(maxX, maxZ);

        // Calculate the number of grid cells in x and z directions
        int xCellCount = Mathf.CeilToInt((topRight.x - bottomLeft.x) / gridSize);
        int zCellCount = Mathf.CeilToInt((topRight.y - bottomLeft.y) / gridSize);

        // Calculate the size of the rectangle based on data and desired scale
        float rectangleSizeX = (topRight.x - bottomLeft.x) / xCellCount;
        float rectangleSizeZ = (topRight.y - bottomLeft.y) / zCellCount;

        // Calculate the final rectangle size
        float rectangleSize = Mathf.Max(rectangleSizeX, rectangleSizeZ) * visualScale;

        CalculateAverageHeightForSquares(Coords);
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
    // Calculate cell sizes based on the provided rectangleSize and visualScale
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
            // Calculate the position of each vertex based on the adjusted cell sizes
            float xPos = x * cellSizeX - (rectangleSize / 2); // Adjust the x position
            float zPos = z * cellSizeZ - (rectangleSize / 2); // Adjust the z position

            // Calculate the center position of the square
            float centerX = xPos + (cellSizeX / 2);
            float centerZ = zPos + (cellSizeZ / 2);

            // You can calculate the average height for this square based on your data
            // For simplicity, I'll assume a constant height of 0.5 for the center
            float averageHeight = 0f;

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

                // Instantiate a cube at the center of the square
                InstantiateCube(new Vector3(centerX, 0, centerZ));
               // InstantiateRedCube(new Vector3(centerX, averageHeight, centerZ)); // Instantiate a cube at the center
                InstantiateRedCube(new Vector3(xPos, 0, zPos)); // Instantiate a cube at the corner
                InstantiateRedCube(new Vector3(xPos + cellSizeX, 0, zPos)); // Instantiate a cube at the corner
                InstantiateRedCube(new Vector3(xPos, 0, zPos + cellSizeZ)); // Instantiate a cube at the corner
                InstantiateRedCube(new Vector3(xPos + cellSizeX, 0, zPos + cellSizeZ)); // Instantiate a cube at the corner

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



   
   void InstantiateCube(Vector3 position)
   {
       if (cubePrefab != null)
       {
           GameObject cube = Instantiate(cubePrefab, position, Quaternion.identity);
           // You can further configure the instantiated cube, e.g., scale or material
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
