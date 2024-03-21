using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemyPathStrategyLG : LevelPhenotypeGenerator
{
    public const int ObstaccleGeneLength = 5;
    public const int EnemyGeneLength = 5;
    public int NecessaryObstacles = 3;
    private int EntityCount => LevelChromosome.Length / 5;

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

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome, BoxCollider2D box)
    {
        int geneIndex = 0;
        int ObstacleCount = GetObstaclesToSpawn(ref geneIndex);
        int EnemyCount = EntityCount - ObstacleCount;
        var Obstacles = new GameObject("Obstacles");

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

        var pathGenerator =
            To.GetComponentInChildren<PathGeneratorClass>();

        //Initialize path generator with enemy count * 5 genes
        float seedf = 0;
        for (int i = 0; i < EnemyCount; i++)
        {
            seedf *= GetGeneValue(geneIndex + i);
        }
        pathGenerator.LevelRandom =
            new System.Random(Mathf.CeilToInt(seedf));
        geneIndex += EnemyCount;
        Physics2D.SyncTransforms();
        return geneIndex;
    }
}