using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class SpawnRandomStealthLevel : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;
    public GameObject BoundaryViualPrefab;
    public GameObject LevelInitializer;
    public List<GameObject> ObstaclePrefabs ;
    private GameObject Boundary;
    public float VisualBoundWidth;
    private GameObject CompositeVisualBoundary;
    public int RandomSeed;
    public int RandomObjectSpawned;
    public LayerMask ObstacleLayerMask;


    //Size modificaiton 
    [Range(10,100)]
    public float MaxDimension = 50.0f;
    [Range(10,50)]
    public float MinDimension = 50.0f;
    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(RandomSeed);
        BoxCollider2D box = InitLevelBoundary();

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(this.transform);
        for (int i = 0; i < RandomObjectSpawned; i++) 
        {
            SpawnRandomObject(box,Obstacles);
        }
        var playerInstance =  SpawnPrefabWithoutCollision(PlayerPrefab,box, 150);
        SetupRRT(playerInstance.GetComponent<CharacterController2D>(),new Vector3());
        Instantiate(LevelInitializer, this.transform);

    }
    public BoxCollider2D InitLevelBoundary()
    {
        Boundary = new GameObject("Boundary", new System.Type[] { typeof(BoxCollider2D) });
        Boundary.transform.SetParent(this.transform, false);
        Boundary.layer = LayerMask.NameToLayer("Boundary");
        var boxCollider = Boundary.GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        //Pick random sizes
        boxCollider.size=  new Vector2(Random.Range(MinDimension,MaxDimension),Random.Range(MinDimension,MaxDimension));
        PlaceBoundaryVisualPrefabs(boxCollider);
        return boxCollider;
    }
        //Place viusal components and a composity collider for them
    public void PlaceBoundaryVisualPrefabs(BoxCollider2D collider2D)
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
    }


    private void PlaceVertical(float x, float length)
    {
        var leftSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        leftSide.transform.position = new Vector3(x, 0, 0);
        leftSide.transform.localScale = new Vector3(VisualBoundWidth, length, 0);
    }

    private void PlaceHorizontal(float y,float length)
    {
        var topSide = Instantiate(BoundaryViualPrefab, CompositeVisualBoundary.transform);
        topSide.transform.position = new Vector3(0, y, 0);
        topSide.transform.localScale = new Vector3(length, VisualBoundWidth, 0);
    }
    void SpawnRandomObject(BoxCollider2D spawnArea, GameObject obstaclesContainer)
    {
        if (ObstaclePrefabs.Count == 0)
        {
            Debug.LogError("Object list is empty. Please assign GameObjects to the ObstaclePrefabs array.");
            return;
        }



        GameObject randomObject = ObstaclePrefabs[Random.Range(0, ObstaclePrefabs.Count)];
        Vector2 randomPosition = GetRandomPositionInsideCollider(spawnArea);

        GameObject instantiatedObject = Instantiate(randomObject, randomPosition, Quaternion.identity,obstaclesContainer.transform);
        float scaleRandom = Random.Range(0.2f, 5.0f);
        instantiatedObject.transform.localScale = new Vector3(scaleRandom,scaleRandom,0); ;

        int randomRotationIndex = Random.Range(0, 8);
        float angle = Mathf.Lerp(0f, 90f, randomRotationIndex / 7f); // 7 because there are 8 positions
        instantiatedObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    Vector2 GetRandomPositionInsideCollider(BoxCollider2D spawnArea)
    {
        Vector2 colliderSize = spawnArea.size;
        Vector2 colliderCenter = spawnArea.bounds.center;
        float randomX = Random.Range(colliderCenter.x - colliderSize.x / 2f, colliderCenter.x + colliderSize.x / 2f);
        float randomY = Random.Range(colliderCenter.y - colliderSize.y / 2f, colliderCenter.y + colliderSize.y / 2f);
        return new Vector2(randomX, randomY);
    }
    GameObject SpawnPrefabWithoutCollision(GameObject prefab,BoxCollider2D spawnArea, int tries)
    {
        Vector2 randomPosition = GetRandomPositionInsideCollider(spawnArea);

        for (int i = 0; i < tries; i++)
        {
            // Instantiate the prefab at the random position
            GameObject instantiatedPrefab = Instantiate(prefab, randomPosition, Quaternion.identity);
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

    bool CheckCollisionWithObstacles(GameObject spawnedObject)
    {
        // Check if the spawned object collides with any obstacles on the specified layer
        Collider2D[] colliders = Physics2D.OverlapBoxAll(spawnedObject.transform.position, spawnedObject.GetComponent<Collider2D>().bounds.size, 0f, ObstacleLayerMask);
        // If there are any colliders in the array, there is a collision
        return colliders.Length > 0;
    }
    void SetupRRT(CharacterController2D characterController, Vector3 destination) 
    {
         RapidlyExploringRandomTreeVisualizer[] rrts= LevelInitializer.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
        foreach (var rrt in rrts)
        {
            rrt.StartNode = characterController.transform;
            rrt.Controller = characterController;
            rrt.EndNode = characterController.transform;

       }

    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
