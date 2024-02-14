using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Grid))]
public class VoxelizedLevel : VoxelizedLevelBase
{
    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    private Collider2D _boundary;
    public int LookAtGrid = 0;
    public int LookAtRange = 1;

    //    [HideInInspector]  public List<PatrolPath> PatrolPaths;
    public List<DynamicObstacleDiscretizer> Discrtizers;

    //private bool[,] _staticObstacleGrid;
    private NativeGrid<bool> _staticObstacleGrid;

    public bool DebugDraw;

    // Start is called before the first frame update
    public List<DynamicObstacleDiscretizer> GetDiscretizersInLevel()
    {
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        return level.GetComponentsInChildren<DynamicObstacleDiscretizer>().ToList();
    }

    public Bounds GetLevelBounds()
    {
        var _boundary = Physics2D.OverlapPoint(this.transform.position, BoundaryLayerMask);
        if (_boundary != null)
        {
            return _boundary.gameObject.GetComponent<Collider2D>().bounds;
        }
        throw new NotImplementedException();
    }

    public override void Init()
    {
        Profiler.BeginSample("Voxelized Representation");
        this.Grid = GetComponent<Grid>();

        _staticObstacleGrid = new NativeGrid<bool>(this.Grid, GetLevelBounds());
        _staticObstacleGrid.SetAll((row, col, ngrid) =>
        {
            if (IsStaticObstacleAtPosition(ngrid.GetWorldPosition(row, col)))
                return true;
            return false;
        });

        FutureGrids = new List<NativeGrid<bool>>();
        Discrtizers = GetDiscretizersInLevel();
        //_staticObstacleGrid = GetStaticObstacleLevel();
        //Initialize intial grid from all static colliders. Assmed to be obstacles

        for (int i = 0; i < Iterations; i++)
        {
            var grid = GenerateFutureGrid(i * Step);
            FutureGrids.Add(grid);
        }
        Profiler.EndSample();
    }

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

    public override NativeGrid<bool> GenerateFutureGrid(float future)
    {
        NativeGrid<bool> futureGrid = new NativeGrid<bool>(_staticObstacleGrid);
        List<Vector2Int> DynamicObstacle = new List<Vector2Int>();
        //Calculate enemny future position
        foreach (var discretizer in Discrtizers)
        {
            var possiblyAffectedCells = discretizer.GetPossibleAffectedCells(this.Grid, future)
                //.Where(intCoord => futureGrid.IsInGrid(intCoord.y,intCoord.x))
                .Where(x => discretizer.IsObstacle(Grid.GetCellCenterWorld(x), future))
                .Select(x => (Vector2Int)x);

            DynamicObstacle.AddRange(possiblyAffectedCells);
        }

        foreach (var obsIntCoord in DynamicObstacle)
        {
            Vector2Int nativeCoord = futureGrid.GetNativeCoord(obsIntCoord);
            if (futureGrid.IsInGrid(nativeCoord.x, nativeCoord.y))
                futureGrid.Set(nativeCoord.x, nativeCoord.y, true);
        }
        return futureGrid;
    }

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
    private void Update()
    {
    }

    private void OnDrawGizmosSelected()
    {
        if (FutureGrids == null) return;
        if (DebugDraw == false) return;
        LookAtGrid = Mathf.Clamp(LookAtGrid, 0, FutureGrids.Count - 1);

        Gizmos.color = Color.blue;
        for (int i = LookAtGrid - LookAtRange; i < LookAtGrid + LookAtRange; i++)
        {
            if (i < 0 || i >= FutureGrids.Count) continue;
            var lookAtCurrent = i;
            DebugDrawGridByIndex(lookAtCurrent);
        }
    }

    public void DebugDrawGridByIndex(int lookAtCurrent)
    {
        NativeGrid<bool> currentGrid = FutureGrids[lookAtCurrent];
        currentGrid.ForEach((row, col) =>
        {
            if (currentGrid.Get(row, col) == true)
            {
                Vector3 worldPosition = currentGrid.GetWorldPosition(row, col);
                worldPosition.z = lookAtCurrent * Step;
                Vector3 cellsize = Grid.cellSize;
                cellsize.z = Step;
                Gizmos.DrawCube(worldPosition, Grid.cellSize);
            }
        });
    }
}