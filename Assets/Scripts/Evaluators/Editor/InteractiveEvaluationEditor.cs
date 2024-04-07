using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InteractiveEvalutation))]
[ExecuteInEditMode]
public class InteractiveEvaluationEditor : Editor
{
    // Window has been selected
    private void OnEnable()
    {
        // Remove delegate listener if it has previously
        // been assigned.
        // eneView.duringSceneGui -= this.OnSceneGUI;
        //
        //        // Add (or re-add) the delegate.
        //SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    //    [DrawGizmo(GizmoType.InSelectionHierarchy)]
    //    public void OnSceneGUI()
    //    {
    //        InteractiveEvalutation interactiveEvalutation = (InteractiveEvalutation)target;
    //        Vector3 pos = Camera.current.transform.position;
    //
    //        Handles.BeginGUI();
    //        Vector2 levelSize = interactiveEvalutation.IEMono.LevelProperties.LevelSize;
    //
    //        Vector3 belowLevel = interactiveEvalutation.transform.position +
    //            Vector3.down * levelSize.y;
    //
    //        Vector2 buttonSize = new Vector2(levelSize.x, 2.0f);
    //
    //        if (Handles.Button(belowLevel, Quaternion.identity, 15, 15.2f, Handles.RectangleHandleCap))
    //        {
    //            interactiveEvalutation.SelectLevel();
    //        }
    //        Handles.EndGUI();
    //}

    //`        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    //`    public void OnSceneGUI()
    //`    {
    //`        InteractiveEvalutation interactiveEvalutation = (InteractiveEvalutation)target;
    //`        Vector2 levelSize = interactiveEvalutation.IEMono.LevelProperties.LevelSize;
    //`
    //`        Vector3 belowLevel = interactiveEvalutation.transform.position +
    //`            Vector3.down * levelSize.y;
    //`
    //`        Vector2 buttonSize = new Vector2(levelSize.x, 2.0f);
    //`
    //`        if (Handles.Button(belowLevel, Quaternion.identity, 2, 2.2f, Handles.RectangleHandleCap))
    //`        {
    //`            interactiveEvalutation.SelectLevel();
    //`        }
    //`    }

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