using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Runs an A star algorthim on the 2d map, but adds additional
//cost to each node in the grid based on how
public class MinimalCostOfAStarOnHeatmap : LevelPropertiesEvaluator
{
    private Vector2Int StartCoord;
    private Vector2Int GoalCoord;
    private Collider2D PlayerCollider;
    public LayerMask ObstacleLayerMask;
    public DiscreteRecalculatingFutureLevel FutureLevel;
    private UnboundedGrid Grid;
    private NativeGrid<float> LevelHeatmap;
    private List<AStar.Node> Path;

    public override string GetName()
    {
        return "AStarHeatmapMinimalPath";
    }

    public override void Init(GameObject phenotype)
    {
        IsValidator = true;
        UnboundedGrid grid = phenotype.GetComponentInChildren<LevelChromosomeMono>().Chromosome.Phenotype.Zones.Grid;
        var character = phenotype.GetComponentInChildren<CharacterController2D>().gameObject;
        StartCoord = (Vector2Int)grid.WorldToCell(character.transform.position);
        PlayerCollider = character.GetComponent<Collider2D>();
        GoalCoord = (Vector2Int)grid.WorldToCell(phenotype.GetComponentInChildren<WinTrigger>().transform.position);

        FutureLevel = phenotype
            .GetComponentInChildren<DiscreteRecalculatingFutureLevel>();
        Grid = grid;
        if (FutureLevel != null)
        {
            LevelHeatmap = FutureLevel.GetThreatHeatmap();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (DrawOnSelected == false) return;
        //        if (UniqueVisibleCells != null)
        //        {
        //            foreach (var cell in UniqueVisibleCells)
        //            {
        //                Gizmos.DrawSphere(Grid.CellToWorld(new Vector3Int(cell.x, cell.y)), 0.1f);
        //            }
        //        }
        if (Path != null)
        {
            foreach (var node in Path)
            {
                var worldPos = LevelHeatmap.GetWorldPosition(node.Rows, node.Cols);
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }

    protected override float MeasureProperty()
    {
        AStar aStar = new AStar(HeatmapMovementCost);
        Vector2Int startNativeCoord = LevelHeatmap.GetNativeCoord(StartCoord);
        Vector2Int goalNativeCoord = LevelHeatmap.GetNativeCoord(GoalCoord);
        AStar.Node start = new AStar.Node(startNativeCoord);
        AStar.Node end = new AStar.Node(goalNativeCoord);
        Path = aStar.Run(start, end);
        if (Path == null)
        {
            return 0.0f;
        }
        float pathCost = Path.Sum(x => HeatmapMovementCost(Path[0], x));
        pathCost /= Path.Count;
        Debug.Log($"Heatmap path cost from A* is: {pathCost}");
        return pathCost;
    }

    private float HeatmapMovementCost(AStar.Node a, AStar.Node b)
    {
        //return Mathf.Lerp(1, 10, 1.0f * LevelHeatmap.Get(b.Rows, b.Cols));
        return LevelHeatmap.Get(b.Rows, b.Cols);
    }
}