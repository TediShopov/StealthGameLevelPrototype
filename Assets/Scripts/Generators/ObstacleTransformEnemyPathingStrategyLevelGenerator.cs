using Codice.CM.SEIDInfo;
using PlasticPipe.Certificates;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

/// <summary>
/// OTEPS is a level generator that encodes level obstacles transform in its layout explicitly
///  0 --> Used to determined ratio between enemies and obstacles
/// and the guard pathing strategies implicitly (list of beahviour that pick next node on each
///  interesection)
///  Chromomosome Layout
///  All Real Number from 0...1
///  0 --> Used to determined ratio between enemies and obstacles
///  O*0 --> Obstacly type picked from a list
///  O*1 --> X position
///  O*2 --> Y position
///  O*3 --> Scale (lerped from Min and Max obstacle scale properties)
///  O*4 --> Rotation
//   E*0 - E*5 --> Enemy behaviours to use in the next intersections
/// </summary>
[RequireComponent(typeof(FloodfilledRoadmapGenerator))]
[RequireComponent(typeof(DiscretePathGenerator))]
[RequireComponent(typeof(IFutureLevel))]
[ExecuteInEditMode]
public class ObstacleTransformEnemyPathingStrategyLevelGenerator :
    LevelPhenotypeGenerator
{
    public const int ObstaccleGeneLength = 5;
    public const int EnemyGeneLength = 5;
    public int NecessaryObstacles = 3;
    public int EnemyCount;
    private int EntityCount => (LevelChromosome.Length - 1) / 5;
    [HideInInspector] public FloodfilledRoadmapGenerator RoadmapGenerator;
    [HideInInspector] public DiscretePathGenerator PathGenerator;
    public DiscreteRecalculatingFutureLevel FutureLevel;

    public void Awake()
    {
        RoadmapGenerator = GetComponent<FloodfilledRoadmapGenerator>();
        PathGenerator = GetComponent<DiscretePathGenerator>();
    }

    public int GetObstaclesToSpawn(ref int geneIndex)
    {
        float obstacleToEnemyRatio = GetGeneValue(geneIndex);
        geneIndex++;
        //Both obstacle and enemies need 5 genes to represent an entity
        int obstacleCount = Mathf.CeilToInt(
        Mathf.Lerp(NecessaryObstacles, EntityCount - 1, obstacleToEnemyRatio));
        return obstacleCount;
    }

    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        if (chromosome is not OTEPSLevelChromosome)
            throw new System.ArgumentException("OTEPS Level generator requries OTEPS level chromosome");
        RoadmapGenerator.ObstacleLayerMask = LevelProperties.ObstacleLayerMask;
        RoadmapGenerator.BoundaryLayerMask = LevelProperties.BoundaryLayerMask;

        Profiler.BeginSample("Manifest Geometry");
        CreateLevelStructure(to);

        //Setup chromosome
        AttachChromosome(chromosome);

        int geneIndex = GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();

        Physics2D.SyncTransforms();

        Profiler.EndSample();
        //Copy grid components of the level prototype.
        var otherGrid = Data.AddComponent<Grid>();
        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;

        var roadmap = RoadmapGenerator.PrototypeComponent(Data);
        roadmap.Init(to);
        roadmap.DoMeasure(to);
        chromosome.Measurements.Add(roadmap.Result);
        chromosome.EnemyRoadmap = RoadmapGenerator.RoadMap;

        Profiler.BeginSample("Assign Paths");
        //Use the generated roadmap to assign guard paths
        AssignPaths(geneIndex, roadmap.RoadMap);
        Profiler.EndSample();

        //Initialize the future level
        //CopyComponent(FutureLevel, To).Init(To);
        var futurePrototype = FutureLevel.PrototypeComponent(Data);
        futurePrototype.Init();

        stopwatch.Stop();
        chromosome.Measurements.Add(
            new StealthLevelEvaluation.MeasureResult
            {
                Type = typeof(ObstacleTransformEnemyPathingStrategyLevelGenerator),
                Time = stopwatch.ElapsedMilliseconds,
                Value = "Level Generation",
                Category = MeasurementType.INITIALIZATION
            }
            );

        UnityEngine.Debug.Log("Generation of phenotype finished");
    }

    public void AssignPaths(int geneIndex, Graph<Vector2> roadmap)
    {
        LevelRandom = new System.Random();
        PathGenerator.geneIndex = geneIndex;
        PathGenerator.Init(To);
        PathGenerator.Roadmap = roadmap;
        PathGenerator.LevelRandom = LevelRandom;
        PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
        List<List<Vector2>> paths = PathGenerator.GeneratePaths(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            enemyPaths[i].InitPatrol(paths[i]);
        }
        geneIndex = PathGenerator.geneIndex;
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;
        int ObstacleCount = GetObstaclesToSpawn(ref geneIndex);
        EnemyCount = EntityCount - ObstacleCount;
        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(Contents.transform);

        //Test for off by oen errors
        for (int i = 0; i < ObstacleCount; i++)
        {
            SpawnObstacle(ref geneIndex, LevelBounds, Obstacles);
        }

        MergeObstacles(Obstacles);

        //Read enemy counts and spawn enemies

        for (int i = 0; i < EnemyCount; i++)
        {
            Instantiate(LevelProperties.EnemyPrefab, Contents.transform);
        }

        //Enemy Behaviour
        Physics2D.SyncTransforms();
        return geneIndex;
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

    public override LevelChromosomeBase GetAdamChromosome(int s)
    {
        return new OTEPSLevelChromosome(this, new System.Random(s));
    }
}