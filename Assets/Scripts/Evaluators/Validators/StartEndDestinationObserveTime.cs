using Mono.Cecil;
using StealthLevelEvaluation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Provides negative fitness depening on how much spawn or destination areas
// are observed by guard FOVS
public class StartEndDestinationObserveTime : MeasureMono
{
    private IFutureLevel FutureLevel;
    private GameObject Start;
    private GameObject End;
    public int TimeframesObserved = 0;
    public int TimesFrameSimulated = 0;

    public override MeasurementType GetCategory()
    {
        return MeasurementType.VALIDATION;
    }

    public override string GetName()
    {
        return "StartEndDestinationObserveTime";
    }

    protected override string Evaluate()
    {
        float percetangeOfTimeFramesObserved = PercentageOfTimeFramesObserved(100);
        return percetangeOfTimeFramesObserved.ToString();
    }

    //     private float PercentageOfTimeFramesObserved(float maxTime)
    //     {
    //         //
    //         int timeframesObserved = 0;
    //         int timesFrameSimulated = 0;
    //         List<BacktrackPatrolPath> simulatedPaths = PatrolPaths
    //             .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();
    //         float speed = PatrolPaths[0].EnemyProperties.Speed;
    //         float vd = PatrolPaths[0].EnemyProperties.ViewDistance;
    //         float fov = PatrolPaths[0].EnemyProperties.FOV;
    //         for (float time = 0; time <= maxTime; time += FutureLevel.Step)
    //         {
    //             timesFrameSimulated++;
    //             //Move all paths
    //             simulatedPaths.ForEach(x => x.MoveAlong(FutureLevel.Step * speed));
    //             for (int i = 0; i < simulatedPaths.Count; i++)
    //             {
    //                 FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
    //                 bool observerStart = FieldOfView.TestCollision(
    //                     Start.transform.position,
    //                     enemyFT,
    //                     fov,
    //                     vd,
    //                     LayerMask.GetMask("Obstacle"));
    //                 bool observesEnd = FieldOfView.TestCollision(
    //                     End.transform.position,
    //                     enemyFT,
    //                     fov,
    //                     vd,
    //                     LayerMask.GetMask("Obstacle"));
    //                 if (observerStart || observesEnd)
    //                 {
    //                     timeframesObserved++;
    //                     break;
    //                 }
    //             }
    //         }
    //         if (timesFrameSimulated == 0) { return 0; }
    //         return (float)timeframesObserved / (float)timesFrameSimulated;
    //    }
    private float PercentageOfTimeFramesObserved(float maxTime)
    {
        TimeframesObserved = 0;
        TimesFrameSimulated = 0;

        var contLevel = (ContinuosFutureLevel)FutureLevel;
        var simulation = contLevel.GetFullSimulation();

        while (simulation.IsFinished == false)
        {
            foreach (var threat in simulation.Threats)
            {
                if (threat.TestThreat(Start.transform.position)
                    || threat.TestThreat(End.transform.position))
                {
                    TimeframesObserved++;
                    IsTerminating = true;
                    return 0.0f;
                }
            }
            TimesFrameSimulated++;
            simulation.Progress();
        }
        if (TimesFrameSimulated == 0) { return 0; }
        return (float)TimeframesObserved / (float)TimesFrameSimulated;
    }

    public override void Init(GameObject phenotype)
    {
        IsValidator = true;
        Phenotype = phenotype;
        FutureLevel = Phenotype.GetComponentInChildren<IFutureLevel>();
        Start = Phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
        End = Phenotype.GetComponentInChildren<WinTrigger>().gameObject;
    }
}