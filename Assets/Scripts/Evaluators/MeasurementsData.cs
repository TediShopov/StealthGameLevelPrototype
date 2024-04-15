using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StealthLevelEvaluation;
using UnityEditor;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

public class ChromoseMeasurementsVisualizer : MonoBehaviour
{
    public static void AttachDataVisualizer(GameObject to, Vector2 relPos)
    {
        var obj = new GameObject("FitnessInfo", new System.Type[] { typeof(ChromoseMeasurementsVisualizer) });
        obj.transform.localPosition = relPos;
        obj.transform.SetParent(to.transform, false);
        var infoMono = obj.GetComponent<ChromoseMeasurementsVisualizer>();
        infoMono.levelChromosome = to.GetComponentInChildren<LevelChromosomeMono>().Chromosome;
    }

    public static void AttachDataVisualizer(GameObject to)
    {
        AttachDataVisualizer(to, Vector2.zero);
    }

    private LevelChromosomeBase levelChromosome;

    public string GetCategoryString(MeasurementType category)
    {
        if (category == MeasurementType.INITIALIZATION)
        {
            return "INITIALIZATION";
        }
        if (category == MeasurementType.PROPERTIES)
        {
            return "PROPERTIES";
        }
        if (category == MeasurementType.VALIDATION)
        {
            return "VALIDATION";
        }
        if (category == MeasurementType.DIFFICULTY)
        {
            return "DIFFICULTY";
        }
        if (category == MeasurementType.OVERALLFITNESS)
        {
            return "OVERALLFITNESS";
        }
        return "None";
    }

    public void OnDrawGizmosSelected()
    {
        string allEvals = "";
        MeasurementType previousMeasurementType = MeasurementType.INITIALIZATION;
        allEvals += "---" + GetCategoryString(previousMeasurementType) + "---" + "\n";
        foreach (var evaluation in levelChromosome.Measurements)
        {
            if (evaluation.Category != previousMeasurementType)
            {
                allEvals += "---" + GetCategoryString(evaluation.Category) + "---" + "\n";
            }

            allEvals += evaluation.ToString();
            previousMeasurementType = evaluation.Category;
        }
        Handles.Label(this.transform.position, allEvals.ToString());
    }
}