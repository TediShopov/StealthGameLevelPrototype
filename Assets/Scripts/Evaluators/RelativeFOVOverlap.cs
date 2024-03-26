using PlasticPipe.PlasticProtocol.Messages;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[System.Serializable]
public struct RelativeFovData
{
    public Grid Grid;
    public LayerMask ObstacleLayerMask;
    public PatrolPath[] _debugEnenmies;
}

namespace StealthLevelEvaluation
{
    public class RelativeFOVOverlap : PhenotypeFitnessEvaluation
    {
        public RelativeFovData Data;

        public void Awake()
        {
            if (Phenotype == null)
                Init(Helpers.SearchForTagUpHierarchy(this.gameObject, "Level"), "RelativeFOV", 0);
        }

        public override void Init(GameObject phenotype, string name, double defValue)
        {
            base.Init(phenotype, name, defValue);
            Data.ObstacleLayerMask = LayerMask.GetMask("Obstacle");
            if (Phenotype != null)
                Data.Grid = Phenotype.GetComponentInChildren<Grid>();
            else
                throw new System.ArgumentException("No valid grid component in level");
        }

        public override void Init(GameObject phenotype)
        {
            Init(phenotype, "Relative FOV Overla", 0);
        }

        //        public RelativeFOVOverlap(GameObject level) : base(level, "Average realtive overlapping areas", 0)
        //        {
        //            Data.Grid = Phenotype.GetComponentInChildren<Grid>(false);
        //        }

        private void DebugDrawDiscreteBounds(Bounds bounds, Color color)
        {
            Gizmos.color = color;
            foreach (var cells in DiscretBoundsCells(bounds))
            {
                Gizmos.DrawSphere(Data.Grid.GetCellCenterWorld(cells), 0.1f);
            }
        }

        private List<Vector3Int> DiscretBoundsCells(Bounds bounds)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>();
            Vector3Int gridMin = Data.Grid.WorldToCell(bounds.min);
            Data.ObstacleLayerMask = LayerMask.GetMask("Obstacle");
            Vector3Int gridMax = Data.Grid.WorldToCell(bounds.max);
            for (int rows = gridMin.y; rows < gridMax.y; rows++)
            {
                for (int cols = gridMin.x; cols < gridMax.x; cols++)
                {
                    worldPositions.Add((new Vector3Int(cols, rows, 0)));
                }
            }
            return worldPositions;
        }

        public override void OnSelected()
        {
            if (Data._debugEnenmies is null) return;
            if (Data.Grid is null) return;
            for (int i = 0; i < Data._debugEnenmies.Length - 1; i++)
            {
                for (int j = i + 1; j < Data._debugEnenmies.Length; j++)
                {
                    var e = Data._debugEnenmies[i];
                    var othere = Data._debugEnenmies[j];
                    float VD = e.EnemyProperties.ViewDistance;
                    float FOV = e.EnemyProperties.FOV;
                    Bounds bounds = FieldOfView.GetFovBounds(
                        e.GetFutureTransform(0),
                    e.EnemyProperties.ViewDistance,
                    e.EnemyProperties.FOV);
                    Bounds otherBounds = FieldOfView.GetFovBounds(
                        othere.GetFutureTransform(0),
                        othere.EnemyProperties.ViewDistance,
                        othere.EnemyProperties.FOV);
                    if (bounds.Intersects(otherBounds))
                    {
                        var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                        DebugDrawDiscreteBounds(overlapp, Color.magenta);
                        List<Vector3Int> visibleCoordinates =
                            DiscretBoundsCells(overlapp)
                            .Where(x =>
                            {
                                var pos = Data.Grid.GetCellCenterWorld(x);
                                bool one = FieldOfView.TestCollision(pos, e.GetFutureTransform(0), FOV, VD, Data.ObstacleLayerMask);
                                bool other = FieldOfView.TestCollision(pos, othere.GetFutureTransform(0), FOV, VD, Data.ObstacleLayerMask);
                                if (one && other)
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawSphere(pos, 0.1f);
                                }
                                return one && other;
                            }).ToList();
                    }
                }
            }
        }

        private float VD;
        private float FOV;

        public override float Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            Data._debugEnenmies = Phenotype.GetComponentsInChildren<PatrolPath>();
            NativeGrid<bool> native = new NativeGrid<bool>(Data.Grid, Helpers.GetLevelBounds(Phenotype));
            native.SetAll((x, y, n) => false);

            VD = Data._debugEnenmies[0].EnemyProperties.ViewDistance;
            FOV = Data._debugEnenmies[0].EnemyProperties.FOV;
            //Formula: angel in radians multipled by radius on the power of 2
            float maxOverlappArea = Mathf.Deg2Rad * FOV * VD * VD;
            float accumulatedOverlapp = 0;
            float maxTime = futureLevel.GetMaxSimulationTime();
            Helpers.LogExecutionTime(() => accumulatedOverlapp = OverlapRelativeToDiscreteMaxFOV(futureLevel, maxTime, maxOverlappArea), "New Overlapp");

            float avgRelOverlapp = accumulatedOverlapp / maxTime;
            return -avgRelOverlapp * 100;
        }

        private HashSet<Vector3Int> VisibleCells(Bounds bounds, FutureTransform ft)
        {
            HashSet<Vector3Int> enemyOneVisibleCoordinates = DiscretBoundsCells(bounds)
                .Where(x =>
                {
                    var pos = Data.Grid.GetCellCenterWorld(x);
                    return FieldOfView.TestCollision(pos, ft, FOV, VD, Data.ObstacleLayerMask);
                }).ToHashSet();
            return enemyOneVisibleCoordinates;
        }

        private float OverlapRelativeToDiscreteMaxFOV(IFutureLevel futureLevel, float maxTime, float maxOverlappArea)
        {
            List<BacktrackPatrolPath> simulatedPaths = Data._debugEnenmies
                .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();
            float accumulatedOverlapp = 0;
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                //Move all paths
                simulatedPaths.ForEach(x => x.MoveAlong(futureLevel.Step * Data._debugEnenmies[0].EnemyProperties.Speed));
                for (int i = 0; i < Data._debugEnenmies.Length - 1; i++)
                {
                    FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
                    Bounds bounds = FieldOfView.GetFovBounds(enemyFT, VD, FOV);
                    HashSet<Vector3Int> enemyOneVisibleCoordinates = VisibleCells(bounds, enemyFT);

                    for (int j = i + 1; j < Data._debugEnenmies.Length; j++)
                    {
                        FutureTransform otherEnemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[j]);
                        Bounds otherBounds = FieldOfView.GetFovBounds(otherEnemyFT, VD, FOV);
                        if (bounds.Intersects(otherBounds))
                        {
                            Profiler.BeginSample("Bounds intersecting");
                            var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                            Profiler.EndSample();
                            Profiler.BeginSample("Cell visibility checking");

                            HashSet<Vector3Int> enemyTwoVisibleCoordinates = VisibleCells(otherBounds, otherEnemyFT);

                            HashSet<Vector3Int> visibleCoordinates = new HashSet<Vector3Int>(enemyOneVisibleCoordinates);
                            visibleCoordinates.IntersectWith(enemyTwoVisibleCoordinates);

                            Profiler.EndSample();
                            float cellArea = (Data.Grid.cellSize.x * Data.Grid.cellSize.y);
                            maxOverlappArea = Mathf.Max(enemyOneVisibleCoordinates.Count * cellArea, enemyTwoVisibleCoordinates.Count * cellArea);
                            if (maxOverlappArea != 0)
                            {
                                float estimatedOverlappArea = visibleCoordinates.Count * cellArea;
                                float relativeOverlappArea = estimatedOverlappArea / maxOverlappArea;
                                accumulatedOverlapp += relativeOverlappArea;
                            }
                        }
                    }
                }
            }
            return accumulatedOverlapp; ;
        }

        private float OveralpRealtiveToSectionArea(IFutureLevel futureLevel, float maxTime, float maxOverlappArea)
        {
            List<BacktrackPatrolPath> simulatedPaths = Data._debugEnenmies
                .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();

            float accumulatedOverlapp = 0;
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                //Move all paths
                simulatedPaths.ForEach(x => x.MoveAlong(futureLevel.Step * Data._debugEnenmies[0].EnemyProperties.Speed));
                for (int i = 0; i < Data._debugEnenmies.Length - 1; i++)
                {
                    FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
                    Bounds bounds = FieldOfView.GetFovBounds(enemyFT, VD, FOV);
                    for (int j = i + 1; j < Data._debugEnenmies.Length; j++)
                    {
                        FutureTransform otherEnemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[j]);
                        Bounds otherBounds = FieldOfView.GetFovBounds(otherEnemyFT, VD, FOV);
                        if (bounds.Intersects(otherBounds))
                        {
                            Profiler.BeginSample("Bounds intersecting");
                            var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                            Profiler.EndSample();
                            Profiler.BeginSample("Cell visibility checking");
                            List<Vector3Int> visibleCoordinates =
                                DiscretBoundsCells(overlapp)
                                .Where(x =>
                                {
                                    var pos = Data.Grid.GetCellCenterWorld(x);
                                    bool one = FieldOfView.TestCollision(pos, enemyFT, FOV, VD, Data.ObstacleLayerMask);
                                    bool other = FieldOfView.TestCollision(pos, otherEnemyFT, FOV, VD, Data.ObstacleLayerMask);
                                    return one && other;
                                }).ToList();
                            Profiler.EndSample();
                            float estimatedOverlappArea = visibleCoordinates.Count * (Data.Grid.cellSize.x * Data.Grid.cellSize.y);
                            float relativeOverlappArea = estimatedOverlappArea / maxOverlappArea;
                            accumulatedOverlapp += relativeOverlappArea;
                        }
                    }
                }
            }
            return accumulatedOverlapp; ;
        }
    }
}