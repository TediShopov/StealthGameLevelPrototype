using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StealthLevelEvaluation
{
    [ExecuteInEditMode]
    public class EnemyPathVarience : LevelPropertiesEvaluator
    {
        protected override float MeasureProperty()
        {
            //TODO this could be extended to work with the predicatble nature
            // of IPredictable threat
            //            var predictableThreats =
            //                Phenotype.GetComponentsInChildren<IPredictableThreat>()
            //                .Select(x=>x.);

            var enemyPathLengths =
                Phenotype.GetComponentsInChildren<PatrolEnemyMono>()
                .Where(x => x is not null && x.GetPatrol() is not null)
                .Select(x => x.GetPatrol().GetPath().GetTotalLength())
                .ToList();

            if (enemyPathLengths.Count() <= 1)
                return 0f;
            float relDev = Helpers.CalculateRelativeVariance(enemyPathLengths);
            return Mathf.Clamp(relDev, 0f, 1f);
            return relDev;
        }
    }
}