using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    [ExecuteInEditMode]
    public class AStarSafePathToEnd : MeasureMono
    {
        private Vector2Int StartCoord;
        private Vector2Int GoalCoord;
        private Collider2D PlayerCollider;
        public LayerMask ObstacleLayerMask;

        public DiscreteRecalculatingFutureLevel FutureLevel;
        private List<AStar.Node> Path;

        //In this case is not only the walkable tiles but
        //the tiles that are completely "safe" - not observed by
        //any enemyu in the future leve time frameon
        private NativeGrid<bool> AllowedGrid;

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
            AStar.Node start = new AStar.Node(StartCoord);
            AStar.Node end = new AStar.Node(GoalCoord);
            Path = aStar.Run(start, end);
            if (Path == null)
            {
                IsTerminating = false;
                return PassStr;
            }
            Debug.Log($"Path count from A* is: {Path.Count}");
            IsTerminating = true;
            return FailStr;
        }

        public override void Init(GameObject manifestation)
        {
            IsValidator = true;

            var chromosomeMono = manifestation.GetComponentInChildren<LevelChromosomeMono>();

            LevelPhenotype phenotype =
                chromosomeMono.Chromosome.Phenotype;
            var heatmap = phenotype.FutureLevel.GetHeatmap();
            AllowedGrid = new NativeGrid<bool>(new UnboundedGrid(heatmap.Grid), phenotype.FutureLevel.GetBounds());
            AllowedGrid.Grid.Origin = chromosomeMono.transform.position;
            AllowedGrid.SetAll((x, y, AllowedGrid) =>
            {
                Vector3 world = AllowedGrid.GetWorldPosition(x, y);
                if (Physics2D.OverlapCircle(world, 0.2f, ObstacleLayerMask))
                {
                    return false;
                }
                if (!Mathf.Approximately(heatmap.Get(x, y), 0.0f))
                {
                    return false;
                }
                return true;
            });

            var character = manifestation.GetComponentInChildren<CharacterController2D>().gameObject;
            StartCoord = (Vector2Int)AllowedGrid.GetNativeCoord(character.transform.position);
            GoalCoord = (Vector2Int)AllowedGrid.GetNativeCoord(manifestation.GetComponentInChildren<WinTrigger>().transform.position);

            PlayerCollider = character.GetComponent<Collider2D>();
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
            return (AllowedGrid.IsInGrid(row, col) && AllowedGrid.Get(row, col) == true);
        }

        private void OnDrawGizmosSelected()
        {
            if (DrawOnSelected == false) return;

            if (AllowedGrid != null)
            {
                AllowedGrid.ForEach(
                    (x, y) =>
                    {
                        Vector3 pos = AllowedGrid.GetWorldPosition(x, y);
                        if (AllowedGrid.Get(x, y) == true)
                            Gizmos.color = Color.green;
                        else
                            Gizmos.color = Color.red;
                        Gizmos.DrawSphere(pos, 0.1f);
                    }
                    );
            }
            if (Path != null)
            {
                foreach (var node in Path)
                {
                    var worldPos = AllowedGrid.GetWorldPosition(node.Rows, node.Cols);
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(worldPos, 0.1f);
                }
            }
        }
    }
}