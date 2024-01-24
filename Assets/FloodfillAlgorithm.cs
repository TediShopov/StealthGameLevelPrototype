using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class FloodfillAlgorithm : MonoBehaviour
{
    public Grid Grid;
    public LayerMask ObstacleLayerMask;
    public PolygonBoundary PolygonBoundary;

    //Dictionary exposed to Unity editor
    public List<Collider2D> ColliderKeys;

    public List<Color> Colors;
    public bool DrawIntialBoundaryCells;

    private Vector3Int _gridMax;
    private Vector3Int _gridMin;

    //Transforms the unity grid to c# binary represenetaion of the level
    private int[,] LevelGrid;

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

    public int GetColliderIndex(Collider2D collider)
    {
        if (collider == null) return -1;
        int index = ColliderKeys.FindIndex(x => x.Equals(collider));
        if (index >= 0)
        {
            return index;
        }
        else
        {
            ColliderKeys.Add(collider);
            return ColliderKeys.Count - 1;
        }
    }

    public Color GetColorForValue(int index)
    {
        if (index >= 0)
        {
            int colorIndex = index % Colors.Count;
            //Circular buffer to assign colors
            return Colors[colorIndex];
        }
        else
        {
            return new Color(0, 0, 0);
        }
    }

    public int[,] CalculateLevelGrid()
    {
        var futureGrid = new int[GetRows(), GetCols()];
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                Vector3 worldPosition = Grid.GetCellCenterWorld(GetVectorFromCoordinates(row, col));
                Collider2D colliderAtCell = GetStaticColliderAt(worldPosition);
                //Return -1 if collider is null
                futureGrid[row, col] = GetColliderIndex(colliderAtCell);
            }
        }
        return futureGrid;
    }

    public Queue<Tuple<int, int>> GetInitialBoundaryCells()
    {
        Queue<Tuple<int, int>> cells = new Queue<Tuple<int, int>>();
        for (int row = 0; row < GetRows(); row++)
        {
            for (int col = 0; col < GetCols(); col++)
            {
                if (IsBoundaryCell(row, col))
                    cells.Enqueue(Tuple.Create(row, col));
            }
        }
        return cells;
    }

    public int GetCols() => _gridMax.x - _gridMin.x;

    public int GetRows() => _gridMax.y - _gridMin.y;

    private Queue<Tuple<int, int>> BoundaryCells = new Queue<Tuple<int, int>>();

    public bool IsInGrid(int row, int col) => row >= 0 && col >= 0 && row < LevelGrid.GetLength(0) && col < LevelGrid.GetLength(1);

    public Tuple<int, int>[] GetNeighbours(int row, int col)
    {
        // Check neighbors (up, down, left, right)
        Tuple<int, int>[] neighbors = {
                Tuple.Create(row - 1, col),
                Tuple.Create(row + 1, col),
                Tuple.Create(row, col - 1),
                Tuple.Create(row, col + 1)
            };
        return neighbors.Where(x => IsInGrid(x.Item1, x.Item2)).ToArray();
    }

    public void FloodFillStep()
    {
        //        while (BoundaryCells.Count > 0)
        //        {
        int boundaryCellCountBeforeAdding = BoundaryCells.Count();
        for (int i = 0; i < boundaryCellCountBeforeAdding; i++)
        {
            var currentCell = BoundaryCells.Dequeue();
            foreach (var neighbor in GetNeighbours(currentCell.Item1, currentCell.Item2))
            {
                int neighborRow = neighbor.Item1;
                int neighborCol = neighbor.Item2;

                if (IsInGrid(neighborRow, neighborCol) && LevelGrid[neighborRow, neighborCol] == -1)
                {
                    LevelGrid[neighborRow, neighborCol] = LevelGrid[currentCell.Item1, currentCell.Item2];
                    BoundaryCells.Enqueue(neighbor);
                }
            }
        }
        //        }
    }

    public void Start()
    {
        Init();
        Helpers.LogExecutionTime(Init, "Floodfill algorithm intitializaiton");
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            FloodFillStep();
        }
    }

    public void Init()
    {
        this.Grid = GetComponent<Grid>();
        if (PolygonBoundary != null)
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            _gridMin = Grid.WorldToCell(levelBounds.min);
            _gridMax = Grid.WorldToCell(levelBounds.max);
        }
        LevelGrid = CalculateLevelGrid();
        BoundaryCells = GetInitialBoundaryCells();
    }

    private Vector3Int GetVectorFromCoordinates(int row, int col) => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);

    private Collider2D GetStaticColliderAt(Vector3 worldPosition)
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

        return hit.collider;
    }

    public bool IsBoundaryCell(int row, int col)
    {
        var neighbours = GetNeighbours(row, col);
        //Bondary cells must be at the boundary of an obstacle so must be oocupied
        if (LevelGrid[row, col] == -1) return false;
        //Atleast one excited and one empty/unnocupied cell
        return neighbours.Any(x => LevelGrid[x.Item1, x.Item2] != -1) && neighbours.Any(x => LevelGrid[x.Item1, x.Item2] == -1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        DebugDrawGridByIndex();
    }

    private void DebugDrawGridByIndex()
    {
        int rows = _gridMax.y - _gridMin.y;
        int cols = _gridMax.x - _gridMin.x;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                if (LevelGrid[row, col] != -1)
                {
                    Gizmos.color = GetColorForValue(LevelGrid[row, col]);
                    Vector3Int cellPosition = new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);
                    Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);
                    worldPosition.z = 0;
                    Vector3 cellsize = Grid.cellSize;
                    cellsize.z = 1;
                    Gizmos.DrawCube(worldPosition, Grid.cellSize);
                }
            }
        }
    }

    // Start is called before the first frame update
}