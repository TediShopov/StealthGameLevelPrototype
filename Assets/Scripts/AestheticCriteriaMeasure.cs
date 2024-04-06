using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AestheticCriteriaMeasure : MonoBehaviour
{
    public List<float> RealAestheticsMeasures = new List<float>();

    public void Measure(GameObject leve)
    {
        RealAestheticsMeasures = MeasureLevelAesthetics(leve);
    }

    private List<float> MeasureLevelAesthetics(GameObject level)
    {
        var chromosome = level.GetComponentInChildren<LevelChromosomeMono>().Chromosome;

        if (chromosome != null)
        {
            return new List<float>()
            {
                (float)chromosome.GetGene(0).Value
            };
        }

        return new List<float>();
    }
}