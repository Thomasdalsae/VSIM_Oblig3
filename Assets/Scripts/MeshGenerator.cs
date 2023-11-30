using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    //Testing
    private Mesh mesh;

    public Vector3[] vertices;
    public int[] triangles;

    public float cubeSize = 0.005f;
    public GameObject cubePrefab;
    public GameObject centerCubePrefab;
    public int xGridCellCount = 5; //antall ruter i x retning
    public int zGridCellCount = 5; // antall ruter i z retning
    public TextAsset textAsset;
    public int gridSize = 5; // Resolution i meter  

    public bool SeeCubes = false;
    private Color[] colors;
    public Gradient gradient;


    private void Awake()
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
        // skaffer koordinater fra fil. 
        string[] lines = textAsset.text.Split('\n');
        List<Vector3> coords = new List<Vector3>();

        float minX = float.MaxValue;
        float minZ = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxZ = float.MinValue;
        float maxY = float.MinValue;

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
                    minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x);
                    maxZ = Mathf.Max(maxZ, z);
                    maxY = Mathf.Max(maxY, y);

                    coords.Add(new Vector3(x, z, y));
                }
            }
        }

        xGridCellCount = Mathf.CeilToInt((maxX - minX) / gridSize);
        zGridCellCount = Mathf.CeilToInt((maxZ - minZ) / gridSize);

        CreateMesh(coords, minX, minZ, minY, maxY);
    }


    void CreateMesh(List<Vector3> coords, float minX, float minZ, float minY, float maxY)
    {
        // lager mesh av koordinatene i coords listen
        float cellSizeX = (coords.Max(v => v.x) - minX) / xGridCellCount;
        float cellSizeZ = (coords.Max(v => v.z) - minZ) / zGridCellCount;
         
        // antall vertices og antall trekanter
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

        colors = new Color[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            float height = Mathf.InverseLerp(minY, maxY, vertices[i].y);
            colors[i] = gradient.Evaluate(height);
        }

        if (SeeCubes)
        {
            InstantiateCubes(cellSizeX, cellSizeZ, minX, minZ);
        }

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

                    // lager en blå kube i midten av hver rute

                    InstantiateCube(centerCubePrefab, new Vector3(centerX, 0, centerZ), true);

                    // lager en rød kube i hvert hjørne av hver rute
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

        cube.transform.SetParent(transform); // setter MeshGenerator som parent

        if (isCenterCube)
        {
            cube.transform.localScale =
                new Vector3(cubeSize * 1.5f, cubeSize * 1.5f, cubeSize * 1.5f); // justerer størrelsen på midta kuben
            cube.GetComponent<Renderer>().material.color = Color.blue; // skift farge på midta kuben til blå
        }
        else
        {
            cube.transform.localScale = new Vector3(cubeSize, cubeSize, cubeSize);
            cube.GetComponent<Renderer>().material.color = Color.red; // skift farge på hjørne kubene til rød
        }
    }


    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    public Vector3 BarycentricCoordinates(Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point)
    {
        // finner barysentriske koordinater for et punkt i forhold til et triangel
        Vector2 p12 = point2 - point1;
        Vector2 p13 = point3 - point1;
        Vector3 n = (Vector3.Cross(new Vector3(p12.x, 0.0f, p12.y), new Vector3(p13.x, 0.0f, p13.y)));
        float areal_123 = n.magnitude;
        Vector3 baryc;
        //u
        Vector2 p = point2 - point;
        Vector2 q = point3 - point;
        n = Vector3.Cross(new Vector3(p.x, 0.0f, p.y), new Vector3(q.x, 0.0f, q.y));
        baryc.x = n.y / areal_123;
        //v
        p = point3 - point;
        q = point1 - point;
        n = Vector3.Cross(new Vector3(p.x, 0.0f, p.y), new Vector3(q.x, 0.0f, q.y));
        baryc.y = n.y / areal_123;
        //w
        p = point1 - point;
        q = point2 - point;
        n = Vector3.Cross(new Vector3(p.x, 0.0f, p.y), new Vector3(q.x, 0.0f, q.y));
        baryc.z = n.y / areal_123;
        return baryc;
    }

    public float GetSurfaceHeight(Vector2 p)
    {
        //  finner høyden på terrenget i punktet p ved hjelp av barysentriske koordinater.
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var v0 = vertices[triangles[i]];
            var v1 = vertices[triangles[i + 1]];
            var v2 = vertices[triangles[i + 2]];

            Vector3 barcoords = BarycentricCoordinates(
                new Vector2(v0.x, v0.z),
                new Vector2(v1.x, v1.z),
                new Vector2(v2.x, v2.z),
                p);

            if (barcoords.x >= 0.0f && barcoords.y >= 0.0f && barcoords.z >= 0.0f &&
                (barcoords.x + barcoords.y + barcoords.z) <= 1.0f)
            {
                float height = barcoords.x * v0.y + barcoords.y * v1.y + barcoords.z * v2.y;
                return height;
            }
        }

        return 0.0f; //  vist pungt ikke finnes returner 0
    }
}