using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StealthLevelIEMono))]
public class StealthLevelIEEditor : Editor
{
    private bool showMetaproperties = true;
    private bool showFundamental = true;
    private bool showLayout = true;
    private bool showLogging = true;

    private GUIStyle _bolded = GUIStyle.none;
    //private SerializedProperty SerializedPreferences;

    private void OnEnable()
    {
        //SerializedPreferences = serializedObject.FindProperty("UserPreferences");
    }

    private void ArrayGUI(SerializedObject obj, string name)
    {
        int no = obj.FindProperty(name + ".Array.size").intValue;
        EditorGUI.indentLevel = 3;
        int c = EditorGUILayout.IntField("Size", no);
        if (c != no)
            obj.FindProperty(name + ".Array.size").intValue = c;

        for (int i = 0; i < no; i++)
        {
            var prop = obj.FindProperty(string.Format("{0}.Array.data[{1}]", name, i));
            EditorGUILayout.PropertyField(prop);
        }
    }

    public override void OnInspectorGUI()
    {
        StealthLevelIEMono ie = (StealthLevelIEMono)target;
        //base.OnInspectorGUI();

        serializedObject.Update();
        if (ie.UserPreferences is not null)
        {
            foreach (var weight in ie.UserPreferences.Weights)
            {
                EditorGUILayout.LabelField(weight.ToString());
            }
        }

        ie.LevelManifestor =
            (LevelManifestor)EditorGUILayout.ObjectField(ie.LevelManifestor, typeof(LevelManifestor), true);

        //        EditorGUILayout.PropertyField(SerializedPreferences, true);

        //EditorGUILayout.PropertyField(SerializedPreferences, true);
        if (ie != null && ie.IsRunning)
        {
            //Additional info for the running algorithm

            //Show how much the synthetic user module has changed
            if (ie.PreferenceTracker is not null
                && ie.PreferenceTracker.PerGeneration.Count > 2)
            {
                EditorGUILayout.LabelField
                    ($"Average Prefference Change: {ie.PreferenceTracker.ChangeSincePrevious()} / {ie.Step}");
                EditorGUILayout.LabelField
                    ($"Average Prefference Change: {ie.PreferenceTracker.TotalChange()}");
            }

            //Todo visualizer user subject preference evaluator
            serializedObject.ApplyModifiedProperties();
            EditorApplication.update.Invoke();

            //Currently selected levels
            EditorGUILayout.LabelField("Selections Count: ", ie.GenerationSelecitons.Count.ToString());

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
                //ie.PopulationCount = EditorGUILayout.IntField("Population Count", ie.PopulationCount);

                ie.SyntheticGenerations = EditorGUILayout.IntField("Synthetic Generaitons ",
                    ie.SyntheticGenerations);
                ie.AimedGenerations = EditorGUILayout.IntField("Generaiton", ie.AimedGenerations);

                ie.CrossoverProb = EditorGUILayout.Slider("Crossover", ie.CrossoverProb, 0.0f, 1.0f);
                ie.MutationProb = EditorGUILayout.Slider("Mutation", ie.MutationProb, 0.0f, 1.0f);

                ie.Step = EditorGUILayout.Slider("Step", ie.Step, 0, 1);
            }

            showFundamental = EditorGUILayout.Foldout(showFundamental, "Fundamentals");
            if (showFundamental)
            {
                ie.PhenotypeEvaluator =
                    (InteractiveEvalutorMono)EditorGUILayout.ObjectField(ie.PhenotypeEvaluator, typeof(InteractiveEvalutorMono), true);
                ie.LevelProperties = (LevelProperties)EditorGUILayout.ObjectField(ie.LevelProperties, typeof(LevelProperties), false);
                ie.Generator = (LevelPhenotypeGenerator)EditorGUILayout.ObjectField(ie.Generator, typeof(LevelPhenotypeGenerator), true);
                ie.Seed = EditorGUILayout.IntField("Seed: ", ie.Seed);
            }

            showLayout = EditorGUILayout.Foldout(showLayout, "Layout");
            if (showLayout)
            {
                ie.ExtraSpacing = EditorGUILayout.Vector2Field("Extra Spacing: ", ie.ExtraSpacing);
                serializedObject.Update();
                var ppl = serializedObject.FindProperty("PopulationPhenotypeLayout");
                EditorGUILayout.PropertyField(ppl);
                serializedObject.ApplyModifiedProperties();
            }
            showLogging = EditorGUILayout.Foldout(showLogging, "Logging");
            if (showLogging)
            {
                //Todo visualizer user subject preference evaluator
                ie.LogMeasurements = EditorGUILayout.Toggle("Is Logging", ie.LogMeasurements);
                ie.LogEveryGenerations = EditorGUILayout.IntField("Log Every N Generations", ie.LogEveryGenerations);
                ie.IndependentRuns = EditorGUILayout.IntField("Independent Runs", ie.IndependentRuns);
                //so.Update();
                if (GUILayout.Button("Run with synthetic model"))
                {
                    //Debug.Log($"First value of weights {ie.UserPreferences[0]}");
                    ie.RunWithSyntheticModel();
                }
            }

            if (GUILayout.Button("Reset Weights"))
            {
                ie.RefreshPreferencesWeight();
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