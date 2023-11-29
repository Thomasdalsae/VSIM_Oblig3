using System.Collections;
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
   // [SerializeField] private float minScale = 0.5f; // Minimum scale for raindrop
    //[SerializeField] private float maxScale = 1.5f; // Maximum scale for raindrop

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
            startLocation = new Vector3(
                UnityEngine.Random.Range(startLocation.x + -_Xsize, startLocation.x + _Xsize),
                this.startLocation.y,
                UnityEngine.Random.Range(startLocation.z + -_Zsize, startLocation.z + _Zsize)
            );

            GameObject newRaindrop = Instantiate(_raindropPrefab, startLocation, Quaternion.identity);

            // Generate a random scale for the raindrop
           // float randomScale = UnityEngine.Random.Range(minScale, maxScale);
            //newRaindrop.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

            // Generate a random delay between 0.1 to 1 second for the next raindrop
            float delay = UnityEngine.Random.Range(0.1f, 1.0f);
            yield return new WaitForSeconds(delay);
        }
    }
}
