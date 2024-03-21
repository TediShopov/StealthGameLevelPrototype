using StealthLevelEvaluation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Provides negative fitness depening on how much spawn or destination areas
// are observed by guard FOVS
public class StartEndDestinationObserveTime : PhenotypeFitnessEvaluation
{
    private List<PatrolPath> PatrolPaths = new List<PatrolPath>();
    private IFutureLevel FutureLevel;
    private GameObject Start;
    private GameObject End;
    public AnimationCurve PenalizationCurve;
    public float WorstFitnessPenalty = 1000;

    public override float Evaluate()
    {
        float percetangeOfTimeFramesObserved = PercentageOfTimeFramesObserved(100);
        float fitness = -WorstFitnessPenalty * PenalizationCurve.Evaluate(percetangeOfTimeFramesObserved);
        return fitness;
    }

    private float PercentageOfTimeFramesObserved(float maxTime)
    {
        //
        int timeframesObserved = 0;
        int timesFrameSimulated = 0;
        List<BacktrackPatrolPath> simulatedPaths = PatrolPaths
            .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();
        float speed = PatrolPaths[0].EnemyProperties.Speed;
        float vd = PatrolPaths[0].EnemyProperties.ViewDistance;
        float fov = PatrolPaths[0].EnemyProperties.FOV;
        for (float time = 0; time <= maxTime; time += FutureLevel.Step)
        {
            timesFrameSimulated++;
            //Move all paths
            simulatedPaths.ForEach(x => x.MoveAlong(FutureLevel.Step * speed));
            for (int i = 0; i < simulatedPaths.Count; i++)
            {
                FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
                bool observerStart = FieldOfView.TestCollision(
                    Start.transform.position,
                    enemyFT,
                    fov,
                    vd,
                    LayerMask.GetMask("Obstacle"));
                bool observesEnd = FieldOfView.TestCollision(
                    End.transform.position,
                    enemyFT,
                    fov,
                    vd,
                    LayerMask.GetMask("Obstacle"));
                if (observerStart || observesEnd)
                {
                    timeframesObserved++;
                    break;
                }
            }
        }
        if (timesFrameSimulated == 0) { return 0; }
        return (float)timeframesObserved / (float)timesFrameSimulated;
    }

    public override void Init(GameObject phenotype)
    {
        Phenotype = phenotype;
        PatrolPaths = Phenotype.GetComponentsInChildren<PatrolPath>().ToList();
        FutureLevel = Phenotype.GetComponentInChildren<IFutureLevel>();
        Start = Phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
        End = Phenotype.GetComponentInChildren<WinTrigger>().gameObject;
    }
}