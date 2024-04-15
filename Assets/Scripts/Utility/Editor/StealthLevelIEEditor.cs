using PlasticGui.Configuration;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StealthLevelIEMono))]
public class StealthLevelIEEditor : Editor
{
    private bool showMetaproperties;
    private bool showFundamental;
    private bool showLayout;
    private bool showLogging;

    private GUIStyle _bolded = GUIStyle.none;
    private SerializedObject so;
    private SerializedProperty SerializedPreferences;

    private void OnEnable()
    {
        so = new SerializedObject(target);
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
        SerializedPreferences = so.FindProperty("UserPreferences");
        ArrayGUI(so, "UserPreferences");
        //EditorGUILayout.PropertyField(SerializedPreferences, true);
        bool appleid = serializedObject.ApplyModifiedProperties();
        if (appleid)
        {
            Debug.Log("Applied");
        }
        if (ie != null && ie.IsRunning)
        {
            //Additional info for the running algorithm

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
                ie.PopulationCount = EditorGUILayout.IntField("Population Count", ie.PopulationCount);

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
            }
            showLogging = EditorGUILayout.Foldout(showLogging, "Logging");
            if (showLogging)
            {
                //Todo visualizer user subject preference evaluator
                ie.LogMeasurements = EditorGUILayout.Toggle("Is Logging", ie.LogMeasurements);
                ie.LogEveryGenerations = EditorGUILayout.IntField("Log Every N Generations", ie.LogEveryGenerations);
                ie.IndependentRuns = EditorGUILayout.IntField("Log Every N Generations", ie.IndependentRuns);
                //so.Update();
                if (GUILayout.Button("Run with synthetic model"))
                {
                    //Debug.Log($"First value of weights {ie.UserPreferences[0]}");
                    ie.RunWithSyntheticModel();
                }
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