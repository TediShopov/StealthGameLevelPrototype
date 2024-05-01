using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RiskMeasure : MeasureMono
    {
        public override MeasurementType GetCategory()
        {
            return MeasurementType.DIFFICULTY;
        }

        //            public RiskMeasure(GameObject level) : base(level, "Risk Measure of solutions", 0)
        //            {
        //            }
        public override string GetName()
        {
            return "MinimalRiskMeasure";
        }

        public override void Init(GameObject phenotype)
        {
        }

        public List<float> RiskMeasures;

        protected override string Evaluate()
        {
            RiskMeasures = new List<float>();
            var RRTVisualizers = Manifestation.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
            var patrols = Manifestation.GetComponentsInChildren<PatrolEnemyMono>()
                .Select(x => x.GetPatrol());
            //var voxelizedLevel = generator.GetComponentInChildren<VoxelizedLevel>();
            var futureLevel = Manifestation.GetComponentInChildren<IFutureLevel>();
            foreach (var x in RRTVisualizers)
            {
                if (x.RRT.Succeeded())
                {
                    MeasureResult childMeasre = new MeasureResult();
                    float time = Helpers.TrackExecutionTime(() =>
                    {
                        var solutionPath =
                            new SolutionPath(x.RRT.ReconstructPathToSolution());
                        var riskMeasure = new FieldOfViewRiskMeasure(
                            solutionPath,
                            patrols);

                        float overallRisk = riskMeasure.OverallRisk(futureLevel.Step);
                        RiskMeasures.Add(overallRisk);
                        childMeasre.Name = "RiskMeasure";
                        childMeasre.Value = overallRisk.ToString();
                    }
                        );

                    childMeasre.Time = time;
                    //this.Result.AddChildMeasure(childMeasre);
                }
            }

            if (RiskMeasures.Count > 0)
                return RiskMeasures.Min().ToString();
            return "-";
            //return string.Join(",", RiskMeasures.ToArray());
        }
    }
}