using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

//Class used to proved utility functions for generating
//standartizes level structure
public class LevelGeneratorBase : MonoBehaviour
{
    public LevelProperties LevelProperties;

    [Range(0.0f, 1.0f)]
    public float MinObjectScale = 0.2f;

    [Range(1.0f, 5.0f)]
    public float MaxObjectScale = 1.0f;

    //Randomizer specific for the level
    public System.Random LevelRandom;

    public float VisualBoundWidth;
    protected BoxCollider2D LevelBounds;

    protected GameObject To;

    protected GameObject Contents;
    protected GameObject CompositeVisualBoundary;
    protected GameObject Boundary;
    protected GameObject Obstacles;
    protected GameObject Data;

    public virtual void Generate(GameObject to = null)
    {
        CreateLevelStructure(to);
    }

    public void CreateLevelStructure(GameObject to)
    {
        To = to;
        To.tag = "Level";

        //Create level and initialize boundary object`
        CreateBoundaryObjectAndBox(LevelProperties.LevelSize);

        Data = new GameObject("Data");
        Data.transform.SetParent(to.transform, false);

        Contents = new GameObject("Content");
        Contents.transform.SetParent(to.transform, false);

        PlaceInitialStartGoalPositions();

        Physics2D.SyncTransforms();
    }

    private void PlaceInitialStartGoalPositions()
    {
        var playerInstance = SpawnGameObjectAtRelative(LevelProperties.RelativeStartPosition,
            LevelProperties.PlayerPrefab);
        playerInstance.transform.SetParent(Contents.transform, true);
        //Destination
        //var destinationIntance = SpawnGameObject(ref geneIndex, box, DestinationPrefab);
        var destinationIntance = SpawnGameObjectAtRelative(LevelProperties.RelativeEndPosiiton,
            LevelProperties.DestinationPrefab);
        destinationIntance.transform.SetParent(Contents.transform, true);
    }

    protected GameObject SpawnGameObjectAtRelative(Vector2 coord, GameObject Prefab)
    {
        float x = Mathf.Lerp(LevelBounds.bounds.min.x, LevelBounds.bounds.max.x, coord.x);
        float y = Mathf.Lerp(LevelBounds.bounds.min.y, LevelBounds.bounds.max.y, coord.y);
        var player = Instantiate(Prefab,
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, 0),
            To.transform);
        return player;
    }

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

    protected void CreateBoundaryObjectAndBox(Vector2 size)
    {
        //Spawn boundary object at the boundary level
        Boundary = new GameObject("Boundary", new System.Type[] { typeof(BoxCollider2D) });
        Boundary.transform.SetParent(To.transform);
        Boundary.transform.localPosition = new Vector3(0, 0, 0);
        Boundary.layer = LayerMask.NameToLayer("Boundary");
        var boxCollider = Boundary.GetComponent<BoxCollider2D>();
        //Mark as trigger
        boxCollider.isTrigger = true;
        boxCollider.size = size;
        //Assign to level bounds
        LevelBounds = boxCollider;
    }

    private bool CheckCollisionWithObstacles(GameObject spawnedObject)
    {
        // Check if the spawned object collides with any obstacles on the specified layer
        Collider2D[] colliders = Physics2D.OverlapBoxAll(spawnedObject.transform.position, spawnedObject.GetComponent<Collider2D>().bounds.size, 0f,
            LevelProperties.ObstacleLayerMask);
        // If there are any colliders in the array, there is a collision
        return colliders.Length > 1;
    }

    public float HalfBoundWidth => VisualBoundWidth / 2.0f;

    //Place viusal components and a composity collider for them
    public GameObject PlaceBoundaryVisualPrefabs()
    {
        var visualBoundary = new GameObject("VisualBoundary");
        visualBoundary.transform.SetParent(Contents.transform, false);
        //Add parrent object to contain all smaller wall objects
        CompositeVisualBoundary = new GameObject("CompositeViualBoundary", new System.Type[] { typeof(CompositeCollider2D) });
        CompositeVisualBoundary.transform.SetParent(visualBoundary.transform, false);

        //Containing object is made to be static composite collider
        var compositeCollider = CompositeVisualBoundary.GetComponent<CompositeCollider2D>();
        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        CompositeVisualBoundary.layer = LayerMask.NameToLayer("Obstacle");
        CompositeVisualBoundary.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;

        //Define height and width of walls
        float halfHeight = LevelBounds.size.y / 2.0f;
        float halfWidth = LevelBounds.size.x / 2.0f;

        //Place the four walls of the collider
        PlaceHorizontal(halfHeight + HalfBoundWidth, LevelBounds.size.x);
        PlaceHorizontal(-halfHeight - HalfBoundWidth, LevelBounds.size.x);
        PlaceVertical(-halfWidth - HalfBoundWidth, LevelBounds.size.y);
        PlaceVertical(halfWidth + HalfBoundWidth, LevelBounds.size.y);

        //Manually generate composite collider
        compositeCollider.GenerateGeometry();
        return CompositeVisualBoundary;
    }

    private void PlaceVertical(float x, float length)
    {
        var leftSide = Instantiate(LevelProperties.BoundaryViualPrefab, CompositeVisualBoundary.transform);
        leftSide.transform.localPosition = new Vector3(x, 0, 0);
        leftSide.transform.localScale = new Vector3(VisualBoundWidth, length + HalfBoundWidth, 0);
    }

    private void PlaceHorizontal(float y, float length)
    {
        var topSide = Instantiate(LevelProperties.BoundaryViualPrefab, CompositeVisualBoundary.transform);
        topSide.transform.localPosition = new Vector3(0, y, 0);
        topSide.transform.localScale = new Vector3(length + HalfBoundWidth, VisualBoundWidth, 0);
    }
}