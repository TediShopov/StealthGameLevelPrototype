using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveGenetic2Pop))]
public class InteractiveGenetic2PopEditr : InteractiveGeneticAlgorithmEditor
{
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
            //((InteractiveGenetic2Pop)target).EndGA();
            throw;
        }
    }
}