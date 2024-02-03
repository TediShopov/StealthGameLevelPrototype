using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TargetRRTSuccessEvaluation : MonoBehaviour,IFitness
{
    public LevelPhenotypeGenerator Generator;
    public double Evaluate(IChromosome chromosome)
    {
        Generator.Generate((LevelChromosome)chromosome);
        var RRTVisualizers = Generator.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
        int successful = RRTVisualizers.Count(x=>x.RRT.Succeeded()==true);
        double successRate= (double)successful / (double)RRTVisualizers.Count();
        Generator.Dispose();
        return successRate;
    }
}
