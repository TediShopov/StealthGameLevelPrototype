using GeneticSharp;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[ExecuteInEditMode]
public class InteractiveEvalutation : MonoBehaviour
{
    private InteractiveGeneticAlgorithm IEMono;
    private LevelChromosomeBase Chromosome;
    private bool _isSelected = false;

    public void Awake()
    {
        IEMono = this.GetComponentInParent<InteractiveGeneticAlgorithm>();
        if (IEMono != null)
            SceneView.duringSceneGui += this.DrawSelectionButton;
    }

    public void OnDestroy()
    {
        SceneView.duringSceneGui -= this.DrawSelectionButton;
    }

    public void SelectLevel()
    {
        //Find level object
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");

        //Get the level chromosome object and change get ref to
        var chromosomeMono = level?.GetComponentInChildren<LevelChromosomeMono>();
        if (chromosomeMono == null)
            throw new System.ArgumentException("No level chromose has been found");
        // its contents
        Chromosome = chromosomeMono.Chromosome;
        if (Chromosome == null)
            throw new System.ArgumentException("No acutal chromose contntes");

        if (IEMono == null)
            throw new System.ArgumentException("No interactive evolution found");

        Debug.Log($"Selected level {level.gameObject.name}");
        IEMono.SelectChromosome(Chromosome);
    }

    public void DrawSelectionButton(SceneView view)
    {
        Vector2 levelSize = IEMono.LevelProperties.LevelSize;

        Vector3 belowLevel = transform.position +
            (Vector3.down * (levelSize.y)) + new Vector3(0, 2.0f, 0);

        Vector2 buttonSize = new Vector2(levelSize.x, 2.0f);

        if (_isSelected)
        {
            Handles.Label(belowLevel, "Deselect");
        }
        else
        {
            Handles.Label(belowLevel, "Select");
        }

        if (Handles.Button(belowLevel, Quaternion.identity, 2.0f, 2.2f, Handles.RectangleHandleCap))
        {
            SelectLevel();
            _isSelected = !_isSelected;
        }
    }

    public void OnDrawGizmosSelected()
    {
        Handles.color = Color.red;
    }

    // Update is called once per frame
}