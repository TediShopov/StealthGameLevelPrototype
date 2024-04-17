using GeneticSharp;
using GeneticSharp.Domain;
using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class GAGenerationLogger
{
    private StealthLevelIEMono InteractiveGeneticMono;
    private InteractiveGeneticAlgorithm GA;
    public int LogEveryNGenerations = 1;
    private int LastLoggedGeneration = 0;

    public GAGenerationLogger(int everyN)
    {
        this.LogEveryNGenerations = everyN;
    }

    public void BindTo(StealthLevelIEMono stealthLevelIEMono)
    {
        InteractiveGeneticMono = stealthLevelIEMono;
        GA = InteractiveGeneticMono.GeneticAlgorithm;
        GA.GenerationRan += AppendEvaluationsEveryNGenerations;
        GA.TerminationReached += AppendAfterTermination;
        AlgorithmName = GetDefaultName();
    }

    private void AppendEvaluationsEveryNGenerations(object sender, EventArgs e)
    {
        int genNubmer = GA.Population.CurrentGeneration.Number;
        if (genNubmer <= 1)
        {
            string header = GetHeader(GA.Population.Generations);
            Helpers.SaveToCSV($"Tests/{AlgorithmName}.txt", header);
        }

        if (genNubmer % LogEveryNGenerations == 0)
        {
            int logsOccured = genNubmer / LogEveryNGenerations;
            AppendEvaluationToCsv(
                GA.Population.Generations.TakeLast(LogEveryNGenerations).ToList());
            LastLoggedGeneration = GA.GenerationsNumber;
        }
    }

    private void AppendAfterTermination(object sender, EventArgs e)
    {
        IList<Generation> generationToLog =
            GA.Population.Generations.Skip(LastLoggedGeneration).ToList();
        AppendEvaluationToCsv(generationToLog);
    }

    private string GetHeader(IList<Generation> generationsToOutput)
    {
        StringBuilder header = new StringBuilder();
        var bestInfo = (OTEPSLevelChromosome)GA.BestChromosome;
        header.Append($"Chromosome Hash,");
        foreach (var e in bestInfo.Measurements)
        {
            header.Append($"{e.Name} Evaluation,");
            header.Append($"{e.Name} Time,");
        }
        header.Remove(header.Length - 1, 0);
        return header.ToString();
    }

    public string AlgorithmName { get; set; }

    public string GetDefaultName()
    {
        return $"GEN_{InteractiveGeneticMono.AimedGenerations}" +
            $"_POP{InteractiveGeneticMono.GeneticAlgorithm.Population.MaxSize}" +
            $"_SZ{InteractiveGeneticMono.LevelProperties.LevelSize}_IndividualTimes";
    }

    //    private string AlgorithmName =>
    //        $"GEN_{InteractiveGeneticMono.AimedGenerations}" +
    //            $"_POP{InteractiveGeneticMono.PopulationCount}" +
    //            $"_SZ{InteractiveGeneticMono.LevelProperties.LevelSize}_IndividualTimes";

    private string GetUserPrefferenceModel()
    {
        string preferences = "";
        preferences += "Preference Model,";
        foreach (float weight in InteractiveGeneticMono.UserPreferences)
            preferences += weight + ",";
        return preferences;
    }

    private void AppendEvaluationToCsv(IList<Generation> generationsToOutput)
    {
        var GA = InteractiveGeneticMono.GeneticAlgorithm;

        string values = string.Empty;
        foreach (var gen in generationsToOutput)
        {
            values += GetUserPrefferenceModel();
            values += "\n";

            foreach (var c in gen.Chromosomes)
            {
                values += $"{c.GetHashCode()},";
                List<MeasureResult> info = ((OTEPSLevelChromosome)c).Measurements;
                if (info != null)
                {
                    foreach (var e in info)
                    {
                        values += $"{e.Value},";
                        values += $"{e.Time},";
                    }
                    values += "\n";
                }
            }
            values += "\n";
        }
        Helpers.SaveToCSV($"Tests/{AlgorithmName}.txt", "\n" + values);
    }

    private void AppendEvaluationToCsv()
    {
        AppendEvaluationToCsv(InteractiveGeneticMono.GeneticAlgorithm.Population.Generations);
    }
}