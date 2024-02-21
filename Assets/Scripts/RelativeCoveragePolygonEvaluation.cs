using CGALDotNet;
using CGALDotNet.Polygons;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RelativeCoveragePolygonEvaluation : PhenotypeFitnessEvaluation
    {
        private Grid Grid;
        private LayerMask ObstacleLayerMask;
        private PatrolPath[] _debugEnenmies;

        public RelativeCoveragePolygonEvaluation(GameObject level) : base(level, "Polygon based average realtive overlapping areas", 0)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        }

        public override void OnSelected()
        {
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
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                for (int i = 0; i < _debugEnenmies.Length - 1; i++)
                {
                    for (int j = i + 1; j < _debugEnenmies.Length; j++)
                    {
                        //Calculation the field of views from a future transfrom

                        //Generate a CGAL object
                        var fieldOfViewPoly = new Polygon2<EEK>(new CGALDotNetGeometry.Numerics.Point2d[0]);
                        var fieldOfViewPolyOther = new Polygon2<EEK>(new CGALDotNetGeometry.Numerics.Point2d[0]);
                    }
                }
            }
            float avgRelOverlapp = accumulatedOverlapp / maxTime;
            return -avgRelOverlapp * 100;
        }
    }
}