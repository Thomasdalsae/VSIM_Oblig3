using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCGenerator : MonoBehaviour
{

    public TextAsset pointData;
    [SerializeField]public GameObject pointPrefab;
    // Start is called before the first frame update
    void Start()
    {
        if (pointData != null)
        {
            string[] lines = pointData.text.Split('\n');


            foreach (string line in lines)
            {
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
