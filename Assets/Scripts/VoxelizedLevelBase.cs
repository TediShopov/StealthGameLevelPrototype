using Codice.Client.Common;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using UnityEngine;

public interface IFutureLevel
{
    public float Step { get; }
    public float Iterations { get; }

    public void Init();

    public Bounds GetBounds();

    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo);
}

[RequireComponent(typeof(Grid))]
public class VoxelizedLevelBase : MonoBehaviour, IFutureLevel
{
    [HideInInspector] public Grid Grid;
    [SerializeField] private float _step;
    [SerializeField] private float _iter;
    public List<NativeGrid<bool>> FutureGrids;

    public float Step => Step;

    public float Iterations => Iterations;

    public virtual void Init()
    {
        this.FutureGrids = new List<NativeGrid<bool>>();
    }

    public virtual NativeGrid<bool> GenerateFutureGrid(float future)
    { return new NativeGrid<bool>(this.Grid, new Bounds()); }

    public int GetFutureLevelIndex(float future)
    {
        return (int)Mathf.Clamp(Mathf.Ceil(future / this.Step), 0, this.Iterations - 1);
    }

    public bool CheckCellsColliding(List<Vector2Int> cells, float futureStart, float futureEnd)
    {
        int indexStart = GetFutureLevelIndex((float)futureStart);
        int indexEnd = GetFutureLevelIndex((float)futureEnd);
        int range = indexEnd - indexStart;
        List<NativeGrid<bool>> relevantFutureMaps;
        if (range == 0)
        {
            relevantFutureMaps = new List<NativeGrid<bool>>() { this.FutureGrids[indexEnd] };
        }
        else
        {
            relevantFutureMaps = this.FutureGrids.GetRange(indexStart, range);
        }

        foreach (var map in relevantFutureMaps)
        {
            foreach (var cell in cells)
            {
                var nativeCoord = map.GetNativeCoord(cell);
                //                if (map.IsInGrid(nativeCoord.y, nativeCoord.x) == false)
                //                    continue;
                //                if (map.Get(nativeCoord.y,nativeCoord.x))
                //                    return true;
                if (map.IsInGrid(nativeCoord.x, nativeCoord.y) == false)
                    continue;
                if (map.Get(nativeCoord.x, nativeCoord.y))
                    return true;
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

    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo)
    {
        Vector2Int startCell = (Vector2Int)this.Grid.WorldToCell(from);
        Vector2Int endCell = (Vector2Int)this.Grid.WorldToCell(to);
        var listOfRCells = VoxelizedLevelBase.GetCellsInLine(startCell, endCell);
        return this.CheckCellsColliding(listOfRCells.ToList(), timeFrom, timeTo);
    }

    public Bounds GetBounds()
    {
        Vector3 min = this.FutureGrids[0].WorldMin;
        min.z = 0;
        Vector3 max = this.FutureGrids[0].WorldMax;
        min.z = this.Iterations * Step;
        Bounds bounds = new Bounds();
        bounds.min = min;
        bounds.max = max;
        return bounds;
    }
}

//[RequireComponent(typeof(Grid))]
//public class DiscretizeLevelToGrid : VoxelizedLevelBase
//{
//    public LayerMask ObstacleLayerMask;
//    [HideInInspector] public List<PatrolPath> PatrolPaths;
//    public PolygonBoundary PolygonBoundary;
//    public int LookAtGrid = 0;
//    public int LookAtRange;
//
//    // Start is called before the first frame update
//    private void Start()
//    {
//        Helpers.LogExecutionTime(Init, "Discretize level to grid");
//    }
//
//    public override void Init()
//    {
//        this.Grid = GetComponent<Grid>();
//        if (PolygonBoundary != null)
//        {
//            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
//            _gridMin = Grid.WorldToCell(levelBounds.min);
//            _gridMax = Grid.WorldToCell(levelBounds.max);
//        }
//        PatrolPaths = FindObjectsOfType<PatrolPath>().ToList();
//        FutureGrids = new List<bool[,]>();
//        for (int i = 0; i < Iterations; i++)
//        {
//            var grid = GenerateFutureGrid(i * Step);
//            FutureGrids.Add(grid);
//        }
//    }
//
//    public override bool[,] GenerateFutureGrid(float future)
//    {
//        int rows = _gridMax.y - _gridMin.y;
//        int cols = _gridMax.x - _gridMin.x;
//        var futureGrid = new bool[rows, cols];
//        for (int row = 0; row < rows; row++)
//        {
//            for (int col = 0; col < cols; col++)
//            {
//                Vector3Int cellPosition = new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);
//                Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);
//                if (IsObstacleAtPosition(worldPosition) || IsEnemiesVisionOnPosition(worldPosition, future))
//                {
//                    futureGrid[row, col] = true;
//                }
//            }
//        }
//        return futureGrid;
//    }
//
//    private bool IsEnemiesVisionOnPosition(Vector3 worldPosition, float future)
//    {
//        foreach (var patrolPath in PatrolPaths)
//        {
//            var positionAndDirection = patrolPath.CalculateFuturePosition(future);
//            if (patrolPath.FieldOfView.TestCollision(worldPosition, positionAndDirection.Item1, positionAndDirection.Item2))
//            {
//                return true;
//            }
//        }
//        return false;
//    }
//
//    private bool IsObstacleAtPosition(Vector3 worldPosition)
//    {
//        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
//        Vector2 halfBoxSize = Grid.cellSize * 0.5f;
//
//        // Perform a BoxCast to check for obstacles in the area
//        RaycastHit2D hit = Physics2D.BoxCast(
//            origin: position2D,
//            size: halfBoxSize,
//            angle: 0f,
//            direction: Vector2.zero,
//            distance: 0.01f,
//            layerMask: ObstacleLayerMask
//        );
//
//        return hit.collider != null;
//    }
//
//    // Update is called once per frame
//    private void Update()
//    {
//    }
//
//    private void OnDrawGizmosSelected()
//    {
//        if (FutureGrids == null) return;
//        LookAtGrid = Mathf.Clamp(LookAtGrid, 0, FutureGrids.Count - 1);
//
//        Gizmos.color = Color.blue;
//        for (int i = LookAtGrid - LookAtRange; i < LookAtGrid + LookAtRange; i++)
//        {
//            var lookAtCurrent = i;
//            DebugDrawGridByIndex(lookAtCurrent);
//        }
//    }
//
//    public void DebugDrawGridByIndex(int lookAtCurrent)
//    {
//        int rows = _gridMax.y - _gridMin.y;
//        int cols = _gridMax.x - _gridMin.x;
//        for (int row = 0; row < rows; row++)
//        {
//            for (int col = 0; col < cols; col++)
//            {
//                if (FutureGrids[lookAtCurrent][row, col])
//                {
//                    Vector3Int cellPosition = new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);
//                    Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);
//
//                    worldPosition.z = lookAtCurrent * Step;
//                    Vector3 cellsize = Grid.cellSize;
//                    cellsize.z = Step;
//                    Gizmos.DrawCube(worldPosition, Grid.cellSize);
//                }
//            }
//        }
//    }
//}