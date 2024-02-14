using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class ContinuosFutureLevelTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void IsCollision_AllInCone()
    {
        GameObject Level = new GameObject("Level");
        Level.tag = "Level";

        //var EnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy");
        var EnemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
        var Enemy = GameObject.Instantiate(
            EnemyPrefab,
            new Vector3(1, 0, 0),
            Quaternion.identity,
            Level.transform);
        PatrolPath patrolPath = Enemy.GetComponent<PatrolPath>();
        GameObject futureLevelObject = new GameObject("CLI");
        futureLevelObject.transform.parent = Level.transform;
        var cli = futureLevelObject.AddComponent<ContinuosFutureLevel>();

        Physics2D.SyncTransforms();
        cli.Init();
        bool isColliding = cli.IsColliding(
            new Vector2(0, 0), new Vector2(1, 0), 0, 1);
        Assert.True(isColliding);
        isColliding = cli.IsColliding(
            new Vector2(-100, -100), new Vector2(-100, 0), 0, 1);
        Assert.False(isColliding);
        //        isColliding = cli.IsColliding(
        //            new Vector2(0, 0), new Vector2(1, 0), 0, 1);
        //        Assert.True(isColliding);
    }

    [Test]
    public void IsCollision_InConeInTime()
    {
        GameObject Level = new GameObject("Level");
        Level.tag = "Level";

        //var EnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy");
        var EnemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
        var Enemy = GameObject.Instantiate(
            EnemyPrefab,
            new Vector3(1, 0, 0),
            Quaternion.identity,
            Level.transform);
        PatrolPath patrolPath = Enemy.GetComponent<PatrolPath>();
        patrolPath.SetPatrolPath(
            new List<Vector2>
            {
                new Vector2(0, 0),
                new Vector2(1, 0)
            }
            );
        GameObject futureLevelObject = new GameObject("CLI");
        futureLevelObject.transform.parent = Level.transform;
        var cli = futureLevelObject.AddComponent<ContinuosFutureLevel>();
        Physics2D.SyncTransforms();
        cli.Init();

        bool isColliding = cli.IsColliding(
            new Vector2(-1, 0), new Vector2(-1, 0), 0, 2);
        Assert.IsTrue(isColliding);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator ContinuosFutureLevelTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}