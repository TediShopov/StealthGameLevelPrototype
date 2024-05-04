using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveEvalutorMono))]
public class InteractiveEvaluatorMonoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var interactiveEvalutor = (InteractiveEvalutorMono)target;
        if (GUILayout.Button("Reset To Default"))
        {
            interactiveEvalutor.UserPreferenceModel.
                SetToDefault();
        }
        if (GUILayout.Button("Normalize"))
        {
            interactiveEvalutor.UserPreferenceModel.
                Normalize();
        }
    }
}