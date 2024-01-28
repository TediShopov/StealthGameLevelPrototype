using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Search;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class FloodfilledRoadmapGenerator : MonoBehaviour
{
    //Dictionary exposed to Unity editor
    public List<Collider2D> ColliderKeys;

    public List<Color> Colors;
    public bool DebugDraw;
    public Grid Grid;
    public LayerMask ObstacleLayerMask;
    public PolygonBoundary PolygonBoundary;
    public Graph<Vector2> RoadMap = new Graph<Vector2>();

    private Vector3Int _gridMax;
    private Vector3Int _gridMin;
    private Queue<Tuple<int, int>> BoundaryCells = new Queue<Tuple<int, int>>();
    public List<Tuple<Vector2, Vector2>> _debugSimplifiedConnections = new List<Tuple<Vector2, Vector2>>();

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

    public void FloodRegions()
    {
        BoundaryCells = GetInitialBoundaryCells();
        while (BoundaryCells.Count > 0)
        {
            var currentCell = BoundaryCells.Dequeue();
            foreach (var neighbor in GetNeighbours(currentCell.Item1, currentCell.Item2))
            {
                int neighborRow = neighbor.Item1;
                int neighborCol = neighbor.Item2;

                if (IsInGrid(neighborRow, neighborCol))
                {
                    if (LevelGrid[neighborRow, neighborCol] == -1)
                    {
                        LevelGrid[neighborRow, neighborCol] = LevelGrid[currentCell.Item1, currentCell.Item2];
                        BoundaryCells.Enqueue(neighbor);
                    }
                    else
                    {
                        int relRow = neighborRow - currentCell.Item1;
                        int relCol = neighborCol - currentCell.Item2;

                        if (relRow > 0)
                        {
                            if (LevelGrid[neighborRow, neighborCol] != LevelGrid[currentCell.Item1, currentCell.Item2])
                            {
                                Vector3 gridCenter = Grid.GetCellCenterWorld(new Vector3Int(neighborCol + _gridMin.x, neighborRow + _gridMin.y, 0));
                                Vector3 lowerLeft = gridCenter + new Vector3(-Grid.cellSize.x / 2.0f, -Grid.cellSize.y / 2.0f, 0);
                                lowerLeft = Snapping.Snap(lowerLeft, (Vector2)Grid.cellSize);
                                Vector3 lowerRight = gridCenter + new Vector3(Grid.cellSize.x / 2.0f, -Grid.cellSize.y / 2.0f, 0);
                                lowerRight = Snapping.Snap(lowerRight, (Vector2)Grid.cellSize);
                                RoadMap.AddNode(lowerLeft);
                                RoadMap.AddNode(lowerRight);
                                RoadMap.AddEdge(lowerLeft, lowerRight);
                            }
                        }
                        if (relCol > 0)
                        {
                            if (LevelGrid[neighborRow, neighborCol] != LevelGrid[currentCell.Item1, currentCell.Item2])
                            {
                                Vector3 gridCenter = Grid.GetCellCenterWorld(new Vector3Int(neighborCol + _gridMin.x, neighborRow + _gridMin.y, 0));
                                Vector3 upperLeft = gridCenter + new Vector3(-Grid.cellSize.x / 2.0f, +Grid.cellSize.y / 2.0f, 0);
                                upperLeft = Snapping.Snap(upperLeft, (Vector2)Grid.cellSize);
                                Vector3 lowerLeft = gridCenter + new Vector3(-Grid.cellSize.x / 2.0f, -Grid.cellSize.y / 2.0f, 0);
                                lowerLeft = Snapping.Snap(lowerLeft, (Vector2)Grid.cellSize);
                                RoadMap.AddNode(upperLeft);
                                RoadMap.AddNode(lowerLeft);
                                RoadMap.AddEdge(upperLeft, lowerLeft);
                            }
                        }
                    }
                }
            }
        }
    }

    public void RemoveRedundantNodes(Vector2 start, ref int totalRecursions, List<Vector2> visited)
    {
        visited.Add(start);
        var neighbors = RoadMap.GetNeighbors(start).Where(x => visited.Contains(x) == false).ToList();

        List<Vector2> visitedFromStart = new List<Vector2>();
        //Mark all immediate negihbours as visited and removed redundant ones
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector2 reachedEnd = RemoveRedundantNodesInConnection(start, neighbors[i], ref totalRecursions);
            if (reachedEnd.Equals(start) == false)
                visitedFromStart.Add(reachedEnd);
        }
        visited.AddRange(visitedFromStart);
        foreach (var neighbor in visitedFromStart)
        {
            RemoveRedundantNodes(neighbor, ref totalRecursions, visited);
        }
        //        foreach (var startNeighbour in neighbors)
        //        {
        //            RemoveRedundantNodesInConnection(start, startNeighbour, ref totalRecursions);
        //        }
    }

    public Vector2 RemoveRedundantNodesInConnection(Vector2 start, Vector2 end, ref int recursionCount)
    {
        recursionCount++;
        if (recursionCount >= 200) return start;
        List<Vector2> visited = new List<Vector2>();
        //Should be changed to a while
        for (int i = 0; i < 1000; i++)
        {
            //Expand end
            var endNeghbors = RoadMap.GetNeighbors(end);
            //Possibly redundant - it has exactly one connection to unvisted node
            var unvisited = endNeghbors.Where(x => !visited.Contains(x) && !x.Equals(start)).ToList();
            if (unvisited.Count > 1)
            {
                //Node is Super node -> break as endj
                break;
            }
            else
            {
                //Continue getting nodes along the line
                var endCandidate = unvisited.FirstOrDefault();
                if (endCandidate == null) break;
                if (Physics2D.Linecast(start, endCandidate, ObstacleLayerMask))
                {
                    //if obstacle is hit cannot simplify more break without changing end
                    break;
                }
                else
                {
                    //Can continue simplifyiund
                    visited.Add(end);

                    end = endCandidate;
                }
            }
        }

        if (visited.Count >= 1)
        {
            //Remove redundant edges 
            RoadMap.RemoveEdge(start, visited[0]);
            for (int j = 0; j < visited.Count - 1; j++)
            {
                RoadMap.RemoveEdge(visited[j], visited[j + 1]);
            }
            RoadMap.RemoveEdge(visited[visited.Count - 1], end);
            //Remove redundant nodes 
            for (int j = 0; j < visited.Count; j++) 
            {
                RoadMap.RemoveNode(visited[j]);
            }

        }

        RoadMap.AddEdge(start, end);
        _debugSimplifiedConnections.Add(Tuple.Create(start, end));

        return end;
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

    public int GetCols() => _gridMax.x - _gridMin.x;

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

    public int GetRows() => _gridMax.y - _gridMin.y;

    public Vector3Int GetVectorFromCoordinates(int row, int col) => new Vector3Int(col + _gridMin.x, row + _gridMin.y, 0);

    public void Init()
    {
        _debugSimplifiedConnections = new List<Tuple<Vector2, Vector2>>();
        this.Grid = GetComponent<Grid>();
        if (PolygonBoundary != null)
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            _gridMin = Grid.WorldToCell(levelBounds.min);
            _gridMax = Grid.WorldToCell(levelBounds.max);
        }
        LevelGrid = CalculateLevelGrid();
        FloodRegions();
        Vector2 superNode = RoadMap.adjacencyList.First().Key;
        foreach (var nodeInfoPair in RoadMap.adjacencyList)
        {
            if (nodeInfoPair.Value.Count > 2)
            {
                superNode = nodeInfoPair.Key;
                break;
            }
        }
        int totalRecursion = 0;
        RemoveRedundantNodes(superNode, ref totalRecursion, new List<Vector2>());
        Debug.Log($"Roadmap nodes: {RoadMap.adjacencyList.Count}");
        Debug.Log($"Simplified connection count : {_debugSimplifiedConnections.Count}");
        Debug.Log($"Recursion count is : {totalRecursion}");
    }

    public bool IsBoundaryCell(int row, int col)
    {
        var neighbours = GetNeighbours(row, col);
        //Bondary cells must be at the boundary of an obstacle so must be oocupied
        if (LevelGrid[row, col] == -1) return false;
        //Atleast one excited and one empty/unnocupied cell
        return neighbours.Any(x => LevelGrid[x.Item1, x.Item2] != -1) && neighbours.Any(x => LevelGrid[x.Item1, x.Item2] == -1);
    }

    public bool IsInGrid(int row, int col) => row >= 0 && col >= 0 && row < LevelGrid.GetLength(0) && col < LevelGrid.GetLength(1);

    public void Start()
    {
        //Helpers.LogExecutionTime(Init, "Floodfill algorithm intitializaiton");
    }

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

    #region Debug

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

    private void DebugSimplifiedConnections()
    {
        Gizmos.color = Color.red;
        foreach (var sc in _debugSimplifiedConnections)
        {
            Vector3 cellsize = Grid.cellSize;
            cellsize.z = 1;
            Gizmos.DrawLine(sc.Item1, sc.Item2);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (DebugDraw)
        {
            Gizmos.color = Color.blue;
            DebugDrawGridByIndex();
            Graph<Vector2>.DebugDrawGraph(RoadMap, Color.red, Color.green, 0.01f);
            //DebugSimplifiedConnections();
            //Debug draw nodes with only one connecitons
        }
    }

    #endregion Debug

    // Start is called before the first frame update
}

