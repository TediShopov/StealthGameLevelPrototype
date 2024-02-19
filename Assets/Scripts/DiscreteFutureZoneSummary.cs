using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class DiscreteFutureZoneSummary : MonoBehaviour
{
    public bool RunOnStart = true;
    public bool DebugDraw = true;
    public LayerMask BoundaryLayerMask;
    public LayerMask ObstacleLayerMask;
    public Grid Grid;
    public List<NativeGrid<bool>> FutureGrids;

    private NativeGrid<bool> _staticObstacleGrid;
    private NativeGrid<bool> _visibilityCountGrid;

    // Start is called before the first frame update
    private void Start()
    {
        Run();
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

    private void Run()
    {
        //Get Future level instance
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        var futureLevel = level.GetComponentInChildren<IFutureLevel>(false);
        this.Grid = GetComponent<Grid>();
        _staticObstacleGrid = new NativeGrid<bool>(this.Grid, GetLevelBounds());
        _staticObstacleGrid.SetAll((row, col, ngrid) =>
        {
            if (IsStaticObstacleAtPosition(ngrid.GetWorldPosition(row, col)))
                return true;
            return false;
        });

        List<Vector2> allCells = new List<Vector2>();
        for (int i = 0; i < _staticObstacleGrid.GetRows(); i++)
        {
            for (int j = 0; j < _staticObstacleGrid.GetCols(); j++)
            {
                if (_staticObstacleGrid.Get(i, j) == false)
                    allCells.Add(_staticObstacleGrid.GetWorldPosition(i, j));
            }
        }

        var continuosFuturelevel = (ContinuosFutureLevel)futureLevel;
        float maxTime = continuosFuturelevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
        var notcolliding = continuosFuturelevel.AreNotCollidingDynamicDiscrete(allCells, 0, maxTime);
        _visibilityCountGrid = new NativeGrid<bool>(_staticObstacleGrid);
        _visibilityCountGrid.SetAll((x, y, _visibilityCountGrid) => false);
        foreach (var worldPos in notcolliding)
        {
            Vector2Int nativeCoord = _visibilityCountGrid.GetNativeCoord((Vector2Int)Grid.WorldToCell(new Vector3(worldPos.x, worldPos.y)));
            _visibilityCountGrid.Set(nativeCoord.x, nativeCoord.y, true);
        }

        //TODO OPTIMZATION FOR FEWER ITERATIONS
        //Get all ptrol path from the future level

        //Grab the maximum path legnth and double it (accountsfor bactracking)

        //Initialize discrete static grid
    }

    public void OnDrawGizmosSelected()
    {
        _visibilityCountGrid.ForEach((row, col) =>
        {
            if (_visibilityCountGrid.Get(row, col) == true)
            {
                Vector3 worldPosition = _visibilityCountGrid.GetWorldPosition(row, col);
                Vector3 cellsize = Grid.cellSize;
                Gizmos.DrawCube(worldPosition, Grid.cellSize);
            }
        });
    }
}