using UnityEngine;

namespace StealthLevelEvaluation
{
    public class ZoneCount : LevelPropertiesEvaluator
    {
        private const int MinZones = 0;

        private const int MaxZones = 15;

        protected override float MeasureProperty()
        {
            var rdGen = Manifestation.GetComponentInChildren<FloodfilledRoadmapGenerator>();
            if (rdGen == null) return 0;
            //TODO add direct reference to the max possible zone
            return Mathf.InverseLerp(MinZones, MaxZones, rdGen.ColliderKeys.Count);
        }
    }
}