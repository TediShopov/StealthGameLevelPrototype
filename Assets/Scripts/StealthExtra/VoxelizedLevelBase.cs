using Codice.Client.Common;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using UnityEngine;

public interface IFutureLevel
{
    public float Step { get; }
    public float Iterations { get; }

    public float GetMaxSimulationTime();

    public void Init();

    public Bounds GetBounds();

    //Static collisions are collision with the level geomtry
    public bool IsStaticCollision(Vector3 from, Vector3 to);

    //Dynamic collisions are collisions with dynamic threats. E.g enemies
    public bool IsDynamicCollision(Vector3 from, Vector3 to);

    public bool IsColliding(Vector3 from, Vector3 to);

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

    public float GetMaxSimulationTime() => Step * Iterations;

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

    public bool IsColliding(Vector3 from, Vector3 to)
    {
        return IsColliding(from, to, from.z, to.z);
    }

    public bool IsStaticCollision(Vector3 from, Vector3 to)
    {
        throw new System.NotImplementedException();
    }

    public bool IsDynamicCollision(Vector3 from, Vector3 to)
    {
        throw new System.NotImplementedException();
    }
}