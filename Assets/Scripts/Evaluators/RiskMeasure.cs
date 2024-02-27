using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class RiskMeasure : PhenotypeFitnessEvaluation
    {
        public RiskMeasure(GameObject level) : base(level, "Risk Measure of solutions", 0)
        {
        }

        public override float Evaluate()
        {
            var RRTVisualizers = Phenotype.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
            var enemyPatrolPaths = Phenotype.GetComponentsInChildren<PatrolPath>();
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
                        enemyPatrolPaths.ToList(),
                        enemyPatrolPaths[0].EnemyProperties,
                        LayerMask.GetMask("Obstacles"));
                    float overallRisk = riskMeasure.OverallRisk(futureLevel.Step);
                    total += overallRisk;
                    succeeded++;
                }
            }
            float avg = total / (float)succeeded;
            return avg;
        }
    }
}