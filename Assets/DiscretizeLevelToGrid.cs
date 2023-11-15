using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class DiscretizeLevelToGrid : MonoBehaviour
{
    [HideInInspector] public Grid Grid;

    public LayerMask ObstacleLayerMask;
    [HideInInspector]  public List<PatrolPath> PatrolPaths;
     public PolygonBoundary PolygonBoundary;
    public float Step;
    public float Iterations;
    public int LookAtGrid = 0;
    public int LookAtRange ;
    public List<bool[,]> FutureGrids;


    private Vector3Int _gridMin;
    private Vector3Int _gridMax;
    // Start is called before the first frame update
    void Start()
    {
        
        this.Grid = GetComponent<Grid>();
        if (PolygonBoundary != null) 
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            _gridMin = Grid.WorldToCell(levelBounds.min);
            _gridMax = Grid.WorldToCell(levelBounds.max);
        }
        PatrolPaths = FindObjectsOfType<PatrolPath>().ToList();
        FutureGrids = new List<bool[,]>();
        for (int i = 0;i<Iterations;i++) 
        {
            var grid = GetFutureGrid(i * Step);
            FutureGrids.Add(grid);
        }
        Debug.Log($"Future Grid Count");

        
    }
    public Vector2 GetMinimumBound()  => this.Grid.GetCellCenterWorld(_gridMin); 
    public Vector2 GetMaximumBound()  => this.Grid.GetCellCenterWorld(_gridMax); 
    
    public bool[,] GetFutureGrid(float future) 
    {

        int rows = _gridMax.y - _gridMin.y;
        int cols = _gridMax.x - _gridMin.x;
        var futureGrid = new bool[rows,cols];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Vector3Int cellPosition = new Vector3Int(col+_gridMin.x, row+_gridMin.y, 0);
                Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);
                if (IsObstacleAtPosition(worldPosition) || IsEnemiesVisionOnPosition(worldPosition,future ))
                {
                    futureGrid[row,col] = true;
                }
            }
        }
        return futureGrid;
    }
    private bool IsEnemiesVisionOnPosition(Vector3 worldPosition,float future) 
    {

        foreach (var patrolPath in PatrolPaths)
        {
            var positionAndDirection = patrolPath.CalculateFuturePosition(future);
            if (patrolPath.FieldOfView.TestCollision(worldPosition,positionAndDirection.Item1,positionAndDirection.Item2)) 
            {
                return true;
            }

        }
        return false;
    }

    private bool IsObstacleAtPosition(Vector3 worldPosition)
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
            var lookAtCurrent = i;
            DebugDrawGridByIndex( lookAtCurrent);

        }
    }

    public int GetFutureLevelIndex(float future) 
    {
        return (int)Mathf.Clamp(Mathf.Ceil(future / this.Step), 0, this.Iterations-1);
    }
    public bool CheckCellsColliding(List<Vector2Int> cells, float futureStart, float futureEnd) 
    {
        
        int indexStart = GetFutureLevelIndex((float)futureStart);
        int indexEnd = GetFutureLevelIndex((float)futureEnd);
        int range = indexEnd - indexStart;
        List<bool[,]> relevantFutureMaps = this.FutureGrids.GetRange(indexStart,range);

        foreach (var map in relevantFutureMaps) 
        {
            foreach (var cell in cells) 
            {
                int col = cell.x- _gridMin.x;
                int row = cell.y- _gridMin.y;
                if ((col < 0 || col >= (_gridMax.x-_gridMin.x)) || (row < 0 || row >= (_gridMax.y-_gridMin.y)))
                {
                    return true;
                }
                
                if (map[row, col]) 
                { 
                    return true;
                }
            }
            
        }
        return false;
    }
    // Function to get cells in a 2D grid that lie in a line
    public static Vector2Int[] GetCellsInLine(Vector2Int start, Vector2Int end)
    {
        Vector2Int[] cells = new Vector2Int[Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.y - start.y)) + 1];
        int i = 0;

        int x = start.x;
        int y = start.y;

        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);

        int sx = (start.x < end.x) ? 1 : -1;
        int sy = (start.y < end.y) ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            cells[i] = new Vector2Int(x, y);
            i++;

            if (x == end.x && y == end.y)
                break;

            int err2 = 2 * err;

            if (err2 > -dy)
            {
                err -= dy;
                x += sx;
            }

            if (err2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return cells;
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
