using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OTEPSGaussianMutator : MutationBase
{
    public float Mean;
    public float StdDev;
    //SRC - https://www.alanzucconi.com/2015/09/16/how-to-sample-from-a-gaussian-distribution/
    public static float NextGaussian()
    {
        float v1, v2, s;
        do
        {
            v1 = 2.0f * RandomizationProvider.Current.GetFloat() - 1.0f;
            v2 = 2.0f * RandomizationProvider.Current.GetFloat() - 1.0f;
            s = v1 * v1 + v2 * v2;
        } while (s >= 1.0f || s == 0f);
        s = Mathf.Sqrt((-2.0f * Mathf.Log(s)) / s);

        return v1 * s;
    }
    public static float NextGaussian(float mean, float standard_deviation)
    {
        return mean + NextGaussian() * standard_deviation;
    }

    protected override void PerformMutate(IChromosome chromosome, float probability)
    {
        //Mutate all
        for (int i = 0; i < chromosome.Length; i++)
        {
            if (RandomizationProvider.Current.GetDouble() <= probability)
            {
                try
                {
                    float randomFromGaussian =
                        NextGaussian(Mean, StdDev);
                    randomFromGaussian = Mathf.Clamp01(randomFromGaussian);
                    chromosome.ReplaceGene(i, new Gene(randomFromGaussian)); ;
                }
                catch (System.Exception)
                {
                    throw;
                }
            }
        }
    }
}