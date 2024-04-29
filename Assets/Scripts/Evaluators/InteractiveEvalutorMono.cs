using GeneticSharp;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[System.Serializable]
//public struct UserPreferenceModel
//{
//    public float OverallPathRisk;
//    public float SuccessChance;
//    public float PathUniqeness;
//}

public class InteractiveEvalutorMono : EvaluatorMono
{
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();
    public LevelProperties LevelProperties;

    public bool DoObjectiveDifficultyEvaluation;
    public UserPreferenceModel UserPreferenceModel;
    //public StealthLevelIEMono IE;

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

    //    //Given a chromosme that is level chromosome
    //    //and has a fitness value
    //    public double Reevaluate(IChromosome chromosome, List<MeasurementType> TypeToEval)
    //    {
    //        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);
    //        var levelObject = levelChromosome.Phenotype;
    //
    //        ////Run the generators --> the game object is now tagged as level
    //        var evaluator = Instantiate(EvaluatorHolder, levelObject.transform);
    //        //Get all evaluators from  the prefab
    //        MeasureMono[] Evaluators = evaluator.GetComponents<MeasureMono>();
    //
    //        //Order so level properties are measured first, followed by validators and
    //        // difficulty estimation
    //        Evaluators = Evaluators
    //            .Where(x => TypeToEval.Contains(x.GetCategory()))
    //            .OrderByDescending(x => x.GetCategory() == MeasurementType.PROPERTIES)
    //            .ThenByDescending(x => x.IsValidator)
    //            .ToArray();
    //
    //        foreach (var e in Evaluators)
    //        {
    //            e.Init(levelObject.gameObject);
    //            e.DoMeasure(levelObject.gameObject);
    //            if (e.IsTerminating)
    //                break;
    //        }
    //
    //        var newMeasurement = Evaluators.Select(x => x.Result).ToArray();
    //        //Replace old values with new ones
    //        foreach (var m in newMeasurement)
    //        {
    //            MeasureResult? oldMeasure =
    //                levelChromosome.Measurements.FirstOrDefault(x => x.Name.Equals(m.Name));
    //            if (oldMeasure.HasValue)
    //                oldMeasure = m;
    //            else
    //                levelChromosome.Measurements.Add(m);
    //        }
    //        Transform data = levelObject.transform.Find("Data");
    //        if (data is not null)
    //        {
    //            AppendAestheticMeasureToObject(levelChromosome, Evaluators);
    //        }
    //
    //        double eval = 0;
    //
    //        if (Evaluators.Any(x => x.IsTerminating))
    //        {
    //            if (levelChromosome.AestheticProperties is not null)
    //            {
    //                eval = levelChromosome.GetAestheticScore(UserPreferenceModel);
    //            }
    //            eval *= 10;
    //        }
    //        else
    //        {
    //            if (DoObjectiveDifficultyEvaluation)
    //            {
    //                var riskMeasure = levelObject.GetComponentInChildren<RiskMeasure>();
    //                var pathUniqueness = levelObject.GetComponentInChildren<PathZoneUniqueness>();
    //                var solver = levelObject.GetComponentInChildren<RRTSolverDifficultyEvaluation>();
    //            }
    //            else
    //            {
    //                if (levelChromosome.AestheticProperties is not null)
    //                {
    //                    eval = levelChromosome.GetAestheticScore(UserPreferenceModel);
    //                }
    //                eval *= 10;
    //            }
    //        }
    //        levelChromosome.Measurements.Add(new MeasureResult()
    //        {
    //            Name = "Fitness",
    //            Category = MeasurementType.OVERALLFITNESS,
    //            Value = eval.ToString()
    //        });
    //
    //        //Apend to name
    //
    //        return eval;
    //    }

    public override double Evaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);

        var levelObject = levelChromosome.Phenotype;

        ////Run the generators --> the game object is now tagged as level

        //levelChromosome.PhenotypeGenerator.Generate(levelChromosome, levelObject);

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

        //Vector2 placement = new Vector2(IE.ExtraSpacing.x / 1.5f, 0);

        Transform data = levelObject.transform.Find("Data");
        if (data is not null)
        {
            AppendAestheticMeasureToObject(levelChromosome, Evaluators);
        }

        if (levelChromosome.AestheticProperties is not null)
        {
            levelChromosome.AestheticScore =
                levelChromosome.GetAestheticScore(UserPreferenceModel);
        }
        AssigneEngagementScore(levelChromosome);

        //Combine engagment score and aesthetic score
        float eval = levelChromosome.AestheticScore + levelChromosome.EngagementScore;

        levelChromosome.Measurements.Add(new MeasureResult()
        {
            Name = "Fitness",
            Category = MeasurementType.OVERALLFITNESS,
            Value = eval.ToString()
        });

        //Apend to name

        return eval;
    }

    public double Reevaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);
        var levelObject = levelChromosome.Phenotype;

        //Keep old measuremens
        float oldAS = levelChromosome.AestheticScore;
        if (levelChromosome.AestheticProperties is not null)
        {
            levelChromosome.AestheticScore =
                levelChromosome.GetAestheticScore(UserPreferenceModel);
        }

        var evaluator = Instantiate(EvaluatorHolder, levelObject.transform);
        //Combine engagment score and aesthetic score
        float oldFitness = (float)levelChromosome.Fitness;
        float eval = levelChromosome.AestheticScore + levelChromosome.EngagementScore;

        if (Mathf.Approximately(eval, (float)levelChromosome.Fitness) == false)
        {
            Debug.Log($"Aesthetic score changed with {levelChromosome.AestheticScore - oldAS}");
            Debug.Log($"Fitness changed with {eval - levelChromosome.Fitness}");
        }
        //`MeasureResult? t = levelChromosome.Measurements
        //    .FirstOrDefault(x => x.Category == MeasurementType.OVERALLFITNESS);
        //if(t.HasValue)
        //    t.GetValueOrDefault() = levelChromosome.Fitness.ToString();
        return eval;
    }

    private void AssigneEngagementScore(LevelChromosomeBase chromosomeBase)
    {
        try
        {
            var levelObject = chromosomeBase.Phenotype;
            var riskMeasure = levelObject.GetComponentInChildren<RiskMeasure>();
            var pathUniqueness = levelObject.GetComponentInChildren<PathZoneUniqueness>();
            var solver = levelObject.GetComponentInChildren<RRTSolverDifficultyEvaluation>();

            chromosomeBase.EngagementScore =
                solver.Chance * riskMeasure.RiskMeasures.Min() * pathUniqueness.SeenPaths.Count();
        }
        catch (System.Exception)
        {
            chromosomeBase.EngagementScore = 0;
        }
    }
}

public struct LevelMeasuredProperties
{
    public float SuccessChance;
    public float PathUniqeness;
}