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
            return "RiskMeasure";
        }

        public override void Init(GameObject phenotype)
        {
        }

        public List<float> RiskMeasures;

        protected override string Evaluate()
        {
            RiskMeasures = new List<float>();
            var RRTVisualizers = Phenotype.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
            var patrols = Phenotype.GetComponentsInChildren<PatrolEnemyMono>()
                .Select(x => x.GetPatrol());
            //var voxelizedLevel = generator.GetComponentInChildren<VoxelizedLevel>();
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>();
            foreach (var x in RRTVisualizers)
            {
                if (x.RRT.Succeeded())
                {
                    var solutionPath =
                        new SolutionPath(x.RRT.ReconstructPathToSolution());
                    var riskMeasure = new FieldOfViewRiskMeasure(
                        solutionPath,
                        patrols);

                    float overallRisk = riskMeasure.OverallRisk(futureLevel.Step);
                    RiskMeasures.Add(overallRisk);
                }
            }
            return string.Join(",", RiskMeasures.ToArray());
        }
    }
}