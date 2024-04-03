using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveEvalutation))]
public class InteractiveEvaluationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        InteractiveEvalutation interactiveEvalutation = (InteractiveEvalutation)target;
        base.OnInspectorGUI();
        if (GUILayout.Button("Select"))
        {
            interactiveEvalutation.SelectLevel();
        }
    }
}