using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RelativeLevelCoverage : LevelPropertiesEvaluator
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
            base.Init(phenotype);
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        }

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

        protected override float MeasureProperty()
        {
            //Get Future level instance
            var futureLevel =
                Phenotype.GetComponentInChildren<IFutureLevel>(false);
            var _staticObstacleGrid =
                new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            int obstacleCount = 0;
            _staticObstacleGrid.SetAll((row, col, ngrid) =>
            {
                if (Helpers.IsColidingCell(
                    ngrid.GetWorldPosition(row, col),
                    Grid.cellSize,
                    ObstacleLayerMask))
                {
                    obstacleCount++;
                    return true;
                }
                return false;
            });
            int maxCells =
                _staticObstacleGrid.GetCols() * _staticObstacleGrid.GetRows();
            maxCells -= obstacleCount;
            int colliding =
                DiscreteCollidingCells(
                    (DiscreteRecalculatingFutureLevel)futureLevel,
                    Helpers.GetLevelBounds(Phenotype));
            float relCoverage = (float)colliding / (float)maxCells;
            return relCoverage;
        }
    }
}