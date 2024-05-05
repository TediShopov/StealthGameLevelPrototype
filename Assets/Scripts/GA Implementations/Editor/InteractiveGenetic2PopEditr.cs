using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveGenetic2Pop))]
public class InteractiveGenetic2PopEditr : InteractiveGeneticAlgorithmEditor
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

    public override void OnInspectorGUI()
    {
        try
        {
            InteractiveGenetic2Pop ie = (InteractiveGenetic2Pop)target;
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
            ((InteractiveGenetic2Pop)target).EndGA();
            throw;
        }
    }
}