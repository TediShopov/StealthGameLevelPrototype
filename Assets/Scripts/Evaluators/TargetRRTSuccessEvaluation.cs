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

public class TargetRRTSuccessEvaluation : MonoBehaviour, IFitness
{
    public GridPopulationManifestor GridPopulation;

    public double Evaluate(IChromosome chromosome)
    {
        var generator = GridPopulation.GetNextGenerator();
        if (generator == null) return 0;
        var levelChromose = (LevelChromosome)chromosome;
        generator.Generate(levelChromose);
        StealthLevelEvaluation.PhenotypeFitnessEvaluation eval =
            new StealthLevelEvaluation.RiskMeasure(generator.gameObject);
        StealthLevelEvaluation.PhenotypeFitnessEvaluation relCovarageEval =
            new StealthLevelEvaluation.RelativeLevelCoverage(generator.gameObject);
        StealthLevelEvaluation.PhenotypeFitnessEvaluation overlappingCoveredArea =
            new StealthLevelEvaluation.RelativeFOVOverlap(generator.gameObject);
        //double evaluatedFitness = EvaluateDifficultyMeasureOfSuccesful(chromosome);
        var infoObj = FitnessInfoVisualizer.AttachInfo(generator.gameObject,
            new FitnessInfo(eval, relCovarageEval, overlappingCoveredArea));
        levelChromose.FitnessInfo = infoObj;

        //Attaching fitness evaluation information to the object itself
        return infoObj.FitnessEvaluations.Sum(x => x.Value);
    }
}