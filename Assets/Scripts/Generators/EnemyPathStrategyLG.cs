using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemyPathStrategyLG : LevelPhenotypeGenerator
{
    public const int ObstaccleGeneLength = 5;
    public const int EnemyGeneLength = 5;
    public int NecessaryObstacles = 3;
    public int EnemyCount;
    private int EntityCount => (LevelChromosome.Length - 1) / 5;

    public int GetObstaclesToSpawn(ref int geneIndex)
    {
        float obstacleToEnemyRatio = GetGeneValue(geneIndex);
        geneIndex++;
        //Both obstacle and enemies need 5 genes to represent an entity

        //        //Converting 0,1 range to a range [3, chromosomeLength -1]
        //        //Ensures at least 3 obstacle for a level and at least one chromose for enemy
        //        float convertedObstacleToEnemyRatio =
        //            Mathf.Lerp(NecessaryObstacles, EntityCount - 1, obstacleToEnemyRatio);

        //        int enemyWeight = Mathf.FloorToInt(Mathf.Lerp(1, 5, obstacleToEnemyRatio));
        //        int totalWeight = 1 + enemyWeight;
        //
        //        float e = (float)enemyWeight / 6.0f;
        //        int enemyCount = Mathf.CeilToInt(e * EntityCount);
        //        int obstacleCount = EntityCount - enemyCount;

        int obstacleCount = Mathf.CeilToInt(
        Mathf.Lerp(NecessaryObstacles, EntityCount - 1, obstacleToEnemyRatio));

        return obstacleCount;
    }

    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        To = to;
        LevelChromosome = chromosome;
        //Boundary
        To.tag = "Level";

        var data = new GameObject("Data");
        data.transform.SetParent(To.transform);
        LevelChromosomeMono chromosomeMono = data.AddComponent<LevelChromosomeMono>();
        chromosomeMono.Chromosome = (LevelChromosome)chromosome;

        var Obstacles = new GameObject("Obstacles");
        BoxCollider2D box =
            SetupLevelInitials(chromosome, to,
            new GameObject("VisBound"));
        Instantiate(LevelInitializer, To.transform);
        int geneIndex = GenerateLevelContent(chromosome, box);
        //Solvers
        var levelInitializer = To.gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
        //var multipleRRTSolvers = To.gameObject.GetComponentInChildren<MultipleRRTRunner>();
        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
        AssignPaths(geneIndex);

        Debug.Log("Generation of phenotype finished");
    }

    //    public void AssignPaths(int geneIndex)
    //    {
    //        LevelRandom = new System.Random();
    //        var pathGenerator =
    //            To.GetComponentInChildren<DiscretePathGenerator>();
    //        pathGenerator.geneIndex = geneIndex;
    //        FloodfilledRoadmapGenerator floodfilledRoadmapGenerator = To.GetComponentInChildren<FloodfilledRoadmapGenerator>();
    //        floodfilledRoadmapGenerator.Init();
    //        floodfilledRoadmapGenerator.FloodRegions();
    //        pathGenerator.Roadmap = floodfilledRoadmapGenerator.RoadMap;
    //        pathGenerator.LevelRandom = LevelRandom;
    //        PatrolPath[] enemyPaths = To.GetComponentsInChildren<PatrolPath>();
    //        List<List<Vector2>> paths = pathGenerator.GeneratePaths(EnemyCount);
    //        for (int i = 0; i < EnemyCount; i++)
    //        {
    //            enemyPaths[i].BacktrackPatrolPath = new BacktrackPatrolPath(paths[i]);
    //        }
    //        geneIndex = pathGenerator.geneIndex;
    //    }

    public void AssignPaths(int geneIndex)
    {
        LevelRandom = new System.Random();
        var pathGenerator =
            To.GetComponentInChildren<DiscretePathGenerator>();
        pathGenerator.geneIndex = geneIndex;
        FloodfilledRoadmapGenerator floodfilledRoadmapGenerator = To.GetComponentInChildren<FloodfilledRoadmapGenerator>();
        floodfilledRoadmapGenerator.Init();
        floodfilledRoadmapGenerator.FloodRegions();
        pathGenerator.Roadmap = floodfilledRoadmapGenerator.RoadMap;
        pathGenerator.LevelRandom = LevelRandom;
        PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
        List<List<Vector2>> paths = pathGenerator.GeneratePaths(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            enemyPaths[i].InitPatrol(paths[i]);
        }
        geneIndex = pathGenerator.geneIndex;
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome, BoxCollider2D box)
    {
        int geneIndex = 0;
        int ObstacleCount = GetObstaclesToSpawn(ref geneIndex);
        EnemyCount = EntityCount - ObstacleCount;
        var Obstacles = new GameObject("Obstacles");
        if (EnemyCount + ObstacleCount > EntityCount)
        {
            int b = 3;
        }
        if ((EnemyCount + ObstacleCount) * 5 + 1 >= chromosome.Length)
        {
            int a = 3;
        }

        //Test for off by oen errors
        for (int i = 0; i < ObstacleCount; i++)
        {
            SpawnObstacle(ref geneIndex, box, Obstacles);
        }

        //Read enemy counts and spawn enemies

        for (int i = 0; i < EnemyCount; i++)
        {
            Instantiate(EnemyPrefab, To.transform);
        }

        //Enemy Behaviour

        Physics2D.SyncTransforms();
        return geneIndex;
    }
}