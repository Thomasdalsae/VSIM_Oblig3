using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{

    //Testing
    private Mesh mesh;

    private Vector3[] vertices;
    private int[] triangles;

    public float cubeSize = 0.005f;
    public GameObject cubePrefab;
    public GameObject centerCubePrefab;
    public int xGridCellCount = 5; // Number of grid cells in the x direction
    public int zGridCellCount = 5; // Number of grid cells in the z direction
    public TextAsset textAsset;
    public int gridSize = 5; // Resolution in meters
    public float visualScale = 1.0f;

    public int xSize = 20;

    public int zSize = 20;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        if (textAsset != null)
        {
            GetCoordsFromFile(textAsset);
        }
        else
        {
            Debug.LogError("No TextAsset provided.");
        }
    }





   void GetCoordsFromFile(TextAsset textAsset)
    {
        string[] lines = textAsset.text.Split('\n');
        List<Vector3> coords = new List<Vector3>();

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
                    minX = Mathf.Min(minX, x);
                    minZ = Mathf.Min(minZ, z);
                    maxX = Mathf.Max(maxX, x);
                    maxZ = Mathf.Max(maxZ, z);
                    coords.Add(new Vector3(x, y, z));
                }
            }
        }

        xGridCellCount = Mathf.CeilToInt((maxX - minX) / gridSize);
        zGridCellCount = Mathf.CeilToInt((maxZ - minZ) / gridSize);

        CreateMesh(coords, minX, minZ);
    }

       
void CreateMesh(List<Vector3> coords, float minX, float minZ)
    {
        float cellSizeX = (coords.Max(v => v.x) - minX) / xGridCellCount;
        float cellSizeZ = (coords.Max(v => v.z) - minZ) / zGridCellCount;

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
                float xPos = x * cellSizeX + minX;
                float zPos = z * cellSizeZ + minZ;
                float centerX = xPos + (cellSizeX / 2);
                float centerZ = zPos + (cellSizeZ / 2);
                float totalHeight = 0f;
                int pointCount = 0;

                foreach (Vector3 point in coords)
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

                    tris += 6;
                }

                vert++;
            }
        }

    InstantiateCubes(cellSizeX, cellSizeZ, minX, minZ);
        UpdateMesh();
    }

 void InstantiateCubes(float cellSizeX, float cellSizeZ, float minX, float minZ)
{
    if (cubePrefab != null && centerCubePrefab != null)
    {
        for (int z = 0; z <= zGridCellCount; z++)
        {
            for (int x = 0; x <= xGridCellCount; x++)
            {
                float xPos = x * cellSizeX + minX;
                float zPos = z * cellSizeZ + minZ;
                float centerX = xPos + (cellSizeX / 2);
                float centerZ = zPos + (cellSizeZ / 2);

                // Instantiate blue cube at the center of the square
                InstantiateCube(centerCubePrefab, new Vector3(centerX, 0, centerZ), true);

                // Instantiate red cubes at the corners of the square
                InstantiateCube(cubePrefab, new Vector3(xPos, 0, zPos));
                InstantiateCube(cubePrefab, new Vector3(xPos + cellSizeX, 0, zPos));
                InstantiateCube(cubePrefab, new Vector3(xPos, 0, zPos + cellSizeZ));
                InstantiateCube(cubePrefab, new Vector3(xPos + cellSizeX, 0, zPos + cellSizeZ));
            }
        }
    }
    else
    {
        Debug.LogError("Cube prefabs are not assigned.");
    }
}

void InstantiateCube(GameObject prefab, Vector3 position, bool isCenterCube = false)
{
    GameObject cube = Instantiate(prefab, position, Quaternion.identity);

    if (isCenterCube)
    {
        cube.transform.localScale = new Vector3(cubeSize * 1.5f, cubeSize * 1.5f, cubeSize * 1.5f); // Adjust the scale for the center cube
        cube.GetComponent<Renderer>().material.color = Color.blue; // Set the center cube color to blue
    }
    else
    {
        cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
        cube.GetComponent<Renderer>().material.color = Color.red; // Set the corner cube color to red
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
