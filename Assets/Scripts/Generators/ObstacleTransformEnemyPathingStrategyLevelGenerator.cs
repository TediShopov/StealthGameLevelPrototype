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
using UnityEngine.U2D;
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
    public SpriteShape SpriteShape;

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

    public override IStealthLevelPhenotype GeneratePhenotype(LevelChromosomeBase levelChromosome)
    {
        LevelChromosome = levelChromosome;
        Bounds levelBounds = new Bounds(
            new Vector2(0, 0),
            LevelProperties.LevelSize);

        StealthLevel stealthLevel = new StealthLevel(levelBounds);

        int geneIndex = 0;
        int ObstaclesSpawned = (levelChromosome.Length - 4) / 5;

        //Test for off by oen errors
        for (int i = 0; i < ObstaclesSpawned; i++)
        {
            SpawnObstacleFromListData(ref geneIndex, stealthLevel);
        }
        return stealthLevel;
    }

    //    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    //    {
    //        Stopwatch stopwatch = Stopwatch.StartNew();
    //        if (chromosome is not OTEPSLevelChromosome)
    //            throw new System.ArgumentException("OTEPS Level generator requries OTEPS level chromosome");
    //        RoadmapGenerator.ObstacleLayerMask = LevelProperties.ObstacleLayerMask;
    //        RoadmapGenerator.BoundaryLayerMask = LevelProperties.BoundaryLayerMask;
    //
    //        IStealthLevelPhenotype stealthLevel = GeneratePhenotype(chromosome);
    //        //GENERATE LEVEL CONTENT
    //        //Spawn game object from obstacle data
    //
    //        //Copy grid components of the level prototype.
    //        //        var otherGrid = Data.AddComponent<Grid>();
    //        //        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
    //        //        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
    //        //        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;
    //        //
    //        //        var roadmap = RoadmapGenerator.PrototypeComponent(Data);
    //        //        roadmap.Init(to);
    //        //        roadmap.DoMeasure(to);
    //        //        chromosome.Measurements.Add(roadmap.Result);
    //        //        chromosome.EnemyRoadmap = RoadmapGenerator.RoadMap;
    //        //
    //        //        //Initialize the future level
    //        //        //CopyComponent(FutureLevel, To).Init(To);
    //        //        var futurePrototype = FutureLevel.PrototypeComponent(Data);
    //        //        futurePrototype.Init();
    //        //
    //    }

    public override LevelChromosomeBase GetAdamChromosome(int s)
    {
        return new OTEPSLevelChromosome(this, new System.Random(s));
    }
}