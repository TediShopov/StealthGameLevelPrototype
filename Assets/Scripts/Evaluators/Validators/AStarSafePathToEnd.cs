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
        public ContinuosFutureLevel FutureLevel;
        private Grid Grid;
        private HashSet<Vector2Int> UniqueVisibleCells;
        private List<AStar.Node> Path;

        //In this case is not only the walkable tiles but
        //the tiles that are completely safe zone based on
        //a level future
        private NativeGrid<bool> LevelGrid;

        public override string Evaluate()
        {
            AStar aStar = new AStar(LevelGrid);
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

        public override void Init(GameObject phenotype)
        {
            Name = "SafePathToEnd";
            IsValidator = true;
            var floodFillGenerator = phenotype.GetComponentInChildren<FloodfilledRoadmapGenerator>();
            Grid grid = phenotype.GetComponentInChildren<Grid>();
            var character = phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
            StartCoord = (Vector2Int)grid.WorldToCell(character.transform.position);
            PlayerCollider = character.GetComponent<Collider2D>();
            GoalCoord = (Vector2Int)grid.WorldToCell(phenotype.GetComponentInChildren<WinTrigger>().transform.position);

            FutureLevel = phenotype.GetComponentInChildren<ContinuosFutureLevel>();
            Grid = phenotype.GetComponentInChildren<Grid>();
            if (FutureLevel != null)
            {
                UniqueVisibleCells = FutureLevel.UniqueVisibleCells(
                    Grid,
                    0,
                    FutureLevel.GetMaxSimulationTime());
            }
            LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(phenotype));
            LevelGrid.SetAll(SetSafeGridCells);
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
            if (UniqueVisibleCells != null)
            {
                foreach (var cell in UniqueVisibleCells)
                {
                    Gizmos.DrawSphere(Grid.CellToWorld(new Vector3Int(cell.x, cell.y)), 0.1f);
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