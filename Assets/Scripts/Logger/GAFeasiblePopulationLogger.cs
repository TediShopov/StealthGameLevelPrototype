using GeneticSharp;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
public class GAFeasiblePopulationLogger : GAGenerationLogger
{
    public GAFeasiblePopulationLogger() : base(1)
    {
    }
    public override void Update()
    {
        base.Update();
    }
    public override void BindTo(InteractiveGeneticAlgorithm ga)
    {
        UnityEngine.Debug.Log($"Bounded To Ga {ga.gameObject.name} ");
        ga.AfterEvaluationStep += AppendGenFitness;
    }
    public override void UnbindFrom(InteractiveGeneticAlgorithm ga)
    {
        UnityEngine.Debug.Log($"Unbound from {ga.gameObject.name}");
        ga.AfterEvaluationStep -= AppendGenFitness;
    }

    private void AppendGenFitness(object sender, EventArgs e)
    {
        int genNubmer = _ga.Population.CurrentGeneration.Number;
        if (genNubmer <= 1)
        {
            string header = GetHeader();
            Helpers.SaveToCSV($"Tests/{GetFilename()}.txt", header);
        }

        if (genNubmer % LogEveryNGenerations == 0)
        {
            int logsOccured = genNubmer / LogEveryNGenerations;
            AppendLog(_ga.Population.CurrentGeneration);
        }
    }

    private void AppendLog(Generation generation)
    {
        StringBuilder stringBuilder = new StringBuilder();
        var feasiblePop = generation.Chromosomes
            .Where(x => ((LevelChromosomeBase)x).Feasibility == true)
            .OrderBy(x => x.Fitness);
        foreach (var feasible in feasiblePop)
        {
            stringBuilder.AppendLine($"{generation.Number},{feasible.Fitness}");
        }
        Helpers.SaveToCSV($"Tests/{GetFilename()}.txt", stringBuilder.ToString());
    }

    public string GetFilename()
    {
        return $"PFF_GA{_ga.AimedGenerations}_SZ{_ga.Population.MinSize}_{_ga.PhenotypeEvaluator.name}";
    }
    public string GetHeader()
    {
        return "GEN, FITNESS\n";
    }
}