using Codice.CM.Common.Tree;
using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

public class AStar
{
    public class Node
    {
        public int Rows => NativeCoord.x;
        public int Cols => NativeCoord.y;
        public Vector2Int NativeCoord;

        public float F = Mathf.Infinity;
        public float G = Mathf.Infinity;
        public float H;

        public Node parent;

        public Node(int rows, int cols)
        {
            NativeCoord = new Vector2Int(rows, cols);
        }

        public Node(Vector2Int native)
        {
            NativeCoord = native;
        }
    }

    public NativeGrid<bool> LevelGrid;

    private Func<Node, Node, float> MovementCost;

    private Dictionary<Vector2Int, Node> PositionToNode = new Dictionary<Vector2Int, Node>();

    private float Heuristic(Node node, Node goal)
    {
        return Mathf.Abs(node.Cols - goal.Cols) + Mathf.Abs(node.Rows - goal.Rows);
    }

    private List<Node> ReconstructPath(Node current)
    {
        List<Node> path = new List<Node>();

        while (current != null)
        {
            path.Insert(0, current);
            current = current.parent;
        }

        return path;
    }

    //    public AStar(NativeGrid<bool> levelGrid)
    //    {
    //        LevelGrid = levelGrid;
    //    }
    public AStar(Func<Node, Node, float> movementCostFunc)
    {
        this.MovementCost = movementCostFunc;
    }

    public List<Node> Run(Node start, Node goal)
    {
        List<Node> openSet = new List<Node>();
        List<Node> closedSet = new List<Node>();
        PositionToNode.Add(new Vector2Int { x = start.Rows, y = start.Cols }, start);
        PositionToNode.Add(new Vector2Int { x = goal.Rows, y = goal.Cols }, goal);

        openSet.Add(start);
        start.G = 0;

        int maxIterCount = 15000;
        int iter = 0;
        while (openSet.Count > 0)
        {
            iter++;
            Node current = openSet.OrderBy(node => node.F).First();
            openSet.Remove(current);
            closedSet.Add(current);

            if (current == goal)
            {
                return ReconstructPath(current);
            }

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (iter > maxIterCount)
                    break;
                if (closedSet.Contains(neighbor)) continue;

                float tentativeGScore = current.G + MovementCost(current, neighbor);
                //bool newPath = !openSet.Contains(neighbor) || tentativeGScore < neighbor.G;
                bool newPath = tentativeGScore < neighbor.G;

                if (newPath)
                {
                    neighbor.parent = current;
                    neighbor.G = tentativeGScore;
                    neighbor.H = Heuristic(neighbor, goal);
                    neighbor.F = neighbor.G + neighbor.H;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null; // No path found
    }

    private List<Node> GetNeighbors(Node node)
    {
        List<Vector2Int> coords = new List<Vector2Int>()
        {
            node.NativeCoord + new Vector2Int(1,0),
            node.NativeCoord + new Vector2Int(-1,0),
            node.NativeCoord + new Vector2Int(0,1),
            node.NativeCoord + new Vector2Int(0,-1),

            node.NativeCoord + new Vector2Int(1,1),
            node.NativeCoord + new Vector2Int(1,-1),
            node.NativeCoord + new Vector2Int(-1,1),
            node.NativeCoord + new Vector2Int(-1,-1)
        };

        List<Node> neighborNodes = new List<Node>();

        foreach (var coord in coords)
        {
            if (PositionToNode.ContainsKey(coord))
            {
                neighborNodes.Add(PositionToNode[coord]);
            }
            else
            {
                var neighbourMap = new Node(coord.x, coord.y);
                PositionToNode.Add(coord, neighbourMap);
                neighborNodes.Add(neighbourMap);
            }
        }
        return neighborNodes;
    }

    //    private float MovementCost(Node a, Node b)
    //    {
    //        // Check if the neighbor is outside the grid or unwalkable (replace 1 with your unwalkable value)
    //        if (!IsWalkable(b.Rows, b.Cols))
    //        {
    //            return Mathf.Infinity; // Set a high cost for unwalkable or out-of-bounds nodes
    //        }
    //
    //        // If both nodes are walkable, return a uniform movement cost (adjust as needed)
    //        return 1.0f;
    //    }
    //
    //    private bool IsWalkable(int row, int col)
    //    {
    //        // Check if coordinates are within grid boundaries and the grid value indicates walkable terrain
    //        return (LevelGrid.IsInGrid(row, col) && LevelGrid.Get(row, col) == true);
    //    }
}

namespace StealthLevelEvaluation
{
    public class AStartPathEvalution : MeasureMono
    {
        public override MeasurementType GetCategory()
        {
            return MeasurementType.VALIDATION;
        }

        private NativeGrid<bool> LevelGrid;
        public List<AStar.Node> Path;
        public Collider2D PlayerCollider;
        public LayerMask ObstacleLayerMask;
        private Vector2Int StartCoord;
        private Vector2Int GoalCoord;

        public override string GetName()
        {
            return "AStartPathEvalution";
        }

        public bool SetObstacleGrid(int row, int col, NativeGrid<bool> ngrid)
        {
            //Return true if box cast did not collide with any obstacle
            return !Physics2D.BoxCast(
                ngrid.GetWorldPosition(row, col),
                PlayerCollider.bounds.size,
                0,
                Vector3.back, 0.1f,
                ObstacleLayerMask);
        }

        public override void Init(GameObject phenotype)
        {
            IsValidator = true;
            UnboundedGrid grid = new UnboundedGrid(phenotype.transform.position, 0.4f);
            var character = phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
            StartCoord =
                (Vector2Int)(grid.WorldToCell(character.transform.position));
            PlayerCollider = character.GetComponent<Collider2D>();
            GoalCoord = (Vector2Int)grid.WorldToCell(phenotype.GetComponentInChildren<WinTrigger>().transform.position);
            LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(phenotype));
            LevelGrid.SetAll(SetObstacleGrid);
        }

        protected override string Evaluate()
        {
            AStar aStar = new AStar(this.MovementCost);
            Vector2Int startNativeCoord = LevelGrid.GetNativeCoord(StartCoord);
            Vector2Int goalNativeCoord = LevelGrid.GetNativeCoord(GoalCoord);
            AStar.Node start = new AStar.Node(startNativeCoord);
            AStar.Node end = new AStar.Node(goalNativeCoord);
            Path = aStar.Run(start, end);
            if (Path == null)
            {
                IsTerminating = true;
                return FailStr;
            }
            Debug.Log($"Path count from A* is: {Path.Count}");
            return PassStr;
        }

        private float MovementCost(AStar.Node a, AStar.Node b)
        {
            // Check if the neighbor is outside the grid or unwalkable (replace 1 with your unwalkable value)
            if (!IsWalkable(b.Rows, b.Cols))
            {
                return Mathf.Infinity; // Set a high cost for unwalkable or out-of-bounds nodes
            }

            // If both nodes are walkable, return a uniform movement cost (adjust as needed)
            return 1.0f;
        }

        private bool IsWalkable(int row, int col)
        {
            // Check if coordinates are within grid boundaries and the grid value indicates walkable terrain
            return (LevelGrid.IsInGrid(row, col) && LevelGrid.Get(row, col) == true);
        }

        public void DrawLevelGrid()
        {
            if (LevelGrid == null) return;
            LevelGrid.ForEach((row, col) =>
            {
                if (LevelGrid.Get(row, col))
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Gizmos.DrawSphere(LevelGrid.GetWorldPosition(row, col), 0.1f);
            });
        }

        private void OnDrawGizmosSelected()
        {
            if (DrawOnSelected == false) return;
            DrawLevelGrid();
            if (Path != null)
            {
                foreach (var node in Path)
                {
                    var worldPos = LevelGrid.GetWorldPosition(node.Rows, node.Cols);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(worldPos, 0.1f);
                }
            }
        }
    }
}