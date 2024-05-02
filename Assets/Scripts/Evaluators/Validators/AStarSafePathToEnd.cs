using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class AStarSafePathToEnd : MeasureMono
    {
        private Vector2Int StartCoord;
        private Vector2Int GoalCoord;
        private Collider2D PlayerCollider;
        public LayerMask ObstacleLayerMask;
        public DiscreteRecalculatingFutureLevel FutureLevel;
        private UnboundedGrid Grid;
        private HashSet<Vector2Int> UniqueVisibleCells;
        private List<AStar.Node> Path;

        //In this case is not only the walkable tiles but
        //the tiles that are completely safe zone based on
        //a level future
        private NativeGrid<bool> LevelGrid;

        public override MeasurementType GetCategory()
        {
            return MeasurementType.VALIDATION;
        }

        public override string GetName()
        {
            return "AStarSafePathToEnd";
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
                IsTerminating = false;
                return 0.0f.ToString();
            }
            Debug.Log($"Path count from A* is: {Path.Count}");
            IsTerminating = true;
            return 1.0f.ToString();
        }

        public override void Init(GameObject manifestation)
        {
            IsValidator = true;

            LevelPhenotype phenotype =
                manifestation.GetComponentInChildren<LevelChromosomeMono>().Chromosome.Phenotype;

            UnboundedGrid grid = phenotype.Zones.Grid;
            var character = manifestation.GetComponentInChildren<CharacterController2D>().gameObject;
            StartCoord = (Vector2Int)grid.WorldToCell(character.transform.position);
            PlayerCollider = character.GetComponent<Collider2D>();
            GoalCoord = (Vector2Int)grid.WorldToCell(manifestation.GetComponentInChildren<WinTrigger>().transform.position);

            //            FutureLevel = manifestation
            //                .GetComponentInChildren<DiscreteRecalculatingFutureLevel>();
            FutureLevel = (DiscreteRecalculatingFutureLevel)phenotype.FutureLevel;
            Grid = grid;
            if (FutureLevel != null)
            {
                UniqueVisibleCells = FutureLevel.UniqueVisibleCells(
                    grid,
                    0,
                    FutureLevel.GetMaxSimulationTime());
            }
            LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(manifestation));
            LevelGrid.SetAll(SetSafeGridCells);
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

        public bool SetSafeGridCells(int row, int col, NativeGrid<bool> ngrid)
        {
            //Return true if box cast did not collide with any obstacle
            bool safe = !UniqueVisibleCells.Contains((Vector2Int)ngrid.GetUnityCoord(row, col));
            bool walkable = !Physics2D.BoxCast(
                ngrid.GetWorldPosition(row, col),
                PlayerCollider.bounds.size,
                0,
                Vector3.back, 0.1f,
                ObstacleLayerMask);
            return safe && walkable;
        }

        private void OnDrawGizmosSelected()
        {
            if (DrawOnSelected == false) return;
            if (UniqueVisibleCells != null)
            {
                foreach (var cell in UniqueVisibleCells)
                {
                    Gizmos.DrawSphere(Grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y)), 0.1f);
                    //Gizmos.DrawSphere(Grid.CellToWorld(new Vector3Int(cell.x, cell.y)), 0.1f);
                }
            }
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