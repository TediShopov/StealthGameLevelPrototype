using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class FieldOfViewRiskMeasure : ISolutionPathRiskMeasurement
{
    public FieldOfViewRiskMeasure(
        SolutionPath solutionPath,
        IEnumerable<Patrol> enemyPatrols
        )

    {
        SolutionPath = solutionPath;
        EnemyPatrols = new List<Patrol>(enemyPatrols);
        foreach (var p in enemyPatrols)
        {
            p.Reset();
        }
    }

    public SolutionPath SolutionPath { get; set; }
    public IEnumerable<Patrol> EnemyPatrols { get; set; }

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
            e.TimeMove(time);
            accumulatedRisk += RiskOfEnemy(SolutionPath.GetFutureTransform(time), e);
        }
        return accumulatedRisk;
    }

    public float RiskOfEnemy(FutureTransform player, Patrol enemy)
    {
        if (enemy.TestThreat(player.Position)) return 0;
        return RiskFromAngle(player, enemy) / DistanceCubed(player, enemy);
    }

    public float DistanceCubed(FutureTransform player, Patrol enemy)
    {
        float normalizedDistance = Vector2.Distance(enemy.GetTransform().Position, player.Position) /
            enemy.AestheticProperties.ViewDistance;

        return Mathf.Pow(normalizedDistance, 3);
    }

    public float RiskFromAngle(FutureTransform player, Patrol enemy)
    {
        Vector2 directionToPlayer = (player.Position - enemy.GetTransform().Position).normalized;
        float angle = Vector2.Angle(enemy.GetTransform().Direction, directionToPlayer);
        //Is in view range
        if (angle < enemy.AestheticProperties.FOV / 2.0f)
        {
            return 1;
        }
        float relAngularCost = Mathf.InverseLerp(180, enemy.AestheticProperties.FOV / 2.0f, angle);
        return Mathf.Lerp(0.5f, 1, relAngularCost);
    }
}