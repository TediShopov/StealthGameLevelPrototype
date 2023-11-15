using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngineInternal;

[RequireComponent(typeof(Grid))]
public class VoxelizedLevel : MonoBehaviour
{
    [HideInInspector] public Grid Grid;
    public LayerMask ObstacleLayerMask;
    public PolygonBoundary PolygonBoundary;
    public float Step;
    public float Iterations;
    public List<bool[,]> FutureGrids;
    //Debug
    public int LookAtGrid = 0;
    public int LookAtRange =1;
    [HideInInspector]  public List<PatrolPath> PatrolPaths;


    private Vector3Int _gridMin;
    private Vector3Int _gridMax;
    private bool[,] _staticObstacleGrid;
    // Start is called before the first frame update
    void Start()
    {
        Init();
        Helpers.TrackExecutionTime(Init, "Voxelized level grid");
    }

    private void Init()
    {
        this.Grid = GetComponent<Grid>();
        if (PolygonBoundary != null)
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            _gridMin = Grid.WorldToCell(levelBounds.min);
            _gridMax = Grid.WorldToCell(levelBounds.max);
        }
        FutureGrids = new List<bool[,]>();
        PatrolPaths = FindObjectsOfType<PatrolPath>().ToList();
        _staticObstacleGrid = GetStaticObstacleLevel();
        for (int i = 0; i < Iterations; i++)
        {
            var grid = VoxelizeFutureStateOfLevel(i * Step);
            FutureGrids.Add(grid);
        }
    }

    public Vector2 GetMinimumBound()  => this.Grid.GetCellCenterWorld(_gridMin); 
    public Vector2 GetMaximumBound()  => this.Grid.GetCellCenterWorld(_gridMax);
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
    public bool[,] VoxelizeFutureStateOfLevel(float future)
    {
        bool[,] futureGrid = Copy(_staticObstacleGrid);
        List<Vector2Int> DynamicObstacle=new List<Vector2Int>();
        //Calculate enemny future position
        foreach (var path in PatrolPaths)
        {
            DynamicObstacle.AddRange(MarkedCellsFromEnemy(path, future, futureGrid));
        }
        foreach (var obs in DynamicObstacle) 
        {
            int row = obs.y - _gridMin.y;
            int col = obs.x - _gridMin.x;
            futureGrid[row,col]=true;
        }
        return futureGrid;  
    }

    public List<Vector3Int> GetPossibleAffectedCells(PatrolPath path, float future) 
    {
        var toReturn = new List<Vector3Int>();

        var position = path.CalculateFuturePosition(future).Item1;
        var direction = path.CalculateFuturePosition(future).Item2;
        Bounds bounds= new Bounds();
        bounds.center = position;
//        bounds.center = position + direction * path.EnemyProperties.ViewDistance/2.0f;
//        bounds.Expand(path.EnemyProperties.ViewDistance*2.0f);
        
        Vector2 minLeft = position + Vector2.Perpendicular(direction)  * path.EnemyProperties.ViewDistance;
        Vector2 maxRight= position + Vector2.Perpendicular(-direction)  * path.EnemyProperties.ViewDistance;
        maxRight += direction * path.EnemyProperties.ViewDistance;
        bounds.Encapsulate(minLeft);
        bounds.Encapsulate(maxRight);

        Vector3Int min = Grid.WorldToCell(bounds.min);
        Vector3Int max = Grid.WorldToCell(bounds.max);
        for (int row = min.y; row < max.y; row++)
        {
            for (int col = min.x; col < max.x; col++)
            {

                toReturn.Add(new Vector3Int(col, row, 0));
            }

        }
        return toReturn;

    }
    public List<Vector2Int> MarkedCellsFromEnemy(PatrolPath path,float future, bool[,] staticLevel) 
    {
        var d = path.FieldOfView.EnemyProperties.ViewDistance;
        var positionDirecion = path.CalculateFuturePosition(future);
        var pos = positionDirecion.Item1;
        //Bounding Box for checking
        var listAffected = GetPossibleAffectedCells(path,future)
            .Where(x=> IsInBounds(x))
            .Where(x=> path.FieldOfView.TestCollision(Grid.GetCellCenterWorld(x),pos,positionDirecion.Item2))
            .Select(x=> (Vector2Int)x);
        return listAffected.ToList();


    }
    public bool IsInBounds(Vector3Int cellCoordinate) 
    {
        if (cellCoordinate.x >= _gridMin.x && cellCoordinate.y >= _gridMin.y && cellCoordinate.x <= _gridMax.x && cellCoordinate.y <= _gridMax.y)
            return true;
        return false;
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
        foreach (var path in PatrolPaths)
        {
            
            var l = GetPossibleAffectedCells(path, 1);
            foreach (var cell in l) 
            {
                Gizmos.DrawSphere(Grid.GetCellCenterWorld(cell), 0.1f);
            }
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
