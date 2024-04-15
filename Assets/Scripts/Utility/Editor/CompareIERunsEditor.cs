using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CompareIERunsOfLevelSolvers))]
public class CompareIERunsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CompareIERunsOfLevelSolvers compareIERunsEditor =
            (CompareIERunsOfLevelSolvers)target;
        base.OnInspectorGUI();
        if (GUILayout.Button("Run Tests"))
        {
            compareIERunsEditor.RunTests();
        }
    }
}