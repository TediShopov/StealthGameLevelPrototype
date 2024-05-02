using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class ContinuosFutureLevelTests
{
    //    private DiscreteRecalculatingFutureLevel CreateFutureLevelMono()
    //    {
    //        GameObject Level = new GameObject("Level");
    //        Level.tag = "Level";
    //
    //        GameObject futureLevelObject = new GameObject("CLI");
    //        futureLevelObject.transform.parent = Level.transform;
    //        return futureLevelObject.AddComponent<DiscreteRecalculatingFutureLevel>();
    //    }
    //
    //    private PatrolEnemyMono SpawnPatrolMono(DiscreteRecalculatingFutureLevel cli)
    //    {
    //        var Enemy = GameObject.Instantiate(
    //            EnemyPrefab,
    //            new Vector3(1, 0, 0),
    //            Quaternion.identity,
    //            cli.transform);
    //        PatrolEnemyMono patrolMono = Enemy.GetComponent<PatrolEnemyMono>();
    //        return patrolMono;
    //    }
    //
    //    public GameObject EnemyPrefab = Resources.Load<GameObject>("Prefabs/Enemy");
    //
    //    // A Test behaves as an ordinary method
    //    [Test]
    //    public void IsCollision_AllInCone()
    //    {
    //        var cli = CreateFutureLevelMono();
    //        PatrolEnemyMono patrolPath = SpawnPatrolMono(cli);
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //        bool isColliding = cli.IsColliding(
    //            new Vector2(0, 0), new Vector2(1, 0), 0, 1);
    //        Assert.True(isColliding);
    //        isColliding = cli.IsColliding(
    //            new Vector2(-100, -100), new Vector2(-100, 0), 0, 1);
    //        Assert.False(isColliding);
    //    }
    //
    //    [Test]
    //    public void IsCollision_InConeInTime()
    //    {
    //        var cli = CreateFutureLevelMono();
    //
    //        //var EnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy");
    //        PatrolEnemyMono patrolPath = SpawnPatrolMono(cli);
    //        patrolPath.InitPatrol(
    //            new List<Vector2>
    //            {
    //                new Vector2(0, 0),
    //                new Vector2(1, 0)
    //            }
    //            );
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //
    //        bool isColliding = cli.IsColliding(
    //            new Vector2(-1, 0), new Vector2(-1, 0), 0, 2);
    //        Assert.IsTrue(isColliding);
    //    }
    //
    //    [Test]
    //    public void IsCollidingSimulation_DoesntChangeActualIThreat()
    //    {
    //        var cli = CreateFutureLevelMono();
    //        //var EnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy");
    //        var Enemy = GameObject.Instantiate(
    //            EnemyPrefab,
    //            new Vector3(1, 0, 0),
    //            Quaternion.identity,
    //            cli.transform);
    //        PatrolEnemyMono patrolMono = Enemy.GetComponent<PatrolEnemyMono>();
    //        patrolMono.InitPatrol(
    //            new List<Vector2>
    //            {
    //                new Vector2(0, 0),
    //                new Vector2(1, 0)
    //            }
    //            );
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //        bool isColliding = cli.IsColliding(
    //            new Vector2(-1, 0), new Vector2(-1, 0), 0, 2);
    //        foreach (var t in cli.DynamicThreats)
    //        {
    //            Assert.AreEqual(t.Time, 0.0f);
    //        }
    //        Assert.IsTrue(isColliding);
    //    }
    //
    //    [Test]
    //    public void IsCollidingSimulation_CorrectPlayerPosition()
    //    {
    //        var cli = CreateFutureLevelMono();
    //        PatrolEnemyMono patrolMono = SpawnPatrolMono(cli);
    //        patrolMono.InitPatrol(
    //            new List<Vector2>
    //            {
    //                new Vector2(0, 0),
    //                new Vector2(1, 0)
    //            }
    //            );
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //        bool isColliding = cli.IsColliding(
    //            new Vector2(-1, 0), new Vector2(-1, 0), 1000, 1100);
    //        foreach (var t in cli.DynamicThreats)
    //        {
    //            Assert.AreEqual(t.Time, 0.0f);
    //        }
    //        Assert.IsTrue(isColliding);
    //    }
    //
    //    [Test]
    //    public void SingleThreatDynamicMovevement_IsColliding()
    //    {
    //        var cli = CreateFutureLevelMono();
    //        PatrolEnemyMono patrolMono = SpawnPatrolMono(cli);
    //        patrolMono.InitPatrol(
    //            new List<Vector2>
    //            {
    //                new Vector2(0, 0),
    //                new Vector2(1, 0)
    //            }
    //            );
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //        bool isColliding = cli.IsColliding(
    //            new Vector2(-1, 0), new Vector2(-1, 0), 1000, 1100);
    //        foreach (var t in cli.DynamicThreats)
    //        {
    //            Assert.AreEqual(t.Time, 0.0f);
    //        }
    //        Assert.IsTrue(isColliding);
    //    }
    //
    //    [Test]
    //    public void MovingGuardStationaryTarget_IsColliding()
    //    {
    //        //Arrange
    //        var cli = CreateFutureLevelMono();
    //        var p = SpawnPatrolMono(cli);
    //        var route = new List<Vector2>();
    //        //1 unit is the distance an enemy travels for a second
    //        float unit = p.EnemyProperties.Speed;
    //        route.Add(Vector2.zero); //Start from zero
    //        route.Add(route[route.Count - 1] + Vector2.left * unit);
    //        route.Add(route[route.Count - 1] + Vector2.left * unit);
    //        p.InitPatrol(route);
    //        Physics2D.SyncTransforms();
    //        cli.Init();
    //
    //        //Stationary target allong guards path
    //        var targets = new List<Vector2>();
    //        targets.Add(Vector2.zero); //Edge case for the start
    //        targets.Add(Vector2.left * unit);
    //        targets.Add(Vector2.left * unit * 0.5f);
    //        targets.Add(Vector2.left * (unit + p.EnemyProperties.ViewDistance / 3.0f));
    //        targets.Add(Vector2.left * (unit + p.EnemyProperties.ViewDistance / 2.0f));
    //        targets.Add(Vector2.left * (unit + p.EnemyProperties.ViewDistance));
    //
    //        //Assert`
    //        foreach (var target in targets)
    //        {
    //            Vector3 targetStart = new Vector3(target.x, target.y, 0);
    //            Vector3 targetEnd = new Vector3(target.x, target.y, 1);
    //            Assert.IsTrue(cli.IsColliding(targetStart, targetEnd));
    //        }
    //    }
    //
    //    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    //    // `yield return null;` to skip a frame.
    //    [UnityTest]
    //    public IEnumerator ContinuosFutureLevelTestsWithEnumeratorPasses()
    //    {
    //        // Use the Assert class to test conditions.
    //        // Use yield to skip a frame.
    //        yield return null;
    //    }
}