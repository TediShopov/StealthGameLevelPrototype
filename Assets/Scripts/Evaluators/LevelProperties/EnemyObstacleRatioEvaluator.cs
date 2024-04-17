using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class EnemyObstacleRatioEvaluator : LevelPropertiesEvaluator
    {
        protected override float MeasureProperty()
        {
            try
            {
                var chromo = (LevelChromosomeBase)Phenotype.GetComponentInChildren<LevelChromosomeMono>().Chromosome;
                return (float)chromo.GetGene(0).Value;
            }
            catch (System.Exception)
            {
                throw;
            }
        }
    }
}