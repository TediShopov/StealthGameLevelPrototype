using GeneticSharp;
using GeneticSharp.Domain;
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

    public GAGenerationLogger(StealthLevelIEMono stealthLevelIEMono, int everyN)
    {
        InteractiveGeneticMono = stealthLevelIEMono;
        GA = InteractiveGeneticMono.GeneticAlgorithm;
        this.LogEveryNGenerations = everyN;
        GA.GenerationRan += AppendEvaluationsEveryNGenerations;
        GA.TerminationReached += AppendAfterTermination;
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
        var bestInfo = (LevelChromosome)GA.BestChromosome;
        header.Append($"Chromosome Hash,");
        foreach (var e in bestInfo.Measurements.FitnessEvaluations)
        {
            header.Append($"{e.Name} Evaluation,");
            header.Append($"{e.Name} Time,");
        }
        header.Remove(header.Length - 1, 0);
        return header.ToString();
    }

    private string AlgorithmName =>
        $"GEN_{InteractiveGeneticMono.AimedGenerations}" +
            $"_POP{InteractiveGeneticMono.PopulationCount}" +
            $"_SZ{InteractiveGeneticMono.LevelProperties.LevelSize}_IndividualTimes";

    private void AppendEvaluationToCsv(IList<Generation> generationsToOutput)
    {
        var GA = InteractiveGeneticMono.GeneticAlgorithm;

        string values = string.Empty;
        foreach (var gen in generationsToOutput)
        {
            foreach (var c in gen.Chromosomes)
            {
                values += $"{c.GetHashCode()},";
                MeasurementsData info = ((LevelChromosome)c).Measurements;
                if (info != null)
                {
                    foreach (var e in info.FitnessEvaluations)
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