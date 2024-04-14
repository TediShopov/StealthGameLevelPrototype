using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StealthLevelEvaluation;
using UnityEditor;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

public class MeasurementsData
{
    public MeasurementsData(params MeasureResult[] measures)
    {
        this.FitnessEvaluations = measures.ToList();
    }

    public List<MeasureResult> FitnessEvaluations;
}

public class ChromoseMeasurementsVisualizer : MonoBehaviour
{
    //    public static FitnessInfo AttachDataVisualizer(GameObject to, )
    //    {
    //        var obj = new GameObject("FitnessInfo", new System.Type[] { typeof(FitnessInfo) });
    //        obj.transform.SetParent(to.transform, false);
    //        var info = obj.GetComponent<FitnessInfo>();
    //        info.FitnessEvaluations = evals.ToList();
    //        return info;
    //    }
    public static void AttachDataVisualizer(GameObject to)
    {
        var obj = new GameObject("FitnessInfo", new System.Type[] { typeof(ChromoseMeasurementsVisualizer) });
        obj.transform.SetParent(to.transform, false);
        var infoMono = obj.GetComponent<ChromoseMeasurementsVisualizer>();
        infoMono.levelChromosome = to.GetComponentInChildren<LevelChromosomeMono>().Chromosome;
    }

    private LevelChromosomeBase levelChromosome;

    public void OnDrawGizmosSelected()
    {
        string allEvals = "";
        foreach (var evaluation in levelChromosome.Measurements.FitnessEvaluations)
        {
            allEvals += evaluation.ToString();
        }
        Handles.Label(this.transform.position, allEvals.ToString());
    }
}