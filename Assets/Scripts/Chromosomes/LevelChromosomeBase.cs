using GeneticSharp;
using PlasticPipe.PlasticProtocol.Messages;
using StealthLevelEvaluation;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using UnityEngine;

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

[Serializable]
public abstract class LevelChromosomeBase : ChromosomeBase
{
    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
        Measurements = new List<MeasureResult>();
        AestheticProperties = new List<float>();
    }

    public LevelPhenotypeGenerator PhenotypeGenerator;

    //The decoded game object - could be null if not generated
    public GameObject Phenotype { get; set; }

    [SerializeField] public List<MeasureResult> Measurements;
    [SerializeField] public List<float> AestheticProperties;
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

//Refresh selection list
//    }
//}