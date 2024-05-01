using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StealthLevelEvaluation
{
    [ExecuteInEditMode]
    public class LevelClutterednessEvaluator : LevelPropertiesEvaluator
    {
        //public LevelProperties LevelProperties;
        public LevelProperties LevelProperties;

        public override void Init(GameObject phenotype)
        {
            base.Init(phenotype);
        }

        public void FixedUpdate()
        {
            if (RunNow)
            {
                this.PropertyValue = LevelClutterednessRatioResult(Manifestation);
                //this.Value = PropertyValue.ToString();
                Debug.Log("Ran from fixed update");
                RunNow = false;
            }
        }

        public bool SetObstacleGrid(int row, int col, NativeGrid<bool> ngrid)
        {
            //Return true if box cast did not collide with any obstacle
            return Physics2D.OverlapBox(
                ngrid.GetWorldPosition(row, col),
                new Vector2(ngrid.Grid.cellSize, ngrid.Grid.cellSize), 0,
                LevelProperties.ObstacleLayerMask
                );
        }

        //The ratio of occupied and unoccupeid cells
        private float LevelClutterednessRatioResult(GameObject level)
        {
            try
            {
                var roadmap = level.GetComponentInChildren<FloodfilledRoadmapGenerator>();
                //Grid grid = roadmap.Grid;
                var LevelGrid = new NativeGrid<bool>(roadmap.Grid, Helpers.GetLevelBounds(level));
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
                float percentOccupationPerSquare =
                    (float)occupied / (float)(occupied + unoccupied);

                //Max allowed occupation percentage = 50%
                return Mathf.Clamp(percentOccupationPerSquare, 0, 0.5f);

                //return Mathf.Lerp(0, )
            }
            catch (System.Exception)
            {
                Debug.Log("Evaluation did not work properly");
                return 0;
            }
        }

        protected override float MeasureProperty()
        {
            return LevelClutterednessRatioResult(Manifestation);
        }
    }
}