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
public class VoxelizedLevel : VoxelizedLevelBase
{
    public LayerMask ObstacleLayerMask;
    public PolygonBoundary PolygonBoundary;
    public int LookAtGrid = 0;
    public int LookAtRange =1;
    [HideInInspector]  public List<PatrolPath> PatrolPaths;
    private bool[,] _staticObstacleGrid;
    // Start is called before the first frame update
    void Start()
    {
        Init();
        Helpers.LogExecutionTime(Init,"Voxelized level grid");
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
