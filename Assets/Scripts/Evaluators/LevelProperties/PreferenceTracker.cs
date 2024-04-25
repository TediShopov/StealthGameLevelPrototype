using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeneticSharp;
using System.Linq;

//Keeps track of all the user preference model throught the generations
public class UserPrefereneceTracker : IObserver<IList<float>>
{
    public Dictionary<int, IList<float>> PerGeneration;
    public IGeneticAlgorithm GeneticAlgorithm;

    public UserPrefereneceTracker(
        IGeneticAlgorithm ga)
    {
        this.PerGeneration = new Dictionary<int, IList<float>>();
        this.GeneticAlgorithm = ga;
    }

    public void Update(IList<float> sub)
    {
        if (GeneticAlgorithm != null)
            PerGeneration.Add(GeneticAlgorithm.GenerationsNumber, sub);
    }

    public float TotalChange()
    {
        int maxRecordedGeneration = PerGeneration.Max(x => x.Key);
        return AveragePropertyDistance(0, maxRecordedGeneration);
    }

    public float ChangeSincePrevious()
    {
        int max = 0;
        int prevMax = 0;
        foreach (var keyValuePair in PerGeneration)
        {
            if (keyValuePair.Key > max)
            {
                prevMax = max;
                max = keyValuePair.Key;
            }
        }
        if (prevMax == 0 || max == 0)
            return 0.0f;

        return AveragePropertyDistance(prevMax, max);
    }

    public float AveragePropertyDistance(int genIndex, int genIndexOther)
    {
        return AveragePropertyDistance(
            PerGeneration[genIndex],
            PerGeneration[genIndexOther]
            );
    }

    public static float AveragePropertyDistance(
        IList<float> one,
        IList<float> two
        )

    {
        if (one.Count != two.Count) return 0.0f;
        float avgDitance = 0;
        for (int i = 0; i < one.Count; i++)
        {
            avgDitance += Mathf.Abs(one[i] - two[i]);
        }
        avgDitance /= (float)one.Count;
        return avgDitance;
    }
}