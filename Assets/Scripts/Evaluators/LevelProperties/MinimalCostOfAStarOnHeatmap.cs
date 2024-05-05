using GeneticSharp;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.AI;

//Runs an A star algorthim on the 2d map, but adds additional
//cost to each node in the grid based on how
[ExecuteInEditMode]
public class MinimalCostOfAStarOnHeatmap : EvaluatorMono
{
    private Vector2Int StartCoord;
    private Vector2Int GoalCoord;
    private Collider2D PlayerCollider;
    public LayerMask ObstacleLayerMask;
    public IFutureLevel FutureLevel;
    private NativeGrid<float> LevelHeatmap;
    private List<AStar.Node> Path;
    private NativeGrid<bool> ObstacleGrid;

    //    public override string GetName()
    //    {
    //        return "AStarHeatmapMinimalPath";
    //    }
    //
    //    public override MeasurementType GetCategory()
    //    {
    //        return MeasurementType.DIFFICULTY;
    //    }

    public void Init(GameObject manifestation)
    {
        //IsValidator = true;
        var character = manifestation.GetComponentInChildren<CharacterController2D>().gameObject;
        PlayerCollider = character.GetComponent<Collider2D>();

        var pheno = LevelChromosomeMono.Find(manifestation).Chromosome.Phenotype;
        FutureLevel = pheno.FutureLevel;
        if (FutureLevel != null)
        {
            LevelHeatmap = new NativeGrid<float>(FutureLevel.GetHeatmap());
        }

        LevelHeatmap.Grid.Origin = manifestation.transform.position;
        StartCoord = (Vector2Int)LevelHeatmap.GetNativeCoord(character.transform.position);
        GoalCoord = (Vector2Int)LevelHeatmap.GetNativeCoord(manifestation.GetComponentInChildren<WinTrigger>().transform.position);

        var chromosomeMono = manifestation.GetComponentInChildren<LevelChromosomeMono>();
        ObstacleGrid = new NativeGrid<bool>(
            new UnboundedGrid(Vector2.zero, 0.4f),
            pheno.FutureLevel.GetBounds());

        ObstacleGrid.Grid.Origin = manifestation.transform.position;
        ObstacleGrid.SetAll((x, y, t) =>
        {
            Vector3 world = t.GetWorldPosition(x, y);
            if (Physics2D.OverlapCircle(world, 0.2f, ObstacleLayerMask))
            {
                return false;
            }
            return true;
        });
    }

    private float HeatmapMovementCost(AStar.Node a, AStar.Node b)
    {
        try
        {
            if (ObstacleGrid.Get(b.Rows, b.Cols) == false)
            {
                return 15;
            }
            return LevelHeatmap.Get(b.Rows, b.Cols);
        }
        catch (System.Exception)
        {
            return 15;
        }
    }

    private float PathCost = 0;

    private void OnDrawGizmosSelected()
    {
        //if (DrawOnSelected == false) return;

        if (ObstacleGrid != null)
        {
            ObstacleGrid.ForEach(
                (x, y) =>
                {
                    Vector3 pos = ObstacleGrid.GetWorldPosition(x, y);
                    if (ObstacleGrid.Get(x, y) == true)
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
                var worldPos = LevelHeatmap.GetWorldPosition(node.Rows, node.Cols);
                Handles.Label(worldPos,
                    $"{node.G}");
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(worldPos, 0.1f);
            }
        }
    }

    public double PathCostH()
    {
        AStar aStar = new AStar(HeatmapMovementCost);
        AStar.Node start = new AStar.Node(StartCoord);
        AStar.Node end = new AStar.Node(GoalCoord);
        Path = aStar.Run(start, end);
        if (Path == null)
        {
            return 0f;
        }
        //PathCost = Path.Sum(x => HeatmapMovementCost(Path[0], x));
        PathCost = Path[Path.Count - 1].F;
        return PathCost;
    }

    //    protected override string Evaluate()
    //    {
    //        return PathCostH().ToString();
    //        AStar aStar = new AStar(HeatmapMovementCost);
    //        AStar.Node start = new AStar.Node(StartCoord);
    //        AStar.Node end = new AStar.Node(GoalCoord);
    //        Path = aStar.Run(start, end);
    //        if (Path == null)
    //        {
    //            return 0.0f.ToString();
    //        }
    //        PathCost = Path.Sum(x => HeatmapMovementCost(Path[0], x));
    //        Debug.Log($"Heatmap path cost from A* is: {PathCost}");
    //        return PathCost.ToString();
    //    }

    public override EvaluatorMono PrototypeComponent(GameObject to)
    {
        GameObject contaier = AttachEvaluatorContainer(to);
        var prototype = contaier.AddComponent<MinimalCostOfAStarOnHeatmap>();
        prototype.ObstacleLayerMask = this.ObstacleLayerMask;
        return prototype;
    }

    public override double Evaluate(IChromosome chromosome)
    {
        if (chromosome is LevelChromosomeBase)
        {
            var levelChromosome = (LevelChromosomeBase)chromosome;
            if (levelChromosome == null) return -100;

            Init(levelChromosome.Manifestation);
            return PathCostH();
        }
        return 0;
    }
}