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

    public bool OnlyAesthetic;
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
    }

    public override double Evaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);

        var levelObject = levelChromosome.Manifestation;

        ////Run the generators --> the game object is now tagged as level

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

        bool infeasible = false;
        foreach (var e in Evaluators)
        {
            e.Init(levelObject.gameObject);

            if (infeasible == false)
            {
                e.DoMeasure(levelObject.gameObject);
                if (e.IsTerminating)
                {
                    infeasible = true;
                }
            }
        }

        var newMeasurements = Evaluators.Select(x => x.Result).ToArray();
        foreach (var newMeasure in newMeasurements)
        {
            levelChromosome.Measurements[newMeasure.Name] = newMeasure;
        }

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

        float eval = -100;
        if (infeasible == false)
        {
            if (OnlyAesthetic)
            {
                //Combine engagment score and aesthetic score
                eval = levelChromosome.AestheticScore;
            }
            else
            {
                eval = levelChromosome.AestheticScore + levelChromosome.EngagementScore;
            }
        }
        levelChromosome.AddOrReplace(
            new MeasureResult()
            {
                Name = "Fitness",
                Category = MeasurementType.OVERALLFITNESS,
                Value = eval.ToString()
            });
        return eval;
    }

    public double Reevaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = CheckValidLevelChromosome(chromosome);
        var levelObject = levelChromosome.Manifestation;

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
        float eval = -100;
        if (OnlyAesthetic)
        {
            eval = levelChromosome.AestheticScore;
        }
        else
        {
            eval = levelChromosome.AestheticScore + levelChromosome.EngagementScore;
        }

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
            var levelObject = chromosomeBase.Manifestation;
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