using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.U2D;

//Create unity gameobject form a stealth level phenptye
public interface ILevelManifestor
{
    public void Manifest(LevelChromosomeBase chromosomeLevel, GameObject to);
}

public class LevelManifestor : MonoBehaviour, ILevelManifestor
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

        //Containing object is made to be static composiProfilerte collider
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

    protected void AttachChromosome(LevelChromosomeBase chromosome)
    {
    }

    private void CreateSpline(Spline spline, List<Vector2> points)
    {
        if (points.Count < 2)
            return;

        spline.Clear();
        for (int i = 0; i < points.Count; i++)
        {
            spline.InsertPointAt(i, points[i]);
        }
    }

    public void Manifest(LevelChromosomeBase levelChromosomeBase, GameObject to)
    {
        Profiler.BeginSample("Manifest Geometry");
        CreateLevelStructure(to);

        PlaceBoundaryVisualPrefabs();

        var stealthLevelPhenotype = levelChromosomeBase.ActualLevelPhenotype;

        foreach (var obstalceData in stealthLevelPhenotype.GetObstacles())
        {
            GameObject obstacle = new GameObject("PolygonObstacle");
            obstacle.transform.position = obstalceData.Position;
            obstacle.transform.rotation = Quaternion.Euler(0, 0, obstalceData.Rotaiton);
            obstacle.transform.localScale = new Vector3(obstalceData.Scale, obstalceData.Scale);

            obstacle.transform.SetParent(Contents.transform, false);
            obstacle.layer = LayerMask.NameToLayer("Obstacle");

            var polygonCollider = obstacle.AddComponent<PolygonCollider2D>();
            polygonCollider.points = obstalceData.PolygonData.ToArray();

            var renderer = obstacle.AddComponent<SpriteShapeRenderer>();
            renderer.color = new Color(0, 0, 0);

            var spriteControll = obstacle.AddComponent<SpriteShapeController>();
            this.CreateSpline(spriteControll.spline, obstalceData.PolygonData);
            renderer.enabled = true;
        }

        Physics2D.SyncTransforms();

        Profiler.EndSample();
        //Copy grid components of the level prototype.
        var otherGrid = Data.AddComponent<Grid>();
        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;

        Profiler.EndSample();
        UnityEngine.Debug.Log("Generation of phenotype finished");
    }

    public void MergeObstacles(GameObject Obstacles)
    {
        var colliders = Obstacles.GetComponentsInChildren<Collider2D>().ToList();
        ContactFilter2D filter = new ContactFilter2D
        {
            useDepth = false,
            useLayerMask = true,
            useTriggers = false,
            useOutsideDepth = false,
            layerMask = LevelProperties.ObstacleLayerMask
        };
        for (int i = 0; i < colliders.Count; i++)
        {
            var overlapCollider = colliders[i];
            List<Collider2D> overlappingCollider = new List<Collider2D>();
            Physics2D.OverlapCollider(
                overlapCollider,
                filter,
                overlappingCollider
                );
            overlappingCollider.Add(overlapCollider);
            if (overlappingCollider.Count >= 2)
            {
                GameObject gm = new GameObject("Composite");
                gm.transform.SetParent(Obstacles.transform);
                var rb = gm.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var comp = gm.AddComponent<CompositeCollider2D>();
                gm.layer = LayerMask.NameToLayer("Obstacle");

                comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
                comp.generationType = CompositeCollider2D.GenerationType.Manual;
                colliders.RemoveAll(x => overlappingCollider.Contains(x));

                AddShapesToCompositeObject(overlappingCollider, comp);
                Physics2D.SyncTransforms();
                colliders.Add(comp);
                Physics2D.SyncTransforms();
            }
        }
    }

    private static void AddShapesToCompositeObject
        (List<Collider2D> overlappingCollider, CompositeCollider2D comp)
    {
        foreach (var c in overlappingCollider)
        {
            if (c is not CompositeCollider2D)
                SimpleShapeToCompositeCollider(c, comp);
            else
            {
                var otherComposite = (CompositeCollider2D)c;
                var shapesFroLevelChromosomeBasemOtherComposite = otherComposite.gameObject.GetComponentsInChildren<Collider2D>()
                    .Where(x => x.usedByComposite == true).
                    ToList();
                foreach (var shape in shapesFroLevelChromosomeBasemOtherComposite)
                {
                    SimpleShapeToCompositeCollider(shape, comp);
                    shape.gameObject.transform.SetParent(comp.transform, true);
                }
                Physics2D.SyncTransforms();
                DestroyImmediate(otherComposite.gameObject);
            }
        }
        comp.GenerateGeometry();
    }

    private static void SimpleShapeToCompositeCollider(Collider2D c, CompositeCollider2D comp)
    {
        if (c is not CompositeCollider2D)
        {
            //A simple shape
            //Remove rigidbody as only final composite collider needs to have it
            var rigidbody = c.gameObject.GetComponent<Rigidbody2D>();
            if (rigidbody)
                DestroyImmediate(rigidbody);

            //Have the shape be used from the composite collider
            var collider = c.gameObject.GetComponent<Collider2D>();
            collider.usedByComposite = true;

            //Set the parent shape
            c.gameObject.transform.SetParent(comp.gameObject.transform);
        }
        else
        {
            throw new System.ArgumentException("Cannot nest composite colliders");
        }
    }
}