using GeneticSharp.Domain;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveGeneticAlgorithm))]
public class InteractiveGeneticAlgorithmEditor : Editor
{
    private bool _independentRuns = true;
    private bool _showFundamental = true;
    private bool _showMetaproperties = true;

    public override void OnInspectorGUI()
    {
        try
        {
            InteractiveGeneticAlgorithm ie = (InteractiveGeneticAlgorithm)target;
            if (ie != null && ie.IsRunning)
            {
                AlgorithmActiveOnGUI(ie);
            }
            else
            {
                AlgorithmInactiveOnGUI(ie);
            }
            if (GUILayout.Button("Dispose"))
            {
                ie.EndGA();
            }
        }
        catch (System.Exception)
        {
            ((InteractiveGeneticAlgorithm)target).EndGA();
            throw;
        }
    }

    protected static void ShowFundamentals(InteractiveGeneticAlgorithm ie)
    {
        ie.PhenotypeEvaluator =
            (InteractiveEvalutorMono)EditorGUILayout.ObjectField(ie.PhenotypeEvaluator, typeof(InteractiveEvalutorMono), true);
        ie.LevelProperties = (LevelProperties)EditorGUILayout.ObjectField(ie.LevelProperties, typeof(LevelProperties), false);
        ie.Generator = (LevelPhenotypeGenerator)EditorGUILayout.ObjectField(ie.Generator, typeof(LevelPhenotypeGenerator), true);
        ie.Seed = EditorGUILayout.IntField("Seed: ", ie.Seed);
    }
    protected static void ShowMetaproperties(InteractiveGeneticAlgorithm ie)
    {
        ie.SyntheticGenerations =
            EditorGUILayout.IntField(
                "Synthetic Generaitons ",
            ie.SyntheticGenerations);

        ie.AimedGenerations = EditorGUILayout.IntField(
            "Generaiton",
            ie.AimedGenerations);

        ie.CrossoverProbability =
            EditorGUILayout.Slider(
                "Crossover",
                ie.CrossoverProbability,
                0.0f,
                1.0f);

        ie.MutationProbability =
            EditorGUILayout.Slider(
                "Mutation",
                ie.MutationProbability,
                0.0f,
                1.0f);
    }
    protected void AlgorithmActiveOnGUI(InteractiveGeneticAlgorithm ie)
    {
        //Additional info for the running algorithm

        //Show how much the synthetic user module has changed
        if (ie.PreferenceTracker is not null
            && ie.PreferenceTracker.PerGeneration.Count > 2)
        {
            EditorGUILayout.LabelField
               ($"Average Prefference Change: {ie.PreferenceTracker.ChangeSincePrevious()} / {ie.PhenotypeEvaluator.UserPreferenceModel.Step}");
            EditorGUILayout.LabelField
                ($"Average Prefference Change: {ie.PreferenceTracker.TotalChange()}");
        }

        //Todo visualizer user subject preference evaluator
        serializedObject.ApplyModifiedProperties();
        EditorApplication.update.Invoke();

        //Currently selected levels
        EditorGUILayout.LabelField("Selections Count: ", ie.GenerationSelecitons.Count.ToString());

        //- Best Fitness
        if (ie.BestChromosome != null)
        {
            EditorGUILayout.LabelField("Best chromose fitness is:",
                ie.BestChromosome.Fitness.ToString());
        }

        //- Current Generation
        EditorGUILayout.LabelField("Current Generation: ",
            ie.GenerationsNumber.ToString());

        if (GUILayout.Button("Run Generation"))
        {
            ie.DoGeneration();
        }
    }
    protected void AlgorithmInactiveOnGUI(InteractiveGeneticAlgorithm ie)
    {
        _showMetaproperties = EditorGUILayout.Foldout(_showMetaproperties, "Metaproperties");
        if (_showMetaproperties)
        {
            ShowMetaproperties(ie);
        }

        _showFundamental = EditorGUILayout.Foldout(_showFundamental, "Fundamentals");
        if (_showFundamental)
        {
            ShowFundamentals(ie);
        }

        ShowPopulationSetup();

        _independentRuns = EditorGUILayout.Foldout(_independentRuns, "Indepent Runs");
        if (_independentRuns)
        {
            ie.IndependentRuns = EditorGUILayout.IntField("Independent Runs", ie.IndependentRuns);
            if (GUILayout.Button("Run with synthetic model"))
            {
                ie.RunWithSyntheticModel();
            }
        }
        if (GUILayout.Button("Randmozise Seed"))
        {
            ie.RandomizeSeed();
        }
        if (GUILayout.Button("Setup"))
        {
            ie.StartGA();
        }
    }

    protected void ShowPopulationSetup()
    {
        serializedObject.Update();
        var ppl = serializedObject.FindProperty("PopulationPhenotypeLayout");
        EditorGUILayout.PropertyField(ppl);
        serializedObject.ApplyModifiedProperties();
    }
}