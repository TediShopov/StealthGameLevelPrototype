using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class LevelClutterednessEvaluator : LevelPropertiesEvaluator
    {
        //public LevelProperties LevelProperties;
        public LevelProperties LevelProperties;

        public override void Init(GameObject phenotype)
        {
            base.Init(phenotype);
        }

        public bool SetObstacleGrid(int row, int col, NativeGrid<bool> ngrid)
        {
            //Return true if box cast did not collide with any obstacle
            return !Physics2D.OverlapBox(
                ngrid.GetWorldPosition(row, col),
                ngrid.Grid.cellSize, 0,
                LevelProperties.ObstacleLayerMask
                );
        }

        //The ratio of occupied and unoccupeid cells
        private float LevelClutterednessRatioResult(GameObject level)
        {
            var roadmap = level.GetComponentInChildren<FloodfilledRoadmapGenerator>();
            Grid grid = roadmap.Grid;
            var LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(level));
            LevelGrid.SetAll(SetObstacleGrid);
            int occupied = 0;
            int unoccupied = 0;
            LevelGrid.ForEach((x, y) =>
            {
                if (LevelGrid.Get(x, y))
                    occupied++;
                else
                    unoccupied++;
            });
            return (float)occupied / (float)(occupied + unoccupied);
        }

        protected override float MeasureProperty()
        {
            return LevelClutterednessRatioResult(Phenotype);
        }
    }
}