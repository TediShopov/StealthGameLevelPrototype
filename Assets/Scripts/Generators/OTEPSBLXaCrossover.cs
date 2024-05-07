using GeneticSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OTEPSBLXaCrossover : CrossoverBase
{
    [SerializeField] private float alpha;

    public OTEPSBLXaCrossover() : base(2, 2)
    {
    }
    public OTEPSBLXaCrossover(float alpha)
        : base(2, 2)
    {
        if (alpha <= 0 || alpha >= 1)
            throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be in the range (0, 1).");

        this.alpha = alpha;
    }
    protected IList<IChromosome> CrossParents(IChromosome parent1,
        IChromosome parent2)
    {
        var childOne = parent1.CreateNew();
        var childTwo = parent1.CreateNew();
        for (int i = 0; i < Math.Min(parent1.Length, parent2.Length); i++)
        {
            var minGene = Mathf.Min((float)parent1.GetGene(i).Value, (float)parent2.GetGene(i).Value);
            var maxGene = Mathf.Max((float)parent1.GetGene(i).Value, (float)parent2.GetGene(i).Value);

            var range = maxGene - minGene;
            var min = Mathf.Clamp01(minGene - range * alpha);
            var max = Mathf.Clamp01(maxGene + range * alpha);

            childOne.ReplaceGene(i, new Gene(RandomInRange(min, max)));
            childTwo.ReplaceGene(i, new Gene(RandomInRange(min, max)));
        }
        return new List<IChromosome> { childOne, childTwo };
    }
    public float RandomInRange(float min, float max)
    {
        return (float)RandomizationProvider.Current.GetFloat() * (max - min) + min;
    }

    protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
    {
        try
        {
            return CrossParents(parents[0], parents[1]);
        }
        catch (Exception)
        {
            throw;
        }
    }
}