using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.EnterpriseServices;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.TestTools;

public class SolutionPathRiskMeasurementEditTests
{
    public DefaultEnemyProperties EnemyProperties = new DefaultEnemyProperties()
    {
        FOV = 45,
        ViewDistance = 1.0f,
        Speed = 1.0f
    };

    [Test]
    public void NoPatrolPaths_RiskIsZero()
    {
        //Stationary guard
        List<Vector3> solutionPathRaw = new List<Vector3>()
        {
            new Vector3(0,0,0),
            new Vector3(1,0,1),
            new Vector3(1,1,2)
        };
        ISolutionPathRiskMeasurement solutionPathRiskMeasurement =
            new FieldOfViewRiskMeasure(
                new SolutionPath(solutionPathRaw),
                new List<Patrol>());
        Assert.AreEqual(0, solutionPathRiskMeasurement.OverallRisk(1), 0.001f);
    }

    //TODO move to play tests
    //    [Test]
    //    public IEnumerator FovLineOfSightBlocked_RiskIsZero()
    //    {
    //        //Stationary guard
    //        List<Vector3> solutionPathRaw = new List<Vector3>()
    //        {
    //            new Vector3(-3,0,0),
    //            new Vector3(-2,0,1),
    //            new Vector3(-1,0,2)
    //        };
    //        var enemyPath = new GameObject("", new Type[] { typeof(PatrolPath) }).GetComponent<PatrolPath>();
    //        var obstacle = new GameObject("", new Type[] { typeof(Rigidbody2D), typeof(BoxCollider2D) })
    //            .GetComponent<BoxCollider2D>();
    //        yield return null;
    //        ISolutionPathRiskMeasurement solutionPathRiskMeasurement =
    //            new FieldOfViewRiskMeasure(
    //                new SolutionPath(solutionPathRaw),
    //                new List<PatrolPath>() { enemyPath },
    //               EnemyProperties,
    //                LayerMask.GetMask("Obstacles"));
    //        Assert.AreEqual(0, solutionPathRiskMeasurement.OverallRisk(1), 0.001f);
    //    }

    [UnityTest]
    public IEnumerator FovLineOfSightNonBlocked_RiskIsNonZero()
    {
        //Stationary guard
        List<Vector3> solutionPathRaw = new List<Vector3>()
        {
            new Vector3(-3,0,0),
            new Vector3(-2,0,1),
            new Vector3(-1,0,2)
        };

        var patrol = new Patrol(EnemyProperties,
            new List<Vector2>(), new FutureTransform()
            {
                Position = new Vector3(0.5f, 0, 0),
                Direction = new Vector3(-1.0f, 0, 0)
            });
        yield return null;
        ISolutionPathRiskMeasurement solutionPathRiskMeasurement =
            new FieldOfViewRiskMeasure(
                new SolutionPath(solutionPathRaw),
                new List<Patrol>() { patrol });

        Assert.AreNotEqual(0, solutionPathRiskMeasurement.OverallRisk(1));
        yield return null;
    }

    [Test]
    public void AngleRiskCost_IsCorrectnes()
    {
        FutureTransform player = new FutureTransform();
        player.Position = new Vector2(0, 0);
        player.Direction = new Vector2(0, 0);

        FutureTransform enemy = new FutureTransform();
        enemy.Position = new Vector2(0, 0);
        enemy.Direction = Vector2.right;
        Patrol patrol = new Patrol(EnemyProperties, new List<Vector2>(), enemy);

        ///
        FieldOfViewRiskMeasure solutionPathRiskMeasurement =
            new FieldOfViewRiskMeasure(
                new SolutionPath(new List<Vector3>()),
                new List<Patrol>() { patrol });

        player.Position = new Vector2(0, 0);

        //Directly on top should return 1
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, patrol));
        //Anywhere in visibility should return 1
        player.Position = Quaternion.AngleAxis(15, Vector3.forward) * Vector2.right;
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, patrol));
        player.Position = Quaternion.AngleAxis(-15, Vector3.forward) * Vector2.right;
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, patrol));
        //Directly opposite return minimum of function 0,5
        player.Position = Quaternion.AngleAxis(180, Vector3.forward) * Vector2.right;
        Assert.AreEqual(0.5f, solutionPathRiskMeasurement.RiskFromAngle(player, patrol));
        //At center of (180 - angleFov)/2.0f return 0.75
        //float n = (180 - patrolProperties.FOV / 2.0f) / 2.0f;
        float n = (EnemyProperties.FOV / 2.0f) + (180 - EnemyProperties.FOV / 2.0f) * 0.5f;
        player.Position = Quaternion.AngleAxis(n, Vector3.forward) * Vector2.right;
        Assert.AreEqual(0.75f, solutionPathRiskMeasurement.RiskFromAngle(player, patrol));
    }
}