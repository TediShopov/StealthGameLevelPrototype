using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscreteSymmetry : LevelPropertiesEvaluator
{
    private NativeGrid<bool> LevelGrid;
    private Collider2D PlayerCollider;
    public LayerMask ObstacleLayerMask;

    public override void Init(GameObject phenotype)
    {
        base.Init(phenotype);
        Grid grid = phenotype.GetComponentInChildren<Grid>();
        var character = phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
        PlayerCollider = character.GetComponent<Collider2D>();
        LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(phenotype));
        LevelGrid.SetAll(SetObstacleGrid);
    }

    public bool SetObstacleGrid(int row, int col, NativeGrid<bool> ngrid)
    {
        //Return true if box cast did not collide with any obstacle
        return !Physics2D.BoxCast(
            ngrid.GetWorldPosition(row, col),
            PlayerCollider.bounds.size,
            0,
            Vector3.back, 0.1f,
            ObstacleLayerMask);
    }

    protected override float MeasureProperty()
    {
        int rows = LevelGrid.GetRows();
        int columns = LevelGrid.GetCols();
        int middleRow = LevelGrid.GetRows() / 2;

        float totalRowSymmetry = 0;
        for (int i = 0; i < middleRow; i++)
        {
            float rowSymmetryPercentage = RowMirrorPercentage(i, rows - 1 - i);
            totalRowSymmetry += rowSymmetryPercentage;
        }
        return totalRowSymmetry / LevelGrid.GetRows();
    }

    private float RowMirrorPercentage(int rowIndex, int otherIndex)
    {
        int syummetricalCells = 0;
        for (int j = 0; j < LevelGrid.GetCols(); j++)
        {
            if (LevelGrid.Get(rowIndex, j) == LevelGrid.Get(otherIndex, j))
            {
                syummetricalCells++;
            }
        }
        return (float)syummetricalCells / (float)LevelGrid.GetCols();  // mirrored
    }
}