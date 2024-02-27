using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EvaluatorPrefabSpawner : MonoBehaviour, IFitness
{
    public GridPopulationManifestor GridPopulation;
    public GameObject EvaluatorHolder;
    //private List<PhenotypeFitnessEvaluation> Evaluators = new List<PhenotypeFitnessEvaluation>();

    public double Evaluate(IChromosome chromosome)
    {
        var generator = GridPopulation.GetNextGenerator();
        if (generator == null) return 0;
        var levelChromose = (LevelChromosome)chromosome;
        generator.Generate(levelChromose);
        var evaluator = Instantiate(EvaluatorHolder, generator.transform);

        //Get all evaluators from  the prefab
        PhenotypeFitnessEvaluation[] Evaluators = evaluator.GetComponents<PhenotypeFitnessEvaluation>();
        var info = FitnessInfoVisualizer.AttachInfo(generator.gameObject, new FitnessInfo());
        foreach (var e in Evaluators)
        {
            e.Init(generator.gameObject);
            e.Evaluate();
            info.FitnessEvaluations.Add(e);
        }
        levelChromose.FitnessInfo = info;
        //Attaching fitness evaluation information to the object itself
        return info.FitnessEvaluations.Sum(x => x.Value);
    }
}