using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace StealthLevelEvaluation
{
    public class RelativeFOVOverlap : PhenotypeFitnessEvaluation
    {
        private Grid Grid;
        private LayerMask ObstacleLayerMask;
        private PatrolPath[] _debugEnenmies;

        public RelativeFOVOverlap(GameObject level) : base(level, "Average realtive overlapping areas", 0)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        }

        private void DebugDrawDiscreteBounds(Bounds bounds, Color color)
        {
            Gizmos.color = color;
            foreach (var cells in DiscretBoundsCells(bounds))
            {
                Gizmos.DrawSphere(Grid.GetCellCenterWorld(cells), 0.1f);
            }
        }

        private List<Vector3Int> DiscretBoundsCells(Bounds bounds)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>();
            Vector3Int gridMin = Grid.WorldToCell(bounds.min);
            Vector3Int gridMax = Grid.WorldToCell(bounds.max);
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
            if (_debugEnenmies is null) return;
            if (Grid is null) return;
            for (int i = 0; i < _debugEnenmies.Length - 1; i++)
            {
                for (int j = i + 1; j < _debugEnenmies.Length; j++)
                {
                    var e = _debugEnenmies[i];
                    var othere = _debugEnenmies[j];
                    float vd = e.EnemyProperties.ViewDistance;
                    float fov = e.EnemyProperties.FOV;
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
                                var pos = Grid.GetCellCenterWorld(x);
                                bool one = FieldOfView.TestCollision(pos, e.GetFutureTransform(0), fov, vd, ObstacleLayerMask);
                                bool other = FieldOfView.TestCollision(pos, othere.GetFutureTransform(0), fov, vd, ObstacleLayerMask);
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

        public override float Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            _debugEnenmies = Phenotype.GetComponentsInChildren<PatrolPath>();
            NativeGrid<bool> native = new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            native.SetAll((x, y, n) => false);
            float maxTime = ((ContinuosFutureLevel)futureLevel)
                .EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());

            float vd = _debugEnenmies[0].EnemyProperties.ViewDistance;
            float fov = _debugEnenmies[0].EnemyProperties.FOV;
            //Formula: angel in radians multipled by radius on the power of 2
            float maxOverlappArea = Mathf.Deg2Rad * fov * vd * vd;
            float accumulatedOverlapp = 0;
            Helpers.LogExecutionTime(() => accumulatedOverlapp = NewAccumualtedOverlapp(futureLevel, maxTime, vd, fov, maxOverlappArea), "New Overlapp");
            float avgRelOverlapp = accumulatedOverlapp / maxTime;
            return -avgRelOverlapp * 100;
        }

        private float NewAccumualtedOverlapp(IFutureLevel futureLevel, float maxTime, float vd, float fov, float maxOverlappArea)
        {
            List<BacktrackPatrolPath> simulatedPaths = _debugEnenmies
                .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();

            float accumulatedOverlapp = 0;
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                //Move all paths
                simulatedPaths.ForEach(x => x.MoveAlong(futureLevel.Step * _debugEnenmies[0].EnemyProperties.Speed));
                for (int i = 0; i < _debugEnenmies.Length - 1; i++)
                {
                    FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
                    Bounds bounds = FieldOfView.GetFovBounds(enemyFT, vd, fov);
                    for (int j = i + 1; j < _debugEnenmies.Length; j++)
                    {
                        FutureTransform otherEnemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[j]);
                        Bounds otherBounds = FieldOfView.GetFovBounds(otherEnemyFT, vd, fov);
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
                                    var pos = Grid.GetCellCenterWorld(x);
                                    bool one = FieldOfView.TestCollision(pos, enemyFT, fov, vd, ObstacleLayerMask);
                                    bool other = FieldOfView.TestCollision(pos, otherEnemyFT, fov, vd, ObstacleLayerMask);
                                    return one && other;
                                }).ToList();
                            Profiler.EndSample();
                            float estimatedOverlappArea = visibleCoordinates.Count * (Grid.cellSize.x * Grid.cellSize.y);
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