using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public abstract class LevelPropertiesEvaluator : MeasureMono
    {
        public float PropertyValue;

        public override string GetName()
        {
            return GetType().Name.ToString();
        }

        public override MeasurementType GetCategory()
        {
            return MeasurementType.PROPERTIES;
        }

        public override void Init(GameObject phenotype)
        {
            Phenotype = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        }

        protected abstract float MeasureProperty();

        protected override string Evaluate()
        {
            try
            {
                PropertyValue = this.MeasureProperty();
                return PropertyValue.ToString();
            }
            catch (System.Exception)
            {
                PropertyValue = 0;
                return "-";
            }
        }
    }

    //    public class LevelPropertiesBulkEvaluator : MonoBehaviour
    //    {
    //        public List<LevelPropertiesEvaluator> LevelPropertiesEvaluators;
    //    public List<float> RealAestheticsMeasures = new List<float>();
    //    public const int MeasureCount = 3;
    //
    //    public void Measure(GameObject leve)
    //    {
    //        RealAestheticsMeasures = MeasureLevelAesthetics(leve);
    //    }
    //
    //    private List<float> MeasureLevelAesthetics(GameObject level)
    //    {
    //        var chromosome = level.GetComponentInChildren<LevelChromosomeMono>().Chromosome;
    //
    //        foreach (var e in LevelPropertiesEvaluators)
    //        {
    //            e.Init(level);
    //        }
    //
    //        if (chromosome != null)
    //        {
    //            return new List<float>()
    //            {
    //                GetEnemyToObstacleRatio(chromosome),
    //                ZoneCount(level),
    //                LevelClutterednessRatio(level)
    //            };
    //        }
    //
    //        return new List<float>() { 0, 0, 0 };
    //    }
    //
    //    private float GetEnemyToObstacleRatio(LevelChromosomeBase levelChromosome)
    //    {
    //        return (float)levelChromosome.GetGene(0).Value;
    //    }
    //
    //    private float ZoneCount(GameObject level)
    //
    //    {
    //        var rdGen = level.GetComponentInChildren<FloodfilledRoadmapGenerator>();
    //        if (rdGen == null) return 0;
    //        //TODO add direct reference to the max possible zone
    //        return Mathf.InverseLerp(MinZones, MaxZones, rdGen.ColliderKeys.Count);
    //    }
    //
    //    public bool SetObstacleGrid(int row, int col, NativeGrid<bool> ngrid)
    //    {
    //        //Return true if box cast did not collide with any obstacle
    //        return !Physics2D.OverlapBox(
    //            ngrid.GetWorldPosition(row, col),
    //            ngrid.Grid.cellSize, 0,
    //            LevelProperties.ObstacleLayerMask
    //            );
    //    }
    //
    //    //The ratio of occupied and unoccupeid cells
    //    private float LevelClutterednessRatio(GameObject level)
    //    {
    //        var roadmap = level.GetComponentInChildren<FloodfilledRoadmapGenerator>();
    //        Grid grid = roadmap.Grid;
    //        var LevelGrid = new NativeGrid<bool>(grid, Helpers.GetLevelBounds(level));
    //        LevelGrid.SetAll(SetObstacleGrid);
    //        int occupied = 0;
    //        int unoccupied = 0;
    //        LevelGrid.ForEach((x, y) =>
    //        {
    //            if (LevelGrid.Get(x, y))
    //                occupied++;
    //            else
    //                unoccupied++;
    //        });
    //        return (float)occupied / (float)(occupied + unoccupied);
    //    }
    //}
}