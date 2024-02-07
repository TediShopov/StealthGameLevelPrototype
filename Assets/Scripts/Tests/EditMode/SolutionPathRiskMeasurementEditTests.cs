using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.TestTools;

public class SolutionPathRiskMeasurementEditTests
{
    [SerializeField] public CharacterController2D CharacterController2D;

    /// <summary>
    ///
    /// </summary>
    /// <param name="solution">Vector2 for position and z paremeter used to store the future value</param>
    /// <param name="patrolPaths">List of patrol path object </param>
    /// <returns></returns>
    public float MeasureRisk(
        List<Vector3> solution,
        List<PatrolPath> patrolPaths)
    {
        if (patrolPaths.Count == 0) return 0;
        List<float> riskPerFuture = new List<float>(solution.Count);
        for (int i = 0; i <= riskPerFuture.Count; i++)
        {
            riskPerFuture[i] = MeasureRisk(solution[i], patrolPaths, solution[i].z);
        }
        return riskPerFuture.Sum() / riskPerFuture.Count;
    }

    public float MeasureRisk(Vector2 position, List<PatrolPath> patrolPaths, float future)
    {
        float maxRiskIFuture = 0;
        foreach (var patrolPath in patrolPaths)
        {
            Tuple<Vector2, Vector2> patrolPathPos = patrolPath.CalculateFuturePosition(future);
            float risk = Vector2.Distance(position, patrolPathPos.Item1);
            if (maxRiskIFuture < risk)
            {
                maxRiskIFuture = risk;
            }
        }
        return maxRiskIFuture;
    }

    [Test]
    public void NoPatrolPaths_RiskIsZero()
    {
        //Stationary guard
        List<Vector3> solutionPath = new List<Vector3>()
        {
            new Vector3(0,0,0),
            new Vector3(1,0,1),
            new Vector3(1,1,2)
        };
        float actual = MeasureRisk(
            solutionPath, new List<PatrolPath>());
        Assert.AreEqual(0, actual, 0.001f);
    }

    [Test]
    public void StationaryGuard_CorrectMeasurement()
    {
        //Stationary guard
        List<Vector3> solutionPath = new List<Vector3>()
        {
            new Vector3(0,0,0),
            new Vector3(1,0,1),
            new Vector3(2,0,2)
        };
        var standingPatrolPaths = new PatrolPath();
        standingPatrolPaths.transform.position = new Vector3(3, 0, 0);
        float actual = MeasureRisk(
            solutionPath, new List<PatrolPath>());
        Assert.AreEqual(2, actual, 0.001f);
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