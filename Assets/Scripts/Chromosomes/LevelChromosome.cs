using UnityEngine;
using GeneticSharp;

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

    public LevelChromosome(LevelPhenotypeGenerator generatorBase, System.Random random) :
        this(random.Next(5, 15), generatorBase, random)
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

    public LevelChromosome(int entityLength, LevelPhenotypeGenerator generatorBase = null, System.Random random = null) :
        base(entityLength * 5 + 1, generatorBase)
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
        return new LevelChromosome(Length / 5, PhenotypeGenerator, ChromosomeRandom);
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(Helpers.GetRandomFloat(ChromosomeRandom, 0f, 1f));
    }

    public override int GetHashCode()
    {
        int hash = 0;
        Gene[] genes = GetGenes();
        foreach (Gene gene in genes)
        {
            float number = (float)gene.Value;
            int scaledNumber = Mathf.RoundToInt(number / 0.0001f);
            hash ^= (hash << 5) ^ (hash >> 3) ^ scaledNumber;
        }
        return hash;
    }
}