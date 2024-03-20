using System.Collections.Generic;
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
    public LevelProperties LevelProperties;

    public int MinEnemiesSpawned = 1;
    public int MaxEnemiesSpawned = 3;

    [Range(0.0f, 1.0f)]
    public float MinObjectScale = 0.2f;

    [Range(1.0f, 5.0f)]
    public float MaxObjectScale = 1.0f;

    //Randomizer specific for the level
    public System.Random LevelRandom;

    protected GameObject Boundary;
    public float VisualBoundWidth;
    protected GameObject CompositeVisualBoundary;

    private Vector2 GetRandomPositionInsideCollider(BoxCollider2D spawnArea)
    {
        float relRandomX = Helpers.GetRandomFloat(LevelRandom, 0f, 1f);
        float relRandomY = Helpers.GetRandomFloat(LevelRandom, 0f, 1f);
        float randomX = Mathf.Lerp(spawnArea.bounds.min.x, spawnArea.bounds.max.x, relRandomX);
        float randomY = Mathf.Lerp(spawnArea.bounds.min.y, spawnArea.bounds.max.y, relRandomY);
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

    public BoxCollider2D InitLevelBoundary(float length, float width, GameObject to)
    {
        Boundary = new GameObject("Boundary", new System.Type[] { typeof(BoxCollider2D) });
        Boundary.transform.SetParent(to.transform);
        Boundary.transform.localPosition = new Vector3(0, 0, 0);
        Boundary.layer = LayerMask.NameToLayer("Boundary");
        var boxCollider = Boundary.GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        //Pick random sizes
        //boxCollider.size = new Vector2(Random.Range(MinDimension, MaxDimension), Random.Range(MinDimension, MaxDimension));
        boxCollider.size = new Vector2(length, width);
        return boxCollider;
    }

    public float HalfBoundWidth => VisualBoundWidth / 2.0f;

    //Place viusal components and a composity collider for them
    public GameObject PlaceBoundaryVisualPrefabs(BoxCollider2D collider2D, GameObject containedIn)
    {
        CompositeVisualBoundary = new GameObject("CompositeViualBoundary", new System.Type[] { typeof(CompositeCollider2D) });
        CompositeVisualBoundary.transform.SetParent(containedIn.transform, false);
        var compositeCollider = CompositeVisualBoundary.GetComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        //compositeCollider.generationType = CompositeCollider2D.GenerationType.Manual;
        CompositeVisualBoundary.layer = LayerMask.NameToLayer("Obstacle");
        //CompositeVisualBoundary.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        CompositeVisualBoundary.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        float halfHeight = collider2D.size.y / 2.0f;
        float halfWidth = collider2D.size.x / 2.0f;

        PlaceHorizontal(halfHeight + HalfBoundWidth, collider2D.size.x);
        PlaceHorizontal(-halfHeight - HalfBoundWidth, collider2D.size.x);
        PlaceVertical(-halfWidth - HalfBoundWidth, collider2D.size.y);
        PlaceVertical(halfWidth + HalfBoundWidth, collider2D.size.y);

        compositeCollider.GenerateGeometry();
        return CompositeVisualBoundary;
    }

    private void PlaceVertical(float x, float length)
    {
        var leftSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        leftSide.transform.localPosition = new Vector3(x, 0, 0);
        leftSide.transform.localScale = new Vector3(VisualBoundWidth, length + HalfBoundWidth, 0);
    }

    private void PlaceHorizontal(float y, float length)
    {
        var topSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        topSide.transform.localPosition = new Vector3(0, y, 0);
        topSide.transform.localScale = new Vector3(length + HalfBoundWidth, VisualBoundWidth, 0);
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
        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(this.transform);
        Obstacles.transform.localPosition = new Vector3(0, 0, 0);
        this.LevelRandom = new System.Random(RandomSeed);
        //        BoxCollider2D box = InitLevelBoundary(
        //            Helpers.GetRandomFloat(LevelRandom, LevelProperties.M, MaxDimension)
        //            , Helpers.GetRandomFloat(LevelRandom, MinDimension, MaxDimension));

        BoxCollider2D box = InitLevelBoundary(LevelProperties.LevelSize.x, LevelProperties.LevelSize.y, this.gameObject);

        //SpawnRandomObstacles(box, Obstacles);
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

    //    private void SpawnRandomObstacles(BoxCollider2D box, GameObject Obstacles)
    //    {
    //        for (int i = 0; i < ObstaclesSpawned; i++)
    //        {
    //            for (int j = 0; j < 5; j++)
    //            {
    //                GameObject randomPrefab = GetRandomPrefab();
    //                var obstacle = SpawnPrefabWithoutCollision(randomPrefab, box, 5);
    //                if (obstacle != null)
    //                {
    //                    //Samples position is already inside box collider, keep its values
    //                    obstacle.transform.SetParent(Obstacles.transform, true);
    //                    break;
    //                }
    //            }
    //        }
    //    }

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