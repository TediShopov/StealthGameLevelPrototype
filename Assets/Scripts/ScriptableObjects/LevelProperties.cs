using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelProperties", menuName = "ScriptableObjects/LevelProperties", order = 1)]
public class LevelProperties : ScriptableObject
{
    public Vector2 LevelSize = new Vector2(15, 15);
    public Vector2 RelativeStartPosition = new Vector2(0.1f, 0.1f);
    public Vector2 RelativeEndPosiiton = new Vector2(0.9f, 0.9f);

    //Prefabs used for building the level
    public GameObject PlayerPrefab;

    public GameObject DestinationPrefab;
    public GameObject EnemyPrefab;
    public GameObject BoundaryViualPrefab;
    public GameObject LevelInitializer;
    public LayerMask ObstacleLayerMask;
    public List<GameObject> ObstaclePrefabs;
}