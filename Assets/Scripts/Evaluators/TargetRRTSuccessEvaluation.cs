using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using System;
using Codice.CM.WorkspaceServer;
using UnityEngine.Profiling;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using StealthLevelEvaluation;

public class TargetRRTSuccessEvaluation : MonoBehaviour, IFitness
{
    public GridPopulationManifestor GridPopulation;

    //private List<PhenotypeFitnessEvaluation> Evaluators = new List<PhenotypeFitnessEvaluation>();
    private PhenotypeFitnessEvaluation[] Evaluators;

    public double Evaluate(IChromosome chromosome)
    {
        var generator = GridPopulation.GetNextGenerator();
        if (generator == null) return 0;
        var levelChromose = (LevelChromosome)chromosome;
        generator.Generate(levelChromose);

        //Get all evaluators from  the prefab
        Evaluators = this.GetComponents<PhenotypeFitnessEvaluation>();
        FitnessInfo info = new FitnessInfo();
        var infoObj = FitnessInfoVisualizer.AttachInfo(generator.gameObject, info);

        foreach (var e in Evaluators)
        {
            e.Phenotype = generator.gameObject;
            info.FitnessEvaluations.Add(e);
            e.Evaluate();
        }

        //double evaluatedFitness = EvaluateDifficultyMeasureOfSuccesful(chromosome);
        levelChromose.FitnessInfo = info;

        //Attaching fitness evaluation information to the object itself
        return infoObj.FitnessEvaluations.Sum(x => x.Value);
    }
}