using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StealthLevelIEMono))]
public class StealthLevelIEEditor : Editor
{
    private bool showMetaproperties;
    private bool showFundamental;

    private GUIStyle _bolded = GUIStyle.none;

    public override void OnInspectorGUI()
    {
        StealthLevelIEMono ie = (StealthLevelIEMono)target;
        //base.OnInspectorGUI();

        if (ie != null && ie.IsRunning)
        {
            //Additional info for the running algorithm

            //Todo visualizer user subject preference evaluator

            //- Best Fitness
            if (ie.GeneticAlgorithm.BestChromosome != null)
            {
                EditorGUILayout.LabelField("Best chromose fitness is:",
                    ie.GeneticAlgorithm.BestChromosome.Fitness.ToString());
            }

            //- Current Generation
            EditorGUILayout.LabelField("Current Generation: ",
                ie.GeneticAlgorithm.GenerationsNumber.ToString());

            if (GUILayout.Button("Run Generation"))
            {
                ie.DoGeneration();
            }
        }
        else
        {
            showMetaproperties = EditorGUILayout.Foldout(showMetaproperties, "Metaproperties");
            if (showMetaproperties)
            {
                ie.PopulationCount = EditorGUILayout.IntField("Population Count", ie.PopulationCount);
                ie.AimedGenerations = EditorGUILayout.IntField("Generaiton", ie.AimedGenerations);

                ie.CrossoverProb = EditorGUILayout.Slider("Crossover", ie.CrossoverProb, 0.0f, 1.0f);
                ie.MutationProb = EditorGUILayout.Slider("Mutation", ie.MutationProb, 0.0f, 1.0f);
            }

            showFundamental = EditorGUILayout.Foldout(showFundamental, "Fundamentals");
            if (showFundamental)
            {
                ie.PhenotypeEvaluator = (EvaluatorMono)EditorGUILayout.ObjectField(ie.PhenotypeEvaluator, typeof(EvaluatorMono), true);
                ie.LevelProperties = (LevelProperties)EditorGUILayout.ObjectField(ie.LevelProperties, typeof(LevelProperties), false);
                ie.Generator = (LevelPhenotypeGenerator)EditorGUILayout.ObjectField(ie.Generator, typeof(LevelPhenotypeGenerator), true);
                ie.Seed = EditorGUILayout.IntField("Seed: ", ie.Seed);
            }

            if (GUILayout.Button("Randmozise Seed"))
            {
                ie.RandomizeSeed();
            }
            if (GUILayout.Button("Setup"))
            {
                ie.Dispose();
                ie.SetupGA();
                ie.DoGeneration();
            }
        }
        if (GUILayout.Button("Dispose"))
        {
            ie.Dispose();
        }
    }
}