using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using System;

//Class used to proved utility functions for generating
//standartizes level structure
public class LevelGeneratorBase : MonoBehaviour
{
    //Prefabs used for building the level
    public GameObject PlayerPrefab;

    public GameObject DestinationPrefab;
    public GameObject EnemyPrefab;
    public GameObject BoundaryViualPrefab;
    public GameObject LevelInitializer;
    public LayerMask ObstacleLayerMask;
    public List<GameObject> ObstaclePrefabs;

    public int MinEnemiesSpawned = 1;
    public int MaxEnemiesSpawned = 3;
    public int ObstaclesSpawned;

    [Range(0.0f, 1.0f)]
    public float MinObjectScale = 0.2f;

    [Range(1.0f, 5.0f)]
    public float MaxObjectScale = 1.0f;

    //Size modificaiton
    [Range(10, 100)]
    public float MaxDimension = 50.0f;

    [Range(10, 50)]
    public float MinDimension = 50.0f;

    //Randomizer specific for the level
    public System.Random LevelRandom;

    protected GameObject Boundary;
    public float VisualBoundWidth;
    protected GameObject CompositeVisualBoundary;

    private Vector2 GetRandomPositionInsideCollider(BoxCollider2D spawnArea)
    {
        Vector2 colliderSize = spawnArea.size;
        Vector2 colliderCenter = spawnArea.bounds.center;
        float randomX = Helpers.GetRandomFloat(LevelRandom, colliderCenter.x - colliderSize.x / 2f, colliderCenter.x + colliderSize.x / 2f);
        float randomY = Helpers.GetRandomFloat(LevelRandom, colliderCenter.y - colliderSize.y / 2f, colliderCenter.y + colliderSize.y / 2f);
        return new Vector2(randomX, randomY);
    }

    public GameObject SpawnPrefabWithoutCollision(GameObject prefab, BoxCollider2D spawnArea, int tries)
    {
        for (int i = 0; i < tries; i++)
        {
            Vector2 randomPosition = GetRandomPositionInsideCollider(spawnArea);
            // Instantiate the prefab at the random position
            GameObject instantiatedPrefab = Instantiate(prefab, randomPosition, Quaternion.identity);
            instantiatedPrefab.transform.SetParent(this.transform, true);
            // Check for collisions with obstacles on the specified layer
            if (CheckCollisionWithObstacles(instantiatedPrefab))
            {
                Destroy(instantiatedPrefab);
            }
            else
            {
                return instantiatedPrefab;
            }
        }
        return null;
    }

    private bool CheckCollisionWithObstacles(GameObject spawnedObject)
    {
        // Check if the spawned object collides with any obstacles on the specified layer
        Collider2D[] colliders = Physics2D.OverlapBoxAll(spawnedObject.transform.position, spawnedObject.GetComponent<Collider2D>().bounds.size, 0f, ObstacleLayerMask);
        // If there are any colliders in the array, there is a collision
        return colliders.Length > 1;
    }

    public BoxCollider2D InitLevelBoundary(float length, float width)
    {
        Boundary = new GameObject("Boundary", new System.Type[] { typeof(BoxCollider2D) });
        Boundary.transform.SetParent(this.transform);
        Boundary.transform.localPosition = new Vector3(0, 0, 0);
        Boundary.layer = LayerMask.NameToLayer("Boundary");
        var boxCollider = Boundary.GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        //Pick random sizes
        //boxCollider.size = new Vector2(Random.Range(MinDimension, MaxDimension), Random.Range(MinDimension, MaxDimension));
        boxCollider.size = new Vector2(length, width);
        return boxCollider;
    }

    //Place viusal components and a composity collider for them
    public GameObject PlaceBoundaryVisualPrefabs(BoxCollider2D collider2D)
    {
        CompositeVisualBoundary = new GameObject("CompositeViualBoundary", new System.Type[] { typeof(CompositeCollider2D) });
        CompositeVisualBoundary.layer = LayerMask.NameToLayer("Obstacle");
        CompositeVisualBoundary.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        float halfHeight = collider2D.size.y / 2.0f;
        float halfWidth = collider2D.size.x / 2.0f;

        PlaceHorizontal(halfHeight, collider2D.size.x);
        PlaceHorizontal(-halfHeight, collider2D.size.x);
        PlaceVertical(-halfWidth, collider2D.size.y);
        PlaceVertical(halfWidth, collider2D.size.y);
        return CompositeVisualBoundary;
    }

    private void PlaceVertical(float x, float length)
    {
        var leftSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        leftSide.transform.position = new Vector3(x, 0, 0);
        leftSide.transform.localScale = new Vector3(VisualBoundWidth, length, 0);
    }

    private void PlaceHorizontal(float y, float length)
    {
        var topSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        topSide.transform.position = new Vector3(0, y, 0);
        topSide.transform.localScale = new Vector3(length, VisualBoundWidth, 0);
    }
}

public class SpawnRandomStealthLevel : LevelGeneratorBase
{
    public int RandomSeed;

    // Start is called before the first frame update
    private void Start()
    {
        //Assign this object to be root object of level by assignign tag
        this.tag = "Level";
        this.LevelRandom = new System.Random(RandomSeed);
        BoxCollider2D box = InitLevelBoundary(
            Helpers.GetRandomFloat(LevelRandom, MinDimension, MaxDimension)
            , Helpers.GetRandomFloat(LevelRandom, MinDimension, MaxDimension));
        PlaceBoundaryVisualPrefabs(box);

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(this.transform);
        Obstacles.transform.localPosition = new Vector3(0, 0, 0);
        CompositeVisualBoundary.transform.SetParent(Obstacles.transform, false);
        SpawnRandomObstacles(box, Obstacles);
        var playerInstance = SpawnPrefabWithoutCollision(PlayerPrefab, box, 150);
        var destinationIntance = SpawnPrefabWithoutCollision(DestinationPrefab, box, 150);
        //int enemiesToSpaw = Random.Range(MinEnemiesSpawned, MaxEnemiesSpawned);
        int enemiesToSpaw = LevelRandom.Next(MinEnemiesSpawned, MaxEnemiesSpawned + 1);
        for (int i = 0; i < enemiesToSpaw; i++)
        {
            SpawnPrefabWithoutCollision(EnemyPrefab, box, 25);
        }
        //SetupRRT(playerInstance.GetComponent<CharacterController2D>(), destinationIntance);
        Instantiate(LevelInitializer, this.transform, false);

        //Triggers scripts

        var levelInitializer = gameObject.GetComponentInChildren<InitializeStealthLevel>();
        var voxelizedLevel = gameObject.GetComponentInChildren<VoxelizedLevel>();
        var multipleRRTSolvers = gameObject.GetComponentInChildren<MultipleRRTRunner>();
        levelInitializer.Init();
        voxelizedLevel.Init();
        multipleRRTSolvers.Run();

        Debug.Log("Random Level Initialziation Finished");
    }

    private void SpawnRandomObstacles(BoxCollider2D box, GameObject Obstacles)
    {
        for (int i = 0; i < ObstaclesSpawned; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                GameObject randomPrefab = GetRandomPrefab();
                var obstacle = SpawnPrefabWithoutCollision(randomPrefab, box, 5);
                if (obstacle != null)
                {
                    //Samples position is already inside box collider, keep its values
                    obstacle.transform.SetParent(Obstacles.transform, true);
                    break;
                }
            }
        }
    }

    private GameObject GetRandomPrefab()
    {
        if (ObstaclePrefabs.Count == 0)
        {
            Debug.LogError("Object list is empty. Please assign GameObjects to the ObstaclePrefabs array.");
            return null;
        }
        GameObject randomPrefab = ObstaclePrefabs[LevelRandom.Next(0, ObstaclePrefabs.Count)];
        float scaleRandom = Helpers.GetRandomFloat(LevelRandom, MinObjectScale, MaxObjectScale);
        randomPrefab.transform.localScale = new Vector3(scaleRandom, scaleRandom, 0); ;

        int randomRotationIndex = LevelRandom.Next(0, 8);
        float angle = Mathf.Lerp(0f, 90f, randomRotationIndex / 7f); // 7 because there are 8 positions
        randomPrefab.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        return randomPrefab;
    }
}