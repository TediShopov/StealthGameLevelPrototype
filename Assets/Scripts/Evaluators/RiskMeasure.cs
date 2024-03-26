using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RiskMeasure : PhenotypeFitnessEvaluation
    {
        //            public RiskMeasure(GameObject level) : base(level, "Risk Measure of solutions", 0)
        //            {
        //            }

        public override void Init(GameObject phenotype, string name, double defValue)
        {
            base.Init(phenotype, name, defValue);
        }

        public override void Init(GameObject phenotype)
        {
            Init(phenotype, "Risk Measure", 0);
        }

        public override float Evaluate()
        {
            var RRTVisualizers = Phenotype.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
            var patrols = Phenotype.GetComponentsInChildren<PatrolEnemyMono>()
                .Select(x => x.GetPatrol());
            //var voxelizedLevel = generator.GetComponentInChildren<VoxelizedLevel>();
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>();
            float total = 0;
            int succeeded = 0;
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
                    total += overallRisk;
                    succeeded++;
                }
            }
            if (succeeded == 0) { return 0; }
            float avg = total / (float)succeeded;
            return avg;
        }
    }
}