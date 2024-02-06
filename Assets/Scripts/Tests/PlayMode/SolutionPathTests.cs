using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests correct output of how humanlike a vector of noes forming a path is
/// </summary>
public class SolutionPathTests
{
    public LevelGeneratorBase LevelGenerator;

    // A Test behaves as an ordinary method
    [Test]
    public void SolutionPathTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    [Test]
    public void NoEnemiesLevel_ReturnsZero()
    {
        //        GameObject levelObject = new GameObject("Level");
        //        levelObject.tag = "Level";
        //        var levelBoundary =LevelGenerator.InitLevelBoundary(6, 6);
        //        LevelGenerator.PlaceBoundaryVisualPrefabs(levelBoundary, levelObject);
        //
        //        int enemyCount = Mathf.CeilToInt(Mathf.Lerp(MinEnemiesSpawned, MaxEnemiesSpawned, geneIndex));
        //        for (int i = 0; i < enemyCount; i++)
        //        {
        //            Instantiate(EnemyPrefab, this.transform);
        //        }
        //
        //        Physics2D.SyncTransforms();
        //        //Solvers
        //        Instantiate(LevelInitializer, this.transform);
        //        var levelInitializer = gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //        var voxelizedLevel = gameObject.GetComponentInChildren<VoxelizedLevel>();
        //        var multipleRRTSolvers = gameObject.GetComponentInChildren<MultipleRRTRunner>();
        //        var pathGenerator = gameObject.GetComponentInChildren<PathGeneratorClass>();
        //        pathGenerator.LevelRandom = LevelRandom;
        //        levelInitializer.Init();
        //        voxelizedLevel.Init();
        //        multipleRRTSolvers.Run();

        float actual = 1;
        float expected = 0;
        Assert.AreEqual(expected, actual, 0.0001f);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator SolutionPathTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}