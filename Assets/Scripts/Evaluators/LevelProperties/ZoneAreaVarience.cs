using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TODO change to invariance
public class ZoneAreaVarience : LevelPropertiesEvaluator
{
    private FloodfilledRoadmapGenerator Roadmap;
    private Dictionary<int, int> zoneIdToArea = new Dictionary<int, int>();
    private float MaxRelativeVarience = 3.0f;

    public override void Init(GameObject phenotype)
    {
        base.Init(phenotype);
        Roadmap = Phenotype.GetComponentInChildren<FloodfilledRoadmapGenerator>();
    }

    protected override float MeasureProperty()
    {
        if (Roadmap == null)
            return 0;

        zoneIdToArea.Clear();
        Roadmap.LevelGrid.ForEach((x, y) =>
        {
            int zoneId = Roadmap.LevelGrid.Get(x, y);
            if (zoneIdToArea.ContainsKey(zoneId))
                zoneIdToArea[zoneId]++;
            else
                zoneIdToArea[zoneId] = 1;
        });

        //Remove the area count of the surrounding zone
        zoneIdToArea.Remove(0);
        float per = Mathf.InverseLerp(0, MaxRelativeVarience, (float)CalculateRelativeVariance());
        return per;

        //return Mathf.Lerp(0, 3.0f, MaxRelativeVarience);
        //return (float)CalculateRelativeVariance();
    }

    public static double CalculateStandardDeviation(Dictionary<int, int> dict)
    {
        float mean = 0;
        foreach (var value in dict.Values)
        {
            mean += value;
        }
        mean /= dict.Count;

        double variance = 0;
        foreach (var value in dict.Values)
        {
            variance += Math.Pow(value - mean, 2);
        }
        variance /= dict.Count - 1;

        return Math.Sqrt(variance); // Standard deviation
    }

    public double StandardDeviation()
    {
        double avg = zoneIdToArea.Values.Average();
        var sum = zoneIdToArea.Values.Sum(v => Math.Pow(v - avg, 2));
        return Math.Sqrt(sum / zoneIdToArea.Values.Count - 1);

        //return Math.Sqrt(zoneIdToArea.Values.Average());
    }

    public double CalculateRelativeVariance()
    {
        double avg = zoneIdToArea.Values.Average();
        double standardDeviation = StandardDeviation();
        if (avg == 0) // Avoid division by zero
        {
            throw new ArgumentException("The mean cannot be zero when calculating relative variance.");
        }

        double coefficientOfVariation = (standardDeviation / avg); // In percentage
        return coefficientOfVariation;
    }
}