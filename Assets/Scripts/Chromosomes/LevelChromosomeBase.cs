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

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

[Serializable]
public abstract class LevelChromosomeBase : ChromosomeBase
{
    public System.Random ChromosomeRandom;

    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
        Measurements = new List<MeasureResult>();
        //AestheticProperties =  new List<float>();
        AestheticProperties = new PropertyMeasurements(0);
    }

    public LevelPhenotypeGenerator PhenotypeGenerator;

    //The decoded game object - could be null if not generated
    public GameObject Phenotype { get; set; }

    [SerializeField] public List<MeasureResult> Measurements;
    [SerializeField] public PropertyMeasurements AestheticProperties;
    [SerializeField] public Graph<Vector2> EnemyRoadmap;

    public override IChromosome Clone()
    {
        //var clone =new LevelChromosome(Length, PhenotypeGenerator);
        var clone = (LevelChromosomeBase)base.Clone();
        clone.Measurements = this.Measurements;
        clone.AestheticProperties = this.AestheticProperties;
        clone.EnemyRoadmap = this.EnemyRoadmap;
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
}

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