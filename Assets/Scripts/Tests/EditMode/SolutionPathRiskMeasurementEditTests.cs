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

public class SolutionPath : IFutureTransform
{
    private List<Vector3> solutionPath;

    public SolutionPath(List<Vector3> solutionPath)
    {
        this.solutionPath = solutionPath;
    }

    public float GetMaxTime()
    { return solutionPath[solutionPath.Count - 1].z; }

    public float GetPathLength()
    {
        float pathLength = 0;
        for (int i = 0; i < solutionPath.Count - 1; i++)
        {
            pathLength += Vector2.Distance(solutionPath[i], solutionPath[i + 1]);
        }
        return pathLength;
    }

    public FutureTransform GetFutureTransform(float time)
    {
        FutureTransform futureTransform = new FutureTransform();

        //Get the segment that has this time
        var segmentEndIndex = solutionPath.FindIndex(x => x.z >= time);
        Vector3 segmentEnd = solutionPath[segmentEndIndex];
        if (segmentEnd.z == time)
        {
            futureTransform.Position = segmentEnd;
            if (segmentEndIndex + 1 < solutionPath.Count)
            {
                futureTransform.Direction = (segmentEnd - solutionPath[segmentEndIndex + 1]).normalized;
            }
            return futureTransform;
        }
        if (segmentEndIndex < 0)
        {
            throw new ArgumentException("Solution doesnt take place at specified time");
        }
        Vector3 segmentStart = solutionPath[segmentEndIndex - 1];
        futureTransform.Position = Vector2.Lerp(segmentStart, segmentEnd, time);
        futureTransform.Direction = (segmentEnd - segmentStart).normalized;
        return futureTransform;
    }
}

public interface ISolutionPathRiskMeasurement
{
    public float RiskInTime(float time);

    public float OverallRisk(float step);
}

public class FieldOfViewSolutionRiskMeasurement : ISolutionPathRiskMeasurement
{
    public FieldOfViewSolutionRiskMeasurement(
        SolutionPath solutionPath,
        List<PatrolPath> enemyPatrols,
        DefaultEnemyProperties enemyProperties,
LayerMask mask
        )

    {
        SolutionPath = solutionPath;
        EnemyPatrols = enemyPatrols;
        DefaultEnemyProperties = enemyProperties;
        ObstacleLayerMask = mask;
    }

    public LayerMask ObstacleLayerMask { get; }
    public SolutionPath SolutionPath { get; set; }
    public DefaultEnemyProperties DefaultEnemyProperties { get; }
    public List<PatrolPath> EnemyPatrols { get; set; }

    public float OverallRisk(float step)
    {
        float accumulatedRisk = 0;
        float overallPassedTime = 0;
        while (overallPassedTime <= SolutionPath.GetMaxTime())
        {
            accumulatedRisk += RiskInTime(overallPassedTime);
            overallPassedTime += step;
        }
        //Normalize by path length
        accumulatedRisk /= SolutionPath.GetPathLength();
        return accumulatedRisk;
    }

    public float RiskInTime(float time)
    {
        float accumulatedRisk = 0;
        foreach (var e in EnemyPatrols)
        {
            accumulatedRisk += RiskOfEnemy(SolutionPath.GetFutureTransform(time), e.GetFutureTransform(time));
        }
        return accumulatedRisk;
    }

    public float RiskOfEnemy(FutureTransform player, FutureTransform enemy)
    {
        if (!Physics2D.Linecast(enemy.Position, player.Position, ObstacleLayerMask))
            return RiskFromAngle(player, enemy) / DistanceCubed(player, enemy);
        return 0;
    }

    public float DistanceCubed(FutureTransform player, FutureTransform enemy)
    {
        return Mathf.Pow(Vector2.Distance(enemy.Position, player.Position), 3);
    }

    public float RiskFromAngle(FutureTransform player, FutureTransform enemy)
    {
        Vector2 directionToPlayer = (player.Position - enemy.Position).normalized;
        float angle = Vector2.Angle(enemy.Direction, directionToPlayer);
        //Is in view range
        if (angle < DefaultEnemyProperties.FOV / 2.0f)
        {
            return 1;
        }
        float relAngularCost = Mathf.InverseLerp(180, DefaultEnemyProperties.FOV / 2.0f, angle);
        return Mathf.Lerp(0.5f, 1, relAngularCost);
    }
}

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
            new FieldOfViewSolutionRiskMeasurement(
                new SolutionPath(solutionPathRaw),
                new List<PatrolPath>(),
               EnemyProperties,
                LayerMask.GetMask("Obstacles"));
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
    //            new FieldOfViewSolutionRiskMeasurement(
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
        var enemyPath = new GameObject("", new Type[] { typeof(Rigidbody2D), typeof(PatrolPath) }).GetComponent<PatrolPath>();
        enemyPath.transform.position = new Vector3(0.5f, 0, 0);
        enemyPath.transform.forward = new Vector3(-1.0f, 0, 0);
        yield return null;
        yield return null;
        ISolutionPathRiskMeasurement solutionPathRiskMeasurement =
            new FieldOfViewSolutionRiskMeasurement(
                new SolutionPath(solutionPathRaw),
                new List<PatrolPath>() { enemyPath },
               EnemyProperties,
                LayerMask.GetMask("Obstacles"));
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

        ///
        FieldOfViewSolutionRiskMeasurement solutionPathRiskMeasurement =
            new FieldOfViewSolutionRiskMeasurement(
                new SolutionPath(new List<Vector3>()),
                new List<PatrolPath>(),
               EnemyProperties,
                LayerMask.GetMask("Obstacles"));

        player.Position = new Vector2(0, 0);
        //Directly on top should return 1
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, enemy));
        //Anywhere in visibility should return 1
        player.Position = Quaternion.AngleAxis(15, Vector3.forward) * Vector2.right;
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, enemy));
        player.Position = Quaternion.AngleAxis(-15, Vector3.forward) * Vector2.right;
        Assert.AreEqual(1.0f, solutionPathRiskMeasurement.RiskFromAngle(player, enemy));
        //Directly opposite return minimum of function 0,5
        player.Position = Quaternion.AngleAxis(180, Vector3.forward) * Vector2.right;
        Assert.AreEqual(0.5f, solutionPathRiskMeasurement.RiskFromAngle(player, enemy));
        //At center of (180 - angleFov)/2.0f return 0.75
        //float n = (180 - EnemyProperties.FOV / 2.0f) / 2.0f;
        float n = (EnemyProperties.FOV / 2.0f) + (180 - EnemyProperties.FOV / 2.0f) * 0.5f;
        player.Position = Quaternion.AngleAxis(n, Vector3.forward) * Vector2.right;
        Assert.AreEqual(0.75f, solutionPathRiskMeasurement.RiskFromAngle(player, enemy));
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator SolutionPathRiskMeasurementEditTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}