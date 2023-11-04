using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TextAsset))]
public class PCGenerator : MonoBehaviour
{
    public TextAsset pointData;
    [SerializeField] public GameObject pointPrefab;

    void Start()
    {
        if (pointData != null)
        {
            string[] lines = pointData.text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] values = line.Split(' ');

                if (values.Length >= 3)
                {
                    float x = float.Parse(values[0]);
                    float y = float.Parse(values[1]);
                    float z = float.Parse(values[2]);
<<<<<<< Updated upstream
                    Vector3 position = new Vector3(x * 5, y * 5, z * 5); // times 5 to make it easier to see
                    Instantiate(pointPrefab, position, Quaternion.identity); // Make a sphere for each location
=======
                    Vector3 position = new Vector3(x, z, y);

                    // Ensure the pointPrefab has a Renderer component
                    Renderer prefabRenderer = pointPrefab.GetComponent<Renderer>();

                    if (prefabRenderer != null)
                    {
                        // Enable GPU instancing on the sharedMaterial of the prefab's renderer
                        Material pointMaterial = prefabRenderer.sharedMaterial;
                        pointMaterial.enableInstancing = true;

                        Instantiate(pointPrefab, position, Quaternion.identity);
                    }
                    else
                    {
                        Debug.LogError("pointPrefab is missing a Renderer component.");
                    }
>>>>>>> Stashed changes
                }
            }
        }
    }
}
