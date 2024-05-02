using Codice.Client.BaseCommands;
using GeneticSharp;
using PlasticPipe.PlasticProtocol.Messages;
using StealthLevelEvaluation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using UnityEngine;
using UnityEngine.Experimental.AI;

public interface IEngagementMeasurable
{
    public float EngagementScore { get; set; }
}

public interface ILevelChromosome :
    IEngagementMeasurable,
    IAestheticMeasurable<ILevelChromosome>
{
}

[Serializable]
public class UnboundedGrid
{
    [SerializeField]
    public Vector2 Origin;

    [SerializeField]
    public float cellSize;

    public UnboundedGrid(Vector2 origin, float cellSize)
    {
        Origin = origin;
        this.cellSize = cellSize;
    }

    public UnboundedGrid(UnboundedGrid other)
    {
        this.Origin = other.Origin;
        this.cellSize = other.cellSize;
    }

    public UnboundedGrid(Grid grid)
    {
        Origin = grid.transform.position;
        this.cellSize = grid.cellSize.x;
    }

    // Converts grid coordinates to world coordinates
    public Vector3 GetCellCenterWorld(Vector3Int coord)
    {
        float worldX = Origin.x + coord.x * cellSize;
        float worldY = Origin.y + coord.y * cellSize;
        return new Vector2(worldX, worldY);
    }

    // Converts grid coordinates to world coordinates
    public Vector3 GetCellCenterWorld(int gridX, int gridY)
    {
        float worldX = Origin.x + gridX * cellSize;
        float worldY = Origin.y + gridY * cellSize;
        return new Vector2(worldX, worldY);
    }

    // Converts world coordinates to grid coordinates
    public Vector3Int WorldToCell(Vector2 worldPos)
    {
        int gridX = Mathf.FloorToInt((worldPos.x - Origin.x) / cellSize);
        int gridY = Mathf.FloorToInt((worldPos.y - Origin.y) / cellSize);
        return new Vector3Int(gridX, gridY, 0);
    }

    // Checks if a given world position is within certain bounds
    public bool IsWithinGrid(Vector2 worldPos, Vector2 bounds)
    {
        return (worldPos.x >= Origin.x && worldPos.x <= Origin.x + bounds.x * cellSize) &&
               (worldPos.y >= Origin.y && worldPos.y <= Origin.y + bounds.y * cellSize);
    }

    // Computes the Euclidean distance between two grid points
    public float DistanceBetween(Vector2Int gridPos1, Vector2Int gridPos2)
    {
        return Vector2Int.Distance(gridPos1, gridPos2);
    }
}

[Serializable]
public class LevelPhenotype
{
    public LevelPhenotype()
    {
    }

    public LevelPhenotype(LevelPhenotype other)
    {
        this.Roadmap = new Graph<Vector2>(other.Roadmap);
        this.Zones = new NativeGrid<int>(other.Zones);
        this.Threats = new List<IPredictableThreat>(other.Threats);
        this.FutureLevel = other.FutureLevel;
    }

    //public GenericDictionary<int, string> Dict;
    [SerializeField] public Graph<Vector2> Roadmap;

    //[SerializeField] public GenericMatrix<int> Matrix;

    [SerializeField] public NativeGrid<int> Zones;
    [SerializeReference] public List<IPredictableThreat> Threats;
    [SerializeReference] public IFutureLevel FutureLevel;
}

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

[Serializable]
public abstract class LevelChromosomeBase : ChromosomeBase,
    IAestheticMeasurable<LevelChromosomeBase>
{
    public System.Random ChromosomeRandom;

    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
        Measurements = new Dictionary<string, MeasureResult>();
        //AestheticProperties =  new List<float>();
        AestheticProperties = new PropertyMeasurements(0);
    }

    public LevelPhenotypeGenerator PhenotypeGenerator;

    //The decoded game object - could be null if not generated
    [SerializeField] public LevelPhenotype Phenotype;

    [SerializeField] public GameObject Manifestation;

    public float AestheticScore { get; set; }
    public float EngagementScore { get; set; }

    [SerializeField] public Dictionary<string, MeasureResult> Measurements;
    [SerializeReference] public PropertyMeasurements AestheticProperties;

    public void AddOrReplace(MeasureResult res)
    {
        this.Measurements[res.Name] = res;
    }

    public override IChromosome Clone()
    {
        //var clone =new LevelChromosome(Length, PhenotypeGenerator);
        var clone = (LevelChromosomeBase)base.Clone();
        clone.Measurements = this.Measurements;
        clone.AestheticProperties = this.AestheticProperties;
        clone.AestheticScore = this.AestheticScore;
        clone.EngagementScore = this.EngagementScore;
        return clone;
    }

    public override bool Equals(object obj)
    {
        if (obj is LevelChromosomeBase)
        {
            var other = (LevelChromosomeBase)obj;
            if (this.Length != other.Length) return false;

            //Compare genes one by one
            Gene[] current = this.GetGenes();
            Gene[] otherGenes = other.GetGenes();

            for (int i = 0; i < current.Length; i++)
            {
                if (current[i].Equals(otherGenes[i]) == false)
                    return false;
            }
            return true;
        }
        return false;
    }

    public PropertyMeasurements GetMeasurements()
    {
        return AestheticProperties;
    }

    //    public void Update(IPreferenceModel<LevelChromosomeBase> sub)
    //    {
    //        UpdateAestheticScore(sub, GetMeasurements());
    //    }'E

    public float UpdateAestheticScore(IPreferenceModel<LevelChromosomeBase> model, PropertyMeasurements properties)
    {
        AestheticScore = CalculateAestheticScore(model.Weights, properties);
        return AestheticScore;
    }

    public static float CalculateAestheticScore(
        IList<float> weights,
        PropertyMeasurements aestheticMeasurements)
    {
        float aestheticScore = 0;
        for (int i = 0; i < aestheticMeasurements.Count; i++)
        {
            aestheticScore += aestheticMeasurements[i] * weights[i];
        }
        return aestheticScore;
    }

    public float GetAestheticScore(IPreferenceModel<LevelChromosomeBase> model)
    {
        AestheticScore = CalculateAestheticScore(model.Weights, GetMeasurements());
        return AestheticScore;
    }
}

[Serializable]
public class PropertyMeasurements : List<float>
{
    //List contaning all the measures
    //public IList<float> Measurements;

    public PropertyMeasurements(int count)
        : base(new float[count])
    {
        //Measurements = new List<float>(new float[count]);
    }

    public PropertyMeasurements(IList<float> measurements)
        : base(measurements)
    {
        //        if (IsInValidMeasureRange(measurements))
        //
        //            this.Measurements = new List<float>(measurements);
        //        else
        //            throw new ArgumentException("Invalid Measurements");
        if (IsInValidMeasureRange(measurements) == false)
        {
            throw new ArgumentException("Invalid Measurements");
        }
    }

    public static PropertyMeasurements Average(
        IList<PropertyMeasurements> measurementInstances)
    {
        if (measurementInstances == null
            || measurementInstances.Count() <= 0)
        {
            return null;
        }

        PropertyMeasurements averageMeasurement =
            new PropertyMeasurements(
                measurementInstances.First().Count);

        foreach (var propertyMeasuremnts in measurementInstances)
        {
            averageMeasurement += propertyMeasuremnts;
        }
        averageMeasurement.DividceEach(measurementInstances.Count);
        return averageMeasurement;
    }

    public static PropertyMeasurements Average(
      PropertyMeasurements one, PropertyMeasurements two)
    {
        PropertyMeasurements averageMeasurement =
            new PropertyMeasurements(one.Count);
        averageMeasurement += one;
        averageMeasurement += two;
        averageMeasurement.DividceEach(2);
        return averageMeasurement;
    }

    public static PropertyMeasurements operator +(
PropertyMeasurements one
, PropertyMeasurements two)
    {
        //        if (one.Measurements.Count != two.Measurements.Count)
        //            throw new System.ArgumentException();
        if (one.Count != two.Count)
            throw new System.ArgumentException();
        else
        {
            PropertyMeasurements levelPropertyMeasurements =
                new PropertyMeasurements(one.Count);

            for (int i = 0; i < one.Count; i++)
            {
                levelPropertyMeasurements[i]
                    = one[i] + two[i];
            }
            return levelPropertyMeasurements;
        }
    }

    public void DividceEach(float a)
    {
        for (int i = 0; i < this.Count; i++)
        {
            this[i] /= a;
        }
    }

    public float AveragePropertyDistance(
        PropertyMeasurements other
        )
    {
        float avgDitance = 0;
        for (int i = 0; i < other.Count; i++)
        {
            avgDitance += PropertyDistance(other, i);
        }
        avgDitance /= (float)this.Count;
        return avgDitance;
    }

    //Distance is always an ABSOLUTE value
    public float PropertyDistance(PropertyMeasurements other, int i)
    {
        return MathF.Abs(other[i] - this[i]);
    }

    //The raw signed change occuring on a property
    public float PropertyChange(PropertyMeasurements other, int i)
    {
        return other[i] - this[i];
    }

    /// <summary>
    /// Checks if measurements are all in the range of
    /// 0 ... 1
    /// </summary>
    /// <param name=""></param>
    private static bool IsInValidMeasureRange(IList<float> measures)
    {
        foreach (var measure in measures)
        {
            if (measure < 0 || measure > 1)
                return false;
        }
        return true;
    }
}

//Refresh selection list
//    }
//}