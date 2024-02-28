using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeneticSharp.Domain.Chromosomes;

public class LevelChromosome : LevelChromosomeBase
{
    private System.Random ChromosomeRandom = new System.Random();

    public Gene[] GetRandomGenes(int length)
    {
        Gene[] genes = new Gene[length];
        for (int i = 0; i < length; i++)
        {
            genes[i] = new Gene(Helpers.GetRandomFloat(ChromosomeRandom, 0f, 1f));
        }
        return genes;
    }

    public LevelChromosome(LevelPhenotypeGenerator generatorBase = null, System.Random random = null) : base(generatorBase.ObstaclesSpawned * 5 + 4, generatorBase)
    {
        if (random == null)
        {
            ChromosomeRandom = new System.Random();
        }
        else
        {
            ChromosomeRandom = random;
        }
        this.ReplaceGenes(0, GetRandomGenes(Length));
    }

    public override IChromosome CreateNew()
    {
        return new LevelChromosome(PhenotypeGenerator, ChromosomeRandom);
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(Helpers.GetRandomFloat(ChromosomeRandom, 0f, 1f));
    }
}