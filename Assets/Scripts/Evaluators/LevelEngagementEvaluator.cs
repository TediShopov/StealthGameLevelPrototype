using GeneticSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelEngagementEvaluator : EvaluatorMono
{
    [SerializeField] public float MaxSpeed;
    [SerializeField] public Vector2 StartPosition;
    [SerializeField] public Vector2 EndPosition;
    [SerializeReference, SubclassPicker] public RRT RRT;

    [SerializeField, Range(0, 1)]
    public float TargetSuccessRate;

    [HideInInspector] public List<RRT> RRTList;
    [SerializeField] public IFutureLevel FutureLevel;

    public int RRTAttemps = 15;

    public void SetupLevelSpecifics(LevelChromosomeBase levelChromosome)
    {
        GameObject levelManifestation = levelChromosome.Manifestation;

        StartPosition = Helpers.SafeGetComponentInChildren<CharacterController2D>
            (levelManifestation).gameObject.transform.position;
        StartPosition = levelChromosome.Manifestation.transform.InverseTransformPoint(StartPosition);

        EndPosition = Helpers.SafeGetComponentInChildren<WinTrigger>
            (levelManifestation).gameObject.transform.position;
        EndPosition = levelChromosome.Manifestation.transform.InverseTransformPoint(EndPosition);
        MaxSpeed = Helpers.SafeGetComponentInChildren<CharacterController2D>(levelManifestation)
            .MaxSpeed;
        FutureLevel = levelChromosome.Phenotype.FutureLevel;
    }

    // Start is called before the first frame update
    public override EvaluatorMono PrototypeComponent(GameObject to)
    {
        LevelEngagementEvaluator proto =
            AttachEvaluatorContainer(to).AddComponent<LevelEngagementEvaluator>();
        //Transfer only the max iteratation and the rrt base class
        proto.RRT = this.RRT;
        proto.TargetSuccessRate = this.TargetSuccessRate;
        return proto;
    }

    private void FillRRTList(GameObject manifestation)
    {
        RRTList = new List<RRT>();

        var rrtContainer = new GameObject($"{this.RRT.GetType().Name}");
        rrtContainer.transform.SetParent(this.transform);
        var rrtVisualizer =
            rrtContainer.AddComponent<RapidlyExploringRandomTreeVisualizer>();
        rrtVisualizer.RRT = this.RRT;

        for (int i = 0; i < RRTAttemps - 1; i++)
        {
            var rrtInstance = Instantiate(rrtVisualizer, this.transform);
            rrtInstance.Setup();
            rrtInstance.Run();
            RRTList.Add(rrtInstance.RRT);
        }
    }

    private static float GetRRTSuccessRate(List<RRT> rrtList)
    {
        return rrtList.Count(x => x.Succeeded()) / rrtList.Count();
    }

    private float GetMinimumRiskMeasure(List<RRT> rrtList)
    {
        float minRiskMeasure = float.MaxValue;
        foreach (var rrt in rrtList.Where(x => x.Succeeded()))
        {
            var riskMeasure = new FieldOfViewRiskMeasure(
                new SolutionPath(rrt.ReconstructPathToSolution()),
                FutureLevel.DynamicThreats.Where(x => x is Patrol)
                .Select(x => (Patrol)x).ToList()
                );
            minRiskMeasure = Mathf.Min(minRiskMeasure, riskMeasure.OverallRisk(FutureLevel.Step));
        }
        return minRiskMeasure;
    }

    private static float GetPathUniqunessScore(List<RRT> rrtList, LevelPhenotype phenotype)
    {
        var solutionPaths = rrtList.Where(x => x.Succeeded()).Select(x => x.ReconstructPathToSolution())
            .ToList();
        var zones = phenotype.Zones;
        List<List<int>> SeenPaths = new List<List<int>>();
        foreach (var rrt in rrtList.Where(x => x.Succeeded()))
        {
            List<List<int>> solutionPathsZones =
                solutionPaths
                .Select(x => GetPathVisitedZones(zones, x))
                .ToList();

            foreach (var path in solutionPathsZones)
            {
                if (SeenPaths.Any(x => ZoneAreEqual(x, path)) == false)
                    SeenPaths.Add(path);
            }
            return SeenPaths.Count;
        }
        return 0;
    }

    public static List<int> GetPathVisitedZones(
        NativeGrid<int> zones,
        List<Vector3> path)
    {
        if (zones == null)
            throw new System.ArgumentNullException(
                "Needs a floodfill algorithm to define level zeons");

        var zoneIndexList = new List<int>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int segmentA = (Vector2Int)zones.Grid.WorldToCell(path[i]);
            Vector2Int segmentB = (Vector2Int)zones.Grid.WorldToCell(path[i + 1]);
            Vector2Int[] cellsAlongLine =
                Helpers.GetCellsInLine(segmentA, segmentB);
            foreach (var cellInSegment in cellsAlongLine)
            {
                int currentZoneIndex = zones.Get(cellInSegment);
                //Add only if index of zone is not found previously in the array
                //                if (zoneIndexList.Count == 0)
                //                    zoneIndexList.Add(currentZoneIndex);
                //                else
                //                {
                //                    if (zoneIndexList[zoneIndexList.Count - 1] != currentZoneIndex)
                //                        zoneIndeGetRRTSuccessRatexList.Add(currentZoneIndex);
                //                }

                if (zoneIndexList.Any(x => x == currentZoneIndex) == false)
                {
                    zoneIndexList.Add(currentZoneIndex);
                }
            }
        }

        return zoneIndexList;
    }

    public static bool ZoneAreEqual(List<int> zoneA, List<int> zoneB)
    {
        return zoneA.SequenceEqual(zoneB);
    }

    public float SuccessRate;
    public float PathUniquness;
    public float MinRisk;

    public float CalculateSuccessRateScore(float successRate, float targetRate)
    {
        if (successRate == 0)
            return 0;
        float difference = Math.Abs(successRate - targetRate);
        float score = 1 - difference;
        //score = Math.Max(0, Math.Min(1, score));
        score = 1 / (1 + Mathf.Exp(-difference));
        return score;
    }

    public override double Evaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = (LevelChromosomeBase)chromosome;
        SetupLevelSpecifics(levelChromosome);
        FillRRTList(levelChromosome.Manifestation);

        SuccessRate = GetRRTSuccessRate(RRTList);
        PathUniquness = GetPathUniqunessScore(RRTList, levelChromosome.Phenotype);
        MinRisk = GetMinimumRiskMeasure(RRTList);

        //Calculate score from success rate
        if (SuccessRate == 0)
            return 0;

        float successRateScore = CalculateSuccessRateScore(SuccessRate, TargetSuccessRate);
        float pathUniqunesScore = Mathf.InverseLerp(0, 12, PathUniquness);

        //return (successRateScore + pathUniqunesScore + MinRisk) / 3;
        return (successRateScore + MinRisk) / 2;
    }
}