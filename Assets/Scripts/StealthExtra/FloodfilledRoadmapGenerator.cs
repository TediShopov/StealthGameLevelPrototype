using CGALDotNet.Processing;
using StealthLevelEvaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class FloodfillRoadmapGeneratorBase
{
}

[ExecuteInEditMode]
[RequireComponent(typeof(Grid))]
public class FloodfilledRoadmapGenerator : MeasureMono,
    IPrototypable<FloodfilledRoadmapGenerator>
{
    //Dictionary exposed to Unity editor
    [HideInInspector] public List<Collider2D> ColliderKeys;

    public bool DoFloodFill = false;
    public bool DebugDraw;
    public bool ExtraChecks = false;
    public float EnemyBSRadius = 1.0f;
    public List<Color> Colors;

    ///    [HideInInspector] public Grid Grid;
    ///    [HideInInspector] public LayerMask ObstacleLayerMask;
    ///    [HideInInspector] public LayerMask BoundaryLayerMask;
    public Grid Grid;

    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    public Graph<Vector2> RoadMap = new Graph<Vector2>();
    public List<Tuple<Vector2, Vector2>> _debugSimplifiedConnections = new List<Tuple<Vector2, Vector2>>();

    private Queue<Tuple<int, int>> BoundaryCells = new Queue<Tuple<int, int>>();

    //Transforms the unity grid to c# binary represenetaion of the level
    public NativeGrid<int> LevelGrid;

    public void Awake()
    {
        Grid = GetComponent<Grid>();
        //        Colors = new List<Color>();
        //        Colors.Add(new Color(1, 0, 0, 0.2f));
        //        Colors.Add(new Color(0, 1, 0, 0.2f));
        //        Colors.Add(new Color(0, 0, 1, 0.2f));
        //        Colors.Add(new Color(1, 1, 0, 0.2f));
        //        Colors.Add(new Color(1, 0, 1, 0.2f));
        //        Colors.Add(new Color(0, 1, 1, 0.2f));
    }

    public void Update()
    {
        if (DoFloodFill)
        {
            Init(Helpers.SearchForTagUpHierarchy(this.gameObject, "Level"));
            Evaluate();
            //FloodRegions();
            //return Physics2D.Linecast(a, b, ObstacleLayerMask);
            DoFloodFill = false;
        }
    }

    public NativeGrid<int> GetFloodGrid()
    { return LevelGrid; }

    public int GetCellZoneIndex(Vector2Int worldGrid)
    {
        Vector2Int nativeCoord = LevelGrid.GetNativeCoord(worldGrid);
        return LevelGrid.Get(nativeCoord.x, nativeCoord.y);
    }

    private List<List<Vector2>> FindSubgraphsDFS()
    {
        HashSet<Vector2> visited = new HashSet<Vector2>();
        List<List<Vector2>> subgraphs = new List<List<Vector2>>();

        foreach (Vector2 node in RoadMap.adjacencyList.Keys)
        {
            if (!visited.Contains(node))
            {
                List<Vector2> subgraph = new List<Vector2>();
                DFS(node, visited, subgraph);
                subgraphs.Add(subgraph);
            }
        }

        return subgraphs;
    }

    private void DFS(Vector2 node, HashSet<Vector2> visited, List<Vector2> subgraph)
    {
        visited.Add(node);
        subgraph.Add(node);

        foreach (Vector2 neighbor in RoadMap.adjacencyList[node])
        {
            if (!visited.Contains(neighbor))
            {
                DFS(neighbor, visited, subgraph);
            }
            //return Physics2D.Linecast(a, b, ObstacleLayerMask);
        }
    }

    private Vector2 PickUnvistiedSuperNode(IEnumerable<Vector2> nodeGroup)
    {
        if (RoadMap.adjacencyList.Count <= 0) return Vector2.zero;
        Vector2 superNode = nodeGroup.First();
        foreach (var node in nodeGroup)
        {
            if (RoadMap.GetNeighbors(node).Count != 2)
            {
                superNode = node;
                break;
            }
        }
        return superNode;
    }

    public int SetCellColliderIndex(int row, int col, NativeGrid<int> ngrid)
    {
        Collider2D[] collidersAtCell = GetStaticColliderAt(LevelGrid.GetWorldPosition(row, col));
        if (collidersAtCell.Length >= 2)
            return 1;
        if (collidersAtCell.Length <= 0)
            return -1;
        return GetColliderIndex(collidersAtCell.First());
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

                if (LevelGrid.IsInGrid(neighborRow, neighborCol))
                {
                    if (LevelGrid.Get(neighborRow, neighborCol) == -1)
                    {
                        LevelGrid.Set(neighborRow, neighborCol, LevelGrid.Get(currentCell.Item1, currentCell.Item2));
                        BoundaryCells.Enqueue(neighbor);
                    }
                    else
                    {
                        int relRow = neighborRow - currentCell.Item1;
                        int relCol = neighborCol - currentCell.Item2;

                        if (relRow > 0)
                        {
                            if (LevelGrid.Get(neighborRow, neighborCol) != LevelGrid.Get(currentCell.Item1, currentCell.Item2))
                            {
                                var lowerLeft = GetLowerLeft(neighborCol, neighborRow);
                                var lowerRight = GetLowerLeft(neighborCol + 1, neighborRow);
                                if (ExtraChecks && CannotTraverse(lowerLeft, lowerRight))
                                    continue;
                                RoadMap.AddNode(lowerRight);
                                RoadMap.AddNode(lowerLeft);
                                RoadMap.AddEdge(lowerLeft, lowerRight);
                            }
                        }
                        if (relCol > 0)
                        {
                            if (LevelGrid.Get(neighborRow, neighborCol) != LevelGrid.Get(currentCell.Item1, currentCell.Item2))
                            {
                                var lowerLeft = GetLowerLeft(neighborCol, neighborRow);
                                var upperLeft = GetLowerLeft(neighborCol, neighborRow + 1);

                                if (ExtraChecks && CannotTraverse(lowerLeft, upperLeft))
                                    continue;
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

    public Vector3 GetLowerLeft(int col, int row)
    {
        return LevelGrid.GetWorldPosition(row, col)
            + new Vector3(-Grid.cellSize.x / 2.0f, -Grid.cellSize.y / 2.0f, 0);
    }

    //}
    public void RemoveRedundantNodes(Vector2 start, ref int totalRecursions, List<Vector2> visited)
    {
        visited.Add(start);
        var neighbors = RoadMap.GetNeighbors(start).Where(x => visited.Contains(x) == false).ToList();

        List<Vector2> visitedFromStart = new List<Vector2>();
        //Mark all immediate negihbours as visited and removed redundant ones
        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector2 reachedEnd = RemoveRedundantNodesInConnection(start, neighbors[i], ref totalRecursions, visited);
            if (reachedEnd.Equals(start) == false)
                visitedFromStart.Add(reachedEnd);
        }
        visited.AddRange(visitedFromStart);
        foreach (var neighbor in visitedFromStart)
        {
            RemoveRedundantNodes(neighbor, ref totalRecursions, visited);
        }
    }

    public bool CannotTraverse(Vector2 a, Vector2 b)
    {
        //return Physics2D.CircleCast(a, EnemyBSRadius, (b - a).normalized, Vector2.Distance(b, a), ObstacleLayerMask);
        return Physics2D.Linecast(a, b, ObstacleLayerMask);
    }

    public Vector2 RemoveRedundantNodesInConnection(Vector2 start, Vector2 end, ref int recursionCount, List<Vector2> visited)
    {
        recursionCount++;
        if (recursionCount >= 200) return start;
        List<Vector2> visitedInConnection = new List<Vector2>();
        //Should be changed to a while
        for (int i = 0; i < 1000; i++)
        {
            //Expand end
            var endNeghbors = RoadMap.GetNeighbors(end);
            //Possibly redundant - it has exactly one connection to unvisted node
            var unvisited = endNeghbors.Where(x => !visitedInConnection.Contains(x) && !visited.Contains(x) && !x.Equals(start)).ToList();
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
                if (CannotTraverse(start, endCandidate))
                {
                    //if obstacle is hit cannot simplify more break without changing end
                    break;
                }
                else
                {
                    //Can continue simplifyiund
                    visitedInConnection.Add(end);

                    end = endCandidate;
                }
            }
        }

        if (visitedInConnection.Count >= 1)
        {
            //Remove redundant edges
            RoadMap.RemoveEdge(start, visitedInConnection[0]);
            for (int j = 0; j < visitedInConnection.Count - 1; j++)
            {
                RoadMap.RemoveEdge(visitedInConnection[j], visitedInConnection[j + 1]);
            }
            RoadMap.RemoveEdge(visitedInConnection[visitedInConnection.Count - 1], end);
            //Remove redundant nodes
            for (int j = 0; j < visitedInConnection.Count; j++)
            {
                RoadMap.RemoveNode(visitedInConnection[j]);
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

    public Queue<Tuple<int, int>> GetInitialBoundaryCells()
    {
        Queue<Tuple<int, int>> cells = new Queue<Tuple<int, int>>();
        LevelGrid.ForEach((x, y) =>
        {
            if (IsBoundaryCell(x, y))
            {
                cells.Enqueue(Tuple.Create(x, y));
            }
        });
        return cells;
        //Queue<Tuple<int, int>> cells = new Queue<Tuple<int, int>>();
        //for (int row = 0; row < GetRows(); row++)
        //{
        //    for (int col = 0; col < GetCols(); col++)
        //    {
        //        if (IsBoundaryCell(row, col))
        //            cells.Enqueue(Tuple.Create(row, col));
        //    }
        //}
        //return cells;
    }

    // Use the Assert class to test conditions

    public Tuple<int, int>[] GetNeighbours(int row, int col)
    {
        // Check neighbors (up, down, left, right)
        Tuple<int, int>[] neighbors = {
                Tuple.Create(row - 1, col),
                Tuple.Create(row + 1, col),
                Tuple.Create(row, col - 1),
                Tuple.Create(row, col + 1)
            };
        return neighbors.Where(x => LevelGrid.IsInGrid(x.Item1, x.Item2)).ToArray();
    }

    public bool IsBoundaryCell(int row, int col)
    {
        var neighbours = GetNeighbours(row, col);
        //Bondary cells must be at the boundary of an obstacle so must be oocupied
        if (LevelGrid.Get(row, col) == -1) return false;
        //Atleast one excited and one empty/unnocupied cell
        return neighbours.Any(x => LevelGrid.Get(x.Item1, x.Item2) != -1) && neighbours.Any(x =>
            LevelGrid.Get(x.Item1, x.Item2) == -1);
    }

    private Collider2D[] GetStaticColliderAt(Vector3 worldPosition)
    {
        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        Vector2 halfBoxSize = Grid.cellSize * 0.5f;

        // Perform a BoxCast to check for obstacles in the area
        RaycastHit2D[] hit = Physics2D.BoxCastAll(
            origin: position2D,
            size: halfBoxSize,
            angle: 0f,
            direction: Vector2.zero,
            distance: 1.0f,
            layerMask: ObstacleLayerMask
        );

        return hit.Select(x => x.collider).ToArray();
    }

    //        int rows = _gridMax.y - _gridMin.y;
    //        int cols = _gridMax.x - _gridMin.x;
    //        for (int row = 0; row < rows; row++)
    //        {
    //            for (int col = 0; col < cols; col++)
    //            {
    //            }
    //        }

    #region Debug

    private void DebugDrawGridByIndex()
    {
        LevelGrid.ForEach((row, col) =>
        {
            if (LevelGrid.Get(row, col) != -1)
            {
                Gizmos.color = GetColorForValue(LevelGrid.Get(row, col));
                Vector3 worldPosition = Grid.GetCellCenterWorld(LevelGrid.GetUnityCoord(row, col));
                worldPosition.z = 0;
                Vector3 cellsize = Grid.cellSize;
                cellsize.z = 1;
                Gizmos.DrawCube(worldPosition, Grid.cellSize);
            }
        });
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
            if (LevelGrid == null) return;
            Gizmos.color = Color.blue;
            DebugDrawGridByIndex();
            Graph<Vector2>.DebugDrawGraph(RoadMap, Color.red, Color.green, 0.01f);
            //DebugSimplifiedConnections();
            //Debug draw nodes with only one connecitons
        }
    }

    public FloodfilledRoadmapGenerator PrototypeComponent(GameObject to)
    {
        var other = to.AddComponent<FloodfilledRoadmapGenerator>();
        other.ObstacleLayerMask = this.ObstacleLayerMask;
        other.BoundaryLayerMask = this.BoundaryLayerMask;
        other.Colors = this.Colors;
        other.DebugDraw = this.DebugDraw;
        other.ExtraChecks = this.ExtraChecks;
        other.EnemyBSRadius = this.EnemyBSRadius;
        return other;
    }

    public override string GetName()
    {
        return "RoadmapFloofill";
    }

    public override void Init(GameObject phenotype)
    {
        ColliderKeys = new List<Collider2D>();
        _debugSimplifiedConnections = new List<Tuple<Vector2, Vector2>>();
        this.Grid = GetComponent<Grid>();

        LevelGrid = new NativeGrid<int>(this.Grid, Helpers.GetLevelBounds(gameObject));

        LevelGrid.SetAll(SetCellColliderIndex);
    }

    protected override string Evaluate()
    {
        Profiler.BeginSample(GetName());
        RoadMap = new Graph<Vector2>();
        FloodRegions();
        if (RoadMap.adjacencyList.Count <= 0) return "-";
        int totalRecursion = 0;

        List<List<Vector2>> subgraphs = FindSubgraphsDFS();
        foreach (var subgraph in subgraphs)
        {
            Vector2 superNode = PickUnvistiedSuperNode(subgraph);
            RemoveRedundantNodes(superNode, ref totalRecursion, new List<Vector2>());
        }
        Profiler.EndSample();

        Debug.Log($"Roadmap nodes: {RoadMap.adjacencyList.Count}");
        Debug.Log($"Simplified connection count : {_debugSimplifiedConnections.Count}");
        Debug.Log($"Recursion count is : {totalRecursion}");
        Debug.Log($"Graph count is : {subgraphs.Count}");
        return "-";
    }

    #endregion Debug

    // Start is called before the first frame update
}