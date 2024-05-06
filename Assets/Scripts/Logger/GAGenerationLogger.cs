using GeneticSharp;
using GeneticSharp.Domain;
using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[ExecuteInEditMode]
public class GAGenerationLogger : MonoBehaviour
{
    [SerializeReference, HideInInspector] private InteractiveGeneticAlgorithm _oldGA = null;
    [SerializeReference] protected InteractiveGeneticAlgorithm _ga = null;
    [SerializeField] public int LogEveryNGenerations = 1;
    [SerializeField, HideInInspector] private int LastLoggedGeneration = 0;

    public virtual void Update()
    {
        if (_ga != _oldGA)
        //if (_ga != null && _ga.Equals(_oldGA) == false)
        {
            if (_oldGA != null)
                UnbindFrom(_oldGA);

            _oldGA = _ga;

            if (_ga != null)
                BindTo(_ga);
        }
    }

    public GAGenerationLogger(int everyN)
    {
        this.LogEveryNGenerations = everyN;
    }

    public virtual void BindTo(InteractiveGeneticAlgorithm ga)
    {
        Debug.Log($"Bounded To Ga {ga.gameObject.name} ");
        ga.GenerationRan += AppendEvaluationsEveryNGenerations;
        ga.TerminationReached += AppendAfterTermination;
        AlgorithmName = GetDefaultName();
    }

    public virtual void UnbindFrom(InteractiveGeneticAlgorithm ga)
    {
        Debug.Log($"Unbound from {ga.gameObject.name}");
        ga.GenerationRan -= AppendEvaluationsEveryNGenerations;
        ga.TerminationReached -= AppendAfterTermination;
    }

    private void AppendEvaluationsEveryNGenerations(object sender, EventArgs e)
    {
        int genNubmer = _ga.Population.CurrentGeneration.Number;
        if (genNubmer <= 1)
        {
            string header = GetHeader(_ga.Population.Generations);
            Helpers.SaveToCSV($"Tests/{AlgorithmName}.txt", header);
        }

        if (genNubmer % LogEveryNGenerations == 0)
        {
            int logsOccured = genNubmer / LogEveryNGenerations;
            AppendEvaluationToCsv(
                _ga.Population.Generations.TakeLast(LogEveryNGenerations).ToList());
            LastLoggedGeneration = _ga.GenerationsNumber;
        }
    }

    private void AppendAfterTermination(object sender, EventArgs e)
    {
        IList<Generation> generationToLog =
            _ga.Population.Generations.Skip(LastLoggedGeneration).ToList();
        AppendEvaluationToCsv(generationToLog);
    }

    private string GetHeader(IList<Generation> generationsToOutput)
    {
        StringBuilder header = new StringBuilder();
        //var bestInfo = (LevelChromosomeBase)_ga.Population.CurrentGeneration.Chromosomes.First();
        var bestInfo = (LevelChromosomeBase)_ga.Population.CurrentGeneration.Chromosomes
            .First();

        header.Append($"Chromosome Hash,");

        foreach (var e in bestInfo.Measurements)
        {
            e.Value.DepthFirstSearch(x =>
            {
                header.Append($"{x.Name}({x.GetDepth()}), {x.Time}({x.GetDepth()}),");
            });
        }
        header.Remove(header.Length - 1, 0);
        return header.ToString();
    }

    public string AlgorithmName { get; set; }

    public string GetDefaultName()
    {
        return $"GEN_{_ga.AimedGenerations}" +
            $"_POP{_ga.Population.MaxSize}" +
            $"_SZ{_ga.LevelProperties.LevelSize}_IndividualTimes";
    }

    private string GetUserPrefferenceModel()
    {
        string preferences = "";
        //        preferences += "Preference Model,";
        //        foreach (float weight in _ga.UserPreferences.Weights)
        //            preferences += weight + ",";
        return preferences;
    }

    private void AppendEvaluationToCsv(IList<Generation> generationsToOutput)
    {
        string values = string.Empty;
        foreach (var gen in generationsToOutput)
        {
            values += GetUserPrefferenceModel();
            values += "\n";

            foreach (var c in gen.Chromosomes)
            {
                values += $"{c.GetHashCode()},";

                foreach (var e in ((LevelChromosomeBase)c).Measurements)
                {
                    e.Value.DepthFirstSearch(x =>
                    {
                        values += $"{x.Value}, {x.Time},";
                    });
                }
                values += "\n";

                //                List<MeasureResult> info =
                //                    ((OTEPSLevelChromosome)c).Measurements.Values.ToList();
                //                if (info != null)
                //                {
                //                    foreach (var e in info)
                //                    {
                //                        values += $"{e.Value},";
                //                        values += $"{e.Time},";
                //                    }
                //                    values += "\n";
                //                }
            }
            //            values += "\n";
        }
        Helpers.SaveToCSV($"Tests/{AlgorithmName}.txt", "\n" + values);
    }

    private void AppendEvaluationToCsv()
    {
        AppendEvaluationToCsv(_ga.Population.Generations);
    }
}