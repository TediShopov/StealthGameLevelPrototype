using GeneticSharp;
using StealthLevelEvaluation;
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEngagementMeasurable
{
    public float EngagementScore { get; set; }
}

public interface ILevelChromosome :
    IEngagementMeasurable,
    IAestheticMeasurable<ILevelChromosome>
{
}

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

[Serializable]
public abstract class LevelChromosomeBase : ChromosomeBase,
    IAestheticMeasurable<LevelChromosomeBase>
{
    [HideInInspector] public System.Random ChromosomeRandom;
    [SerializeField] public GameObject Manifestation;

    //The decoded game object - could be null
    [SerializeField] public Dictionary<string, MeasureResult> Measurements;

    [SerializeField] public LevelPhenotype Phenotype;
    [SerializeField, HideInInspector] public LevelPhenotypeGenerator PhenotypeGenerator;
    [SerializeReference] public PropertyMeasurements AestheticProperties;
    public float AestheticScore { get; set; }
    public float EngagementScore { get; set; }

    [SerializeField]
    private bool _feas;

    public bool Feasibility
    {
        get { return _feas; }
        set { _feas = value; }
    }

    //public bool Feasibility { get; set; }

    //    public LevelChromosomeBase(LevelChromosomeBase other)
    //        : base(other.Length)
    //    {
    //        this.Transfer(other);
    //    }
    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
        Measurements = new Dictionary<string, MeasureResult>();
        AestheticProperties = new PropertyMeasurements(0);
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
        clone.Phenotype = this.Phenotype;
        clone.Manifestation = this.Manifestation;
        clone.Feasibility = this.Feasibility;
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
    public float GetAestheticScore(IPreferenceModel<LevelChromosomeBase> model)
    {
        AestheticScore = CalculateAestheticScore(model.Weights, GetMeasurements());
        return AestheticScore;
    }
    public PropertyMeasurements GetMeasurements()
    {
        return AestheticProperties;
    }
    public void Transfer(LevelChromosomeBase other)
    {
        this.Measurements = other.Measurements;
        this.AestheticProperties = other.AestheticProperties;
        this.AestheticScore = other.AestheticScore;
        this.Phenotype = other.Phenotype;
        this.Manifestation = other.Manifestation;
        this.EngagementScore = other.EngagementScore;
        this.Fitness = other.Fitness;
        this.Feasibility = other.Feasibility;
    }
}