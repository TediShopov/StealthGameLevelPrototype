using GeneticSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OTEPSVariableLenghtMutator : MutationBase
{
    private float[] _probabilities = new float[3];
    private UniformMutation _wholeGeneUniformMutation = new UniformMutation(true);

    public OTEPSVariableLenghtMutator(
        float weightAdd,
        float weightRemove,
        float weightRandom)
    {
        float total = weightAdd + weightRemove + weightRandom;
        _probabilities[0] = weightAdd / total;
        _probabilities[1] = weightRemove / total;
        _probabilities[2] = weightRandom / total;
    }

    public bool IsOrdered => true;

    protected override void PerformMutate(IChromosome chromosome, float probability)
    {
        double isMutating = RandomizationProvider.Current.GetDouble(0, 1); // Random number between 0 and 1
        if (isMutating >= probability) return;
        //Uniform chance to pick one of 4 mutation strategiesweightAdd
        double randomNumber = RandomizationProvider.Current.GetDouble(0, 1); // Random number between 0 and 1
        double cumulativeProbability = 0;
        int chosenOutcome = 0;

        for (int i = 0; i < _probabilities.Length; i++)
        {
            cumulativeProbability += _probabilities[i];

            if (randomNumber < cumulativeProbability)
            {
                chosenOutcome = i;
                break;
            }
        }
        if (chosenOutcome == 0)
        {
            int oldLength = chromosome.Length;
            //Add obstacle
            chromosome.Resize(chromosome.Length + 5);
            chromosome.ReplaceGene(oldLength, chromosome.GenerateGene(oldLength));
            chromosome.ReplaceGene(oldLength + 1, chromosome.GenerateGene(oldLength + 1));
            chromosome.ReplaceGene(oldLength + 2, chromosome.GenerateGene(oldLength + 2));
            chromosome.ReplaceGene(oldLength + 3, chromosome.GenerateGene(oldLength + 3));
            chromosome.ReplaceGene(oldLength + 4, chromosome.GenerateGene(oldLength + 4));
            chromosome.ValidateGenes();
            Debug.Log("Added Obstacles Mutation");
        }
        if (chosenOutcome == 1)
        {
            //Remove obstacle
            chromosome.Resize(chromosome.Length - 5);
            Debug.Log("Removed Obstacles Mutation");
        }
        if (chosenOutcome == 2)
        {
            //Randomize obstacle
            try
            {
                ;
                _wholeGeneUniformMutation = new UniformMutation(
                    Enumerable.Range(0, chromosome.Length).ToArray());
                _wholeGeneUniformMutation.Mutate(chromosome, probability);
                Debug.Log("Changed Obstacles Mutation");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}