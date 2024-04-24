using GeneticSharp;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct UserPreferenceModel
{
    public float OverallPathRisk;
    public float SuccessChance;
    public float PathUniqeness;
}

public class InteractiveEvalutorMono : EvaluatorMono
{
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();
    public LevelProperties LevelProperties;

    public bool DoObjectiveDifficultyEvaluation;
    public StealthLevelIEMono IE;

    public int GetCountOfLevelProperties()
    {
        var evaluatorsInPrefab = EvaluatorHolder.GetComponents<MeasureMono>();

        var countOfPropertyEvaluators =
            evaluatorsInPrefab
            .Where(x => x.GetCategory() == MeasurementType.PROPERTIES)
            .Count();
        return countOfPropertyEvaluators;
    }

    public void AppendAestheticMeasureToObject(
        LevelChromosomeBase chromo,
        MeasureMono[] evals)
    {
        //Find all measurement of type properties
        var PropertyEvaluators = evals.Where(x => x.GetCategory() == MeasurementType.PROPERTIES)
            .Select(x => ((LevelPropertiesEvaluator)x).PropertyValue)
            .ToList();
        chromo.AestheticProperties = new PropertyMeasurements(PropertyEvaluators);

        //        if (chromo.AestheticProperties == null)
        //            chromo.AestheticProperties = new List<float>();

        //        chromo.AestheticProperties.Clear();
        //        foreach (var propertyEvaluator in PropertyEvaluators)
        //        {
        //            chromo.AestheticProperties.Add(propertyEvaluator.PropertyValue);
        //        }
    }

    public override double Evaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);
        var levelObject = levelChromosome.Phenotype;

        //Run the generators --> the game object is now tagged as level
        levelChromosome.PhenotypeGenerator.Generate(levelChromosome, levelObject);

        var evaluator = Instantiate(EvaluatorHolder, levelObject.transform);
        //Get all evaluators from  the prefab
        MeasureMono[] Evaluators = evaluator.GetComponents<MeasureMono>();

        //Order so level properties are measured first, followed by validators and
        // difficulty estimation
        Evaluators = Evaluators
            .Where(x => x.GetCategory() != MeasurementType.INITIALIZATION)
            .OrderByDescending(x => x.GetCategory() == MeasurementType.PROPERTIES)
            .ThenByDescending(x => x.IsValidator)
            .ToArray();

        foreach (var e in Evaluators)
        {
            e.Init(levelObject.gameObject);
            e.DoMeasure(levelObject.gameObject);
            if (e.IsTerminating)
                break;
        }

        var newMeasurement = Evaluators.Select(x => x.Result).ToArray();
        if (levelChromosome.Measurements == null)
        {
            levelChromosome.Measurements = new List<MeasureResult>();
        }
        levelChromosome.Measurements.AddRange(newMeasurement);

        Vector2 placement = new Vector2(IE.ExtraSpacing.x / 1.5f, 0);

        //Attach mono behaviour to visualize the measurements
        ChromoseMeasurementsVisualizer.AttachDataVisualizer(
            levelObject.gameObject,
            placement);

        Transform data = levelObject.transform.Find("Data");
        if (data is not null)
        {
            AppendAestheticMeasureToObject(levelChromosome, Evaluators);
        }

        //TODO Apply a proper fitness formula

        double eval = 0;

        //Attaching fitness evaluation information to the object itself

        if (Evaluators.Any(x => x.IsTerminating))
        {
            eval = 0.5f;
            levelObject.name += " Infeasible";
            levelObject.name += $" Fitness {eval}";
        }
        else
        {
            if (DoObjectiveDifficultyEvaluation)
            {
                var riskMeasure = levelObject.GetComponentInChildren<RiskMeasure>();
                var pathUniqueness = levelObject.GetComponentInChildren<PathZoneUniqueness>();
                var solver = levelObject.GetComponentInChildren<RRTSolverDifficultyEvaluation>();
            }
            else
            {
                for (int i = 0; i < levelChromosome.AestheticProperties.Count; i++)
                {
                    eval += levelChromosome.AestheticProperties[i]
                        * IE.UserPreferences.Current()[i];
                }
                eval *= 10;
            }
            levelObject.name += " Feasible";
            levelObject.name += $" Fitness {eval}";
        }

        levelChromosome.Measurements.Add(new MeasureResult()
        {
            Name = "Fitness",
            Category = MeasurementType.OVERALLFITNESS,
            Value = eval.ToString()
        });
        return eval;
    }
}

public struct LevelMeasuredProperties
{
    public float SuccessChance;
    public float PathUniqeness;
}