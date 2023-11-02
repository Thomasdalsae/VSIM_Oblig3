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
        if (pointData != null) // checking if there is a TextAsset
        {
            string[] lines = pointData.text.Split('\n'); // Split on each new line;
            
            for (int i = 1; i < lines.Length; i++) 
            {
                string line = lines[i];
                string[] values = line.Split(' '); // Split on each space

                if (values.Length >= 3)
                {
                    //Getting x y z value from txt
                    float x = float.Parse(values[0]);
                    float y = float.Parse(values[1]);
                    float z = float.Parse(values[2]);
                    Vector3 position = new Vector3(x , y, z); // times 5 to make it easier to see
                    Instantiate(pointPrefab, position, Quaternion.identity); // Make a sphere for each location
                }
            }
        }
    }
}
