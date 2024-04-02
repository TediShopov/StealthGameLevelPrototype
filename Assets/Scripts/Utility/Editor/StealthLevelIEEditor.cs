using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StealthLevelIEMono))]
public class StealthLevelIEEditor : Editor

{
    public override void OnInspectorGUI()
    {
        StealthLevelIEMono ie = (StealthLevelIEMono)target;
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("I Did it!");
        if (GUILayout.Button("Randmozise Seed"))
        {
            ie.RandomizeSeed();
        }
        if (GUILayout.Button("Setup"))
        {
            ie.SetupGA();
        }
        if (GUILayout.Button("Run Generation"))
        {
            ie.DoGeneration();
        }
        if (GUILayout.Button("Dispose"))
        {
            ie.Dispose();
        }
        if (GUILayout.Button("Run"))
        {
            Debug.Log("Run");
        }
    }
}