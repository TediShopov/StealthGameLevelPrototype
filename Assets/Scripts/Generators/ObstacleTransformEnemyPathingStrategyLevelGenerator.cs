using GeneticSharp;
using StealthLevelEvaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Serialization;
using UnityEngine;

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
[RequireComponent(typeof(DiscretePathGenerator))]
[ExecuteInEditMode]
public class ObstacleTransformEnemyPathingStrategyLevelGenerator :
    LevelPhenotypeGenerator
{
    public const int ObstaccleGeneLength = 5;
    public const int EnemyGeneLength = 5;
    [HideInInspector] public int NecessaryObstacles = 3;
    [HideInInspector] public int EnemyCount;

    [HideInInspector] private int EntityCount => (LevelChromosome.Length - 1) / 5;

    [SerializeField] public FloodfilledRoadmapGenerator RoadmapGenerator;

    [HideInInspector] public DiscretePathGenerator PathGenerator;
    [SerializeReference, SubclassPicker] public IFutureLevel FutureLevel;

    public void Awake()
    {
        PathGenerator = GetComponent<DiscretePathGenerator>();
        //        if (RoadmapGenerator is null)
        //            RoadmapGenerator = new FloodfilledRoadmapGenerator();
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
        To = to;
        EnsureComponentValidity();

        //        if (chromosome is not OTEPSLevelChromosome)
        //            throw new System.ArgumentException("OTEPS Level generator requries OTEPS level chromosome");

        chromosome.Phenotype = new LevelPhenotype();
        int geneIndex = 0;

        MeasureResult geomtry = MeasureResultFromStep("Geometry Construction",
            () => { geneIndex = GenerateGeometry(chromosome, to); });

        MeasureResult roadmap = MeasureResultFromStep("Roadmap",
            () => { AssignRoadmToPhenotype(chromosome, to); });

        MeasureResult paths = MeasureResultFromStep("Path Assignment",
            () => { AssignPaths(geneIndex, chromosome); });

        MeasureResult future = MeasureResultFromStep("Level Future",
             () => { CalculateLevelFuture(chromosome); });

        chromosome.AddOrReplace(geomtry);
        chromosome.AddOrReplace(roadmap);
        chromosome.AddOrReplace(paths);
        chromosome.AddOrReplace(future);

        //Add extra visualizer to provide disegner insight
        // into ediot view
        this.Data.AddComponent<RoadmapVisualizer>();
        this.Data.AddComponent<FutureLevelSlider>();

        UnityEngine.Debug.Log("Generation of phenotype finished");
    }

    protected virtual void EnsureComponentValidity()
    {
        if (this.FutureLevel == null)
        {
            throw new System.ArgumentNullException(nameof(this.FutureLevel));
            //FutureLevel = new DiscreteCahcedFutureLevel(0.2f, 50, LevelProperties);
        }

        if (RoadmapGenerator is null)
        {
            throw new System.ArgumentNullException(nameof(this.RoadmapGenerator));
            //RoadmapGenerator = new FloodfilledRoadmapGenerator();
        }
        RoadmapGenerator.ObstacleLayerMask = LevelProperties.ObstacleLayerMask;
        RoadmapGenerator.BoundaryLayerMask = LevelProperties.BoundaryLayerMask;
    }

    protected void CalculateLevelFuture(LevelChromosomeBase chromosomeBase)
    {
        //Initialize the future level
        var futurePrototype = (IFutureLevel)FutureLevel.Clone();
        try
        {
            futurePrototype.GlobalTransform = chromosomeBase.Manifestation.transform;
        }
        catch (System.Exception)
        {
            throw;
        }
        futurePrototype.Generate(chromosomeBase.Phenotype);
        chromosomeBase.Phenotype.FutureLevel = futurePrototype;
    }

    protected void AssignRoadmToPhenotype(LevelChromosomeBase chromosome, GameObject to)
    {
        RoadmapGenerator.LevelProperties = this.LevelProperties;
        RoadmapGenerator.Generate(to);
        chromosome.Phenotype.Roadmap = RoadmapGenerator.RoadMap;
        chromosome.Phenotype.Zones = RoadmapGenerator.LevelGrid;
    }

    protected int GenerateGeometry(LevelChromosomeBase chromosome, GameObject to)
    {
        CreateLevelStructure(to);

        //Setup chromosome
        AttachChromosome(chromosome);

        int geneIndex = GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();

        Physics2D.SyncTransforms();
        return geneIndex;
    }

    public MeasureResult MeasureResultFromStep(string stepName, System.Action action)
    {
        var toReturn = new MeasureResult();
        toReturn.Name = stepName;
        toReturn.Category = MeasurementType.INITIALIZATION;
        toReturn.Parent = null;
        toReturn.Time = Helpers.TrackExecutionTime(action.Invoke);
        return toReturn;
    }

    public void AssignPaths(
        int geneIndex, LevelChromosomeBase levelChromosomeBase)
    {
        try
        {
            int seed = Mathf.CeilToInt((float)levelChromosomeBase.GetGene(geneIndex).Value
                * (float)levelChromosomeBase.GetGene(geneIndex + 1).Value
                * (float)levelChromosomeBase.GetGene(geneIndex + 2).Value
                * (float)levelChromosomeBase.GetGene(geneIndex + 3).Value
                * (float)levelChromosomeBase.GetGene(geneIndex + 4).Value);

            LevelRandom = new System.Random(seed);
            PathGenerator.geneIndex = geneIndex;
            PathGenerator.Init(To);
            PathGenerator.Roadmap = LevelChromosome.Phenotype.Roadmap;
            PathGenerator.LevelRandom = LevelRandom;
            PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
            List<List<Vector2>> paths = PathGenerator.GeneratePaths(EnemyCount);

            if (LevelChromosome.Phenotype.Threats == null)
                LevelChromosome.Phenotype.Threats = new List<IPredictableThreat>();

            for (int i = 0; i < EnemyCount; i++)
            {
                enemyPaths[i].InitPatrol(paths[i]);
                levelChromosomeBase.Phenotype.Threats.Add(enemyPaths[i].GetPatrol());
            }
            geneIndex = PathGenerator.geneIndex;
        }
        catch (System.Exception)
        {
            UnityEngine.Debug.Log($"Error getting {geneIndex} from {levelChromosomeBase.Length}");

            throw;
        }
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
                comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
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