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
        // henter data fra pointData filen pg lager en punktsky med prefaben pointPrefab;
        // tar å plasserer punktene i verdenen basert på x, y, z koordinatene i pointData filen
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


                    Vector3 position = new Vector3(x, z, y);

                    // garanterer at pointPrefab har en renderer komponent  
                    Renderer prefabRenderer = pointPrefab.GetComponent<Renderer>();

                    if (prefabRenderer != null)
                    {
                        Material pointMaterial = prefabRenderer.sharedMaterial;
                        pointMaterial.enableInstancing = true;

                        Instantiate(pointPrefab, position, Quaternion.identity);
                    }
                    else
                    {
                        Debug.LogError("pointPrefab is missing a Renderer component.");
                    }
                }
            }
        }
    }
}
