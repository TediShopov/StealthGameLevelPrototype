using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngineInternal;

[RequireComponent(typeof(Grid))]
public class VoxelizedLevel : VoxelizedLevelBase
{
    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    private Collider2D _boundary;
    public int LookAtGrid = 0;
    public int LookAtRange =1;
    //    [HideInInspector]  public List<PatrolPath> PatrolPaths;
    public List<DynamicObstacleDiscretizer> Discrtizers;
    private bool[,] _staticObstacleGrid;
    // Start is called before the first frame update
    void Start()
    {
        Init();
        Helpers.LogExecutionTime(Init,"Voxelized level grid");
    }

    public override void Init()
    {
        this.Grid = GetComponent<Grid>();
        _boundary =Physics2D.OverlapPoint(this.transform.position, BoundaryLayerMask);
        if (_boundary != null)
        {
            Bounds levelBounds = _boundary.GetComponent<Collider2D>().bounds;
            _gridMin = Grid.WorldToCell(levelBounds.min);
            _gridMax = Grid.WorldToCell(levelBounds.max);
        }
        FutureGrids = new List<bool[,]>();
        Discrtizers = FindObjectsOfType<DynamicObstacleDiscretizer>().ToList();
        _staticObstacleGrid = GetStaticObstacleLevel();
        for (int i = 0; i < Iterations; i++)
        {
            var grid = GenerateFutureGrid(i * Step);
            FutureGrids.Add(grid);
        }
    }

    public bool[,] GetStaticObstacleLevel() 
    {
        var futureGrid = new bool[GetRows(),GetCols()];
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                Vector3 worldPosition = Grid.GetCellCenterWorld(GetVectorFromInternaclCoordinates(row,col));
                if (IsStaticObstacleAtPosition(worldPosition))
                {
                    futureGrid[row,col] = true;
                }
            }
        }
        return futureGrid;
    }
    public int GetRows() => _gridMax.y - _gridMin.y;
    public int GetCols() => _gridMax.x - _gridMin.x;

    public static T[,] Copy<T>(T[,] array)
    {
        int width = array.GetLength(0);
        int height = array.GetLength(1);
        T[,] copy = new T[width, height];

        for (int w = 0; w < width; w++)
        {
            for (int h = 0; h < height; h++)
            {
                copy[w, h] = array[w, h];
            }
        }

        return copy;
    }
    public override bool[,] GenerateFutureGrid(float future)
    {
        bool[,] futureGrid = Copy(_staticObstacleGrid);
        List<Vector2Int> DynamicObstacle=new List<Vector2Int>();
        //Calculate enemny future position
        foreach (var discretizer in Discrtizers)
        {
            var possiblyAffectedCells = discretizer.GetPossibleAffectedCells(this.Grid, future)
                .Where(x => IsInBounds(x))
                .Where(x => discretizer.IsObstacle(Grid.GetCellCenterWorld(x), future))
                .Select(x => (Vector2Int)x);

            DynamicObstacle.AddRange(possiblyAffectedCells);
        }
        foreach (var obs in DynamicObstacle) 
        {
            int row = obs.y - _gridMin.y;
            int col = obs.x - _gridMin.x;
            futureGrid[row,col]=true;
        }
        return futureGrid;  
    }


    private Vector3Int GetVectorFromInternaclCoordinates(int row, int col) => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0); 
    private bool IsStaticObstacleAtPosition(Vector3 worldPosition)
    {
        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        Vector2 halfBoxSize = Grid.cellSize * 0.5f;

        // Perform a BoxCast to check for obstacles in the area
        RaycastHit2D hit = Physics2D.BoxCast(
            origin: position2D,
            size: halfBoxSize,
            angle: 0f,
            direction: Vector2.zero,
            distance: 0.01f,
            layerMask: ObstacleLayerMask
        );

        return hit.collider != null;
    }

    // Update is called once per frame
    void Update()
    {


    }
    private void OnDrawGizmosSelected()
    {
        if (FutureGrids == null) return;
        LookAtGrid = Mathf.Clamp(LookAtGrid, 0, FutureGrids.Count-1);
        
        Gizmos.color = Color.blue;
        for (int i = LookAtGrid-LookAtRange; i < LookAtGrid+LookAtRange; i++)
        {
            if (i < 0 || i >= FutureGrids.Count) continue;
            var lookAtCurrent = i;
            DebugDrawGridByIndex( lookAtCurrent);

        }
    }
    public void DebugDrawGridByIndex(int lookAtCurrent)
    {
        int rows = _gridMax.y - _gridMin.y;
        int cols = _gridMax.x - _gridMin.x;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (FutureGrids[lookAtCurrent][row, col])
                {
                    Vector3Int cellPosition = new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);
                    Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);

                    worldPosition.z = lookAtCurrent * Step;
                    Vector3 cellsize = Grid.cellSize;
                    cellsize.z = Step;
                    Gizmos.DrawCube(worldPosition, Grid.cellSize);
                }
            }
        }
    }
}
