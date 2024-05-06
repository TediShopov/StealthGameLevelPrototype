using Codice.Client.Common;
using GeneticSharp;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public interface ISubjectiveFitness : IFitness
{
    public UserPreferenceModel UserPreferenceWeights { get; set; }

    public double Reevaluate(IChromosome chromosome);
}

[ExecuteInEditMode]
public class InteractiveEvalutorMono : EvaluatorMono
{
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();
    public LevelProperties LevelProperties;

    public bool ToogleAestheticContribution;
    public bool ToogleEngagementContribution;

    [SerializeField]
    public EvaluatorMono ObjectiveFitness;

    public UserPreferenceModel UserPreferenceModel;

    [SerializeField] public GameObject OldEvaluatorPrefab = null;

    public void Awake()
    {
        this.UserPreferenceModel = new UserPreferenceModel(this, 1);
    }

    public void Update()
    {
        if (this.EvaluatorHolder == null) return;
        //Check if there is a different evaluator used
        if (this.OldEvaluatorPrefab == null)
        {
            //Reset the weight to new evaluator
            OldEvaluatorPrefab = this.EvaluatorHolder.gameObject;
            UserPreferenceModel.UpdateWeights(GetCountOfLevelProperties());
        }
        else if (this.EvaluatorHolder.gameObject.Equals(OldEvaluatorPrefab) == false)
        {
            //Reset the weight to new evaluator
            OldEvaluatorPrefab = this.EvaluatorHolder.gameObject;
            UserPreferenceModel.UpdateWeights(GetCountOfLevelProperties());
        }
    }

    public void Prepare()
    {
        this.UserPreferenceModel.Normalize();
    }

    //public InteractiveGeneticAlgorithm IE;

    public int GetCountOfLevelProperties()
    {
        var evaluatorsInPrefab = EvaluatorHolder.GetComponents<MeasureMono>();

        var countOfPropertyEvaluators =
            evaluatorsInPrefab
            .Where(x => x.GetCategory() == MeasurementType.PROPERTIES)
            .Count();
        return countOfPropertyEvaluators;
    }
    public string[] GetNamesOfLevelProperties()
    {
        var evaluatorsInPrefab = EvaluatorHolder.GetComponents<MeasureMono>();

        var countOfPropertyEvaluators =
            evaluatorsInPrefab
            .Where(x => x.GetCategory() == MeasurementType.PROPERTIES)
            .Select(x => x.GetName());
        return countOfPropertyEvaluators.ToArray();
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
        LevelChromosomeBase levelChromosome = TryGetValidLevelChromosome(chromosome);

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

        levelChromosome.Feasibility = true;
        foreach (var e in Evaluators)
        {
            e.Init(levelObject.gameObject);

            if (levelChromosome.Feasibility == true)
            {
                e.DoMeasure(levelObject.gameObject);
                if (e.IsTerminating)
                {
                    levelChromosome.Feasibility = false;
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

        float eval = -100;
        if (levelChromosome.Feasibility == true)
        {
            eval = 0;
            if (ToogleAestheticContribution)
            {
                eval += levelChromosome.AestheticScore;
            }
            if (ToogleEngagementContribution)
            {
                levelChromosome.EngagementScore =
                    (float)ObjectiveFitness.AttachToAndEvaluate(levelChromosome);
                eval += levelChromosome.EngagementScore;
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
        LevelChromosomeBase levelChromosome = TryGetValidLevelChromosome(chromosome);
        var levelObject = levelChromosome.Manifestation;

        //Keep old measuremens
        float oldAS = levelChromosome.AestheticScore;
        if (levelChromosome.AestheticProperties is not null)
        {
            levelChromosome.AestheticScore =
                levelChromosome.GetAestheticScore(UserPreferenceModel);
        }

        float eval = -100;
        //Combine engagment score and aesthetic score
        if (levelChromosome.Feasibility == true)
        {
            eval = 0;
            if (ToogleAestheticContribution)
            {
                eval += levelChromosome.AestheticScore;
            }
            if (ToogleEngagementContribution)
            {
                eval += levelChromosome.EngagementScore;
            }
        }

        if (Mathf.Approximately(eval, (float)levelChromosome.Fitness) == false)
        {
            Debug.Log($"Aesthetic score changed with {levelChromosome.AestheticScore - oldAS}");
            Debug.Log($"Fitness changed with {eval - levelChromosome.Fitness}");
        }
        return eval;
    }
}

public struct LevelMeasuredProperties
{
    public float SuccessChance;
    public float PathUniqeness;
}