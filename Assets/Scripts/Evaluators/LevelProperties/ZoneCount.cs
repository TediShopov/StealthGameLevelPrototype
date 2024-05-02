using System.Collections.Generic;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public class ZoneCount : LevelPropertiesEvaluator
    {
        private const int MinZones = 0;

        private const int MaxZones = 15;

        protected override float MeasureProperty()
        {
            var phenotype = Manifestation.GetComponentInChildren<LevelChromosomeMono>().Chromosome.Phenotype;
            if (phenotype == null) return 0;

            HashSet<int> uniqueZones = new HashSet<int>();
            phenotype.Zones.ForEach((x, y) =>
            {
                uniqueZones.Add(phenotype.Zones.Get(x, y));
            });

            //TODO add direct reference to the max possible zone
            return Mathf.InverseLerp(MinZones, MaxZones, uniqueZones.Count);
        }
    }
}