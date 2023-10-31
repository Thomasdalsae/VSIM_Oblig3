using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCGenerator : MonoBehaviour
{
    public TextAsset pointData;
    [SerializeField] public GameObject pointPrefab;

    void Start()
    {
        if (pointData != null)
        {
            string[] lines = pointData.text.Split('\n');
            
            for (int i = 1; i < lines.Length; i++) // Start from the second line (index 1)
            {
                string line = lines[i];
                string[] values = line.Split(' ');

                if (values.Length >= 3)
                {
                    float x = float.Parse(values[0]);
                    float y = float.Parse(values[1]);
                    float z = float.Parse(values[2]);
                    Vector3 position = new Vector3(x * 5, y * 5, z * 5);
                    Instantiate(pointPrefab, position, Quaternion.identity);
                }
            }
        }
    }
}
