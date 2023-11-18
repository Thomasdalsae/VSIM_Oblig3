using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(GameObject))] // Needs to have raindrop prefab
public class RainGenerator : MonoBehaviour
{
    #region Variables

    [SerializeField] private int _Xsize;
    [SerializeField] private int _Zsize;
    [SerializeField] private float _raindropSize;
    [SerializeField] private int AmountOfRainDrops;
    [SerializeField] private GameObject _raindropPrefab;
    [SerializeField] private Vector3 startLocation;

    #endregion

    private void Start()
    {
        startLocation = transform.localPosition;
        GenerateRain();

    }

    private void GenerateRain()
    {
        StartCoroutine(SpawnRain());
    }

    private IEnumerator SpawnRain()
    {
        for (int i = 0; i < AmountOfRainDrops; i++)
        {
            Vector3 startLocation = new Vector3(
                UnityEngine.Random.Range(-_Xsize, _Xsize),
                this.startLocation.y,
                UnityEngine.Random.Range(-_Zsize, _Zsize)
            );

            Instantiate(_raindropPrefab, startLocation, Quaternion.identity);

            // Generate a random delay between 0.1 to 1 second for the next raindrop
            float delay = UnityEngine.Random.Range(0.1f, 1.0f);
            yield return new WaitForSeconds(delay);
        }
    }
}
