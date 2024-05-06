using GeneticSharp;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
public class GAPropertyContributionLogger : GAGenerationLogger
{
    public GAPropertyContributionLogger() : base(1)
    {
    }
    private void Awake()
    {
        if (_ga != null)
            BindTo(_ga);
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
            .Select(x => ((LevelChromosomeBase)x))
            .Where(x => x.Feasibility == true)
            .OrderBy(x => x.Fitness);

        var weights = _ga.PhenotypeEvaluator.UserPreferenceModel.Weights;
        foreach (var feasible in feasiblePop)
        {
            stringBuilder.Append($"{generation.Number},");
            for (int i = 0; i < feasible.AestheticProperties.Count; i++)
            {
                stringBuilder.Append($"{weights[i]},{feasible.AestheticProperties[i]},");
            }
            stringBuilder.Append("\n");
        }
        Helpers.SaveToCSV($"Tests/{GetFilename()}.txt", stringBuilder.ToString());
    }

    public string GetFilename()
    {
        return $"PContrib_GA_{_ga.PhenotypeEvaluator.name}_{_ga.AimedGenerations}_SZ{_ga.Population.MinSize}";
    }
    public string GetHeader()
    {
        string[] names = _ga.PhenotypeEvaluator.GetNamesOfLevelProperties();
        string header = "GEN,";
        foreach (var name in names)
        {
            header += name + "_Weight," + name + "_Value" + ",";
        }
        header += "\n";
        return header;
    }
}