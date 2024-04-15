using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RelativeLevelCoverage : MeasureMono
    {
        private Grid Grid;
        private LayerMask ObstacleLayerMask;

        //            public RelativeLevelCoverage(GameObject level) : base(level, "Relative Level Coverage", 0)
        //            {
        //                Grid = Phenotype.GetComponentInChildren<Grid>(false);
        //                ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        //            }
        public override string GetName()
        {
            return "Relative Coverage";
        }

        public override void Init(GameObject phenotype)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        }

        //        public override void Init(GameObject phenotype)
        //        {
        //            Init(phenotype,);
        //        }

        //        private int DiscreteNotCollidingCells(ContinuosFutureLevel futureLevel, ref NativeGrid<bool> staticObstacle)
        //        {
        //            List<Vector2> allCells = new List<Vector2>();
        //            for (int i = 0; i < staticObstacle.GetRows(); i++)
        //            {
        //                for (int j = 0; j < staticObstacle.GetCols(); j++)
        //                {
        //                    if (staticObstacle.Get(i, j) == false)
        //                        allCells.Add(staticObstacle.GetWorldPosition(i, j));
        //                }
        //            }
        //
        //            float maxTime = futureLevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
        //            var notcolliding = futureLevel.AreNotCollidingDynamicDiscrete(allCells, 0, maxTime);
        //            var _visibilityCountGrid = new NativeGrid<bool>(staticObstacle);
        //            _visibilityCountGrid.SetAll((x, y, _visibilityCountGrid) => false);
        //            foreach (var worldPos in notcolliding)
        //            {
        //                Vector2Int nativeCoord = _visibilityCountGrid.GetNativeCoord((Vector2Int)Grid.WorldToCell(new Vector3(worldPos.x, worldPos.y)));
        //                _visibilityCountGrid.Set(nativeCoord.x, nativeCoord.y, true);
        //            }
        //            return notcolliding.Count;
        //        }

        private int DiscreteCollidingCells(DiscreteRecalculatingFutureLevel futureLevel, Bounds levelBounds)
        {
            var boundsInt = new BoundsInt();
            boundsInt.min = Grid.WorldToCell(levelBounds.min);
            boundsInt.max = Grid.WorldToCell(levelBounds.max);

            int maxCells = (boundsInt.max.x - boundsInt.min.x) * (boundsInt.max.y - boundsInt.min.y);

            float maxTime = futureLevel.GetMaxSimulationTime();
            var UniqueVisibleCells = futureLevel.UniqueVisibleCells(Grid, 0, maxTime);

            return UniqueVisibleCells.Count;
        }

        protected override string Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            var _staticObstacleGrid = new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            int obstacleCount = 0;
            _staticObstacleGrid.SetAll((row, col, ngrid) =>
            {
                if (Helpers.IsColidingCell(ngrid.GetWorldPosition(row, col), Grid.cellSize, ObstacleLayerMask))
                {
                    obstacleCount++;
                    return true;
                }
                return false;
            });
            int maxCells = _staticObstacleGrid.GetCols() * _staticObstacleGrid.GetRows();
            int colliding = DiscreteCollidingCells((DiscreteRecalculatingFutureLevel)futureLevel, Helpers.GetLevelBounds(Phenotype));
            colliding -= obstacleCount;
            float relCoverage = (float)colliding / (float)maxCells;
            return relCoverage.ToString();
        }
    }
}