using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GameObject))] // Needs to have raindrop prefab

public class BallSpawner : MonoBehaviour
{
    [SerializeField] GameObject ballPrefab;
    [SerializeField] private Vector3 spawnLocation;


    private void Start()
    {
        spawnLocation = transform.localPosition;
        GenerateBall(); 
    }
    
    private void GenerateBall()
    {
        Instantiate(ballPrefab, spawnLocation, Quaternion.identity);
    }
}
