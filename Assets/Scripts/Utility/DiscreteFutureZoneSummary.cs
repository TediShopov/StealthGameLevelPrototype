using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class DiscreteFutureZoneSummary : MonoBehaviour
{
    //   public bool RunOnStart = true;
    //    public bool DebugDraw = true;
    //    public LayerMask BoundaryLayerMask;
    //    public LayerMask ObstacleLayerMask;
    //    public Grid Grid;
    //    public List<NativeGrid<bool>> FutureGrids;
    //
    //    private NativeGrid<bool> _staticObstacleGrid;
    //    private NativeGrid<bool> _visibilityCountGrid;
    //    public HashSet<Vector2Int> UniqueVisibleCells;
    //    public BoundsInt boundsInt;
    //
    //    // Start is called before the first frame update
    //    private void Start()
    //    {
    //        Run();
    //    }
    //
    //    private int DiscreteNotCollidingCells(ContinuosFutureLevel futureLevel, ref NativeGrid<bool> staticObstacle)
    //    {
    //        List<Vector2> allCells = new List<Vector2>();
    //        for (int i = 0; i < staticObstacle.Rows; i++)
    //        {
    //            for (int j = 0; j < staticObstacle.Cols; j++)
    //            {
    //                if (staticObstacle.Get(i, j) == false)
    //                    allCells.Add(staticObstacle.GetWorldPosition(i, j));
    //            }
    //        }
    //
    //        float maxTime = futureLevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
    //        var notcolliding = futureLevel.AreNotCollidingDynamicDiscrete(allCells, 0, maxTime);
    //        var _visibilityCountGrid = new NativeGrid<bool>(staticObstacle);
    //        _visibilityCountGrid.SetAll((x, y, _visibilityCountGrid) => false);
    //        foreach (var worldPos in notcolliding)
    //        {
    //            Vector2Int nativeCoord = _visibilityCountGrid.GetNativeCoord((Vector2Int)Grid.WorldToCell(new Vector3(worldPos.x, worldPos.y)));
    //            _visibilityCountGrid.Set(nativeCoord.x, nativeCoord.y, true);
    //        }
    //        return notcolliding.Count;
    //    }
    //
    //    private int DiscreteCollidingCells(ContinuosFutureLevel futureLevel, Bounds levelBounds)
    //    {
    //        boundsInt = new BoundsInt();
    //        boundsInt.min = Grid.WorldToCell(levelBounds.min);
    //        boundsInt.max = Grid.WorldToCell(levelBounds.max);
    //
    //        int maxCells = (boundsInt.max.x - boundsInt.min.x) * (boundsInt.max.y - boundsInt.min.y);
    //
    //        float maxTime = futureLevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
    //        UniqueVisibleCells = futureLevel.UniqueVisibleCells(Grid, 0, maxTime, 0.1f);
    //
    //        return UniqueVisibleCells.Count;
    //    }
    //
    //    private void Run()
    //    {
    //        //Get Future level instance
    //        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
    //        var futureLevel = level.GetComponentInChildren<IFutureLevel>(false);
    //        this.Grid = GetComponent<Grid>();
    //        _staticObstacleGrid = new NativeGrid<bool>(this.Grid, Helpers.GetLevelBounds(level));
    //        int count = DiscreteCollidingCells((ContinuosFutureLevel)futureLevel, Helpers.GetLevelBounds(level));
    //        //        _staticObstacleGrid = new NativeGrid<bool>(this.Grid, Helpers.GetLevelBounds(level));
    //        //        _staticObstacleGrid.SetAll((row, col, ngrid) =>
    //        //        {
    //        //            if (Helpers.IsColidingCell(ngrid.GetWorldPosition(row, col), Grid.cellSize, ObstacleLayerMask))
    //        //                return true;
    //        //            return false;
    //        //        });
    //        //
    //        //        List<Vector2> allCells = new List<Vector2>();
    //        //        for (int i = 0; i < _staticObstacleGrid.Rows; i++)
    //        //        {
    //        //            for (int j = 0; j < _staticObstacleGrid.Cols; j++)
    //        //            {
    //        //                if (_staticObstacleGrid.Get(i, j) == false)
    //        //                    allCells.Add(_staticObstacleGrid.GetWorldPosition(i, j));
    //        //            }
    //        //        }
    //        //
    //        //        var continuosFuturelevel = (ContinuosFutureLevel)futureLevel;
    //        //        float maxTime = continuosFuturelevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
    //        //        var notcolliding = continuosFuturelevel.AreNotCollidingDynamicDiscrete(allCells, 0, maxTime);
    //        //        _visibilityCountGrid = new NativeGrid<bool>(_staticObstacleGrid);
    //        //        _visibilityCountGrid.SetAll((x, y, _visibilityCountGrid) => false);
    //        //        foreach (var worldPos in notcolliding)
    //        //        {
    //        //            Vector2Int nativeCoord = _visibilityCountGrid.GetNativeCoord((Vector2Int)Grid.WorldToCell(new Vector3(worldPos.x, worldPos.y)));
    //        //            _visibilityCountGrid.Set(nativeCoord.x, nativeCoord.y, true);
    //        //        }
    //
    //        //TODO OPTIMZATION FOR FEWER ITERATIONS
    //        //Get all ptrol path from the future level
    //
    //        //Grab the maximum path legnth and double it (accountsfor bactracking)
    //
    //        //Initialize discrete static grid
    //    }
    //
    //    public void OnDrawGizmosSelected()
    //    {
    //        if (boundsInt == null && UniqueVisibleCells is null) return;
    //        for (int y = boundsInt.min.y; y < boundsInt.max.y; y++)
    //        {
    //            for (int x = boundsInt.min.x; x < boundsInt.max.x; x++)
    //            {
    //                Vector2Int gridCoordinate = new Vector2Int(y, x);
    //                if (UniqueVisibleCells.Contains(gridCoordinate)) continue;
    //                Gizmos.DrawSphere(Grid.GetCellCenterWorld(new Vector3Int(gridCoordinate.x, gridCoordinate.y)), 0.1f);
    //            }
    //        }
    //
    //        //        _visibilityCountGrid.ForEach((row, col) =>
    //        //        {
    //        //            if (_visibilityCountGrid.Get(row, col) == true)
    //        //            {
    //        //                Vector3 worldPosition = _visibilityCountGrid.GetWorldPosition(row, col);
    //        //                Vector3 cellsize = Grid.cellSize;
    //        //                Gizmos.DrawCube(worldPosition, Grid.cellSize);
    //        //            }
    //        //        });
    //    }
}