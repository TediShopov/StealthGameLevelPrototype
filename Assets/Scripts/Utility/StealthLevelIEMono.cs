using Codice.Client.BaseCommands.Merge;
using GeneticSharp;
using GeneticSharp.Domain;
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

/// <summary>
///A bridge class creating interactive genetic algorithm
///while exposing controllable properties to the editor.
/// </summary>
//public class InteractiveGeneticAlgorithm : MonoBehaviour
//{
//    //#endregion Events
//    //
//    //    public UserPrefereneceTracker PreferenceTracker;
//    //
//    //    public bool IsRunning => GeneticAlgorithm != null
//    //        && GeneticAlgorithm.IsRunning;
//    //
//}

public class NativeRandom : IRandomization
{
    private System.Random _random;

    public NativeRandom()
    {
        _random = new System.Random();
    }

    public NativeRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    public double GetDouble()
    {
        return _random.NextDouble();
    }

    public double GetDouble(double min, double max)
    {
        // Get a random double between min and max
        return min + _random.NextDouble() * (max - min);
    }

    public float GetFloat()
    {
        // Convert a random double to float
        return (float)_random.NextDouble();
    }

    public float GetFloat(float min, float max)
    {
        // Get a random float between min and max
        return min + (float)(_random.NextDouble() * (max - min));
    }

    public int GetInt(int min, int max)
    {
        // Return a random integer between min (inclusive) and max (exclusive)
        return _random.Next(min, max);
    }

    public int[] GetInts(int length, int min, int max)
    {
        int[] result = new int[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = GetInt(min, max);
        }
        return result;
    }

    public int[] GetUniqueInts(int length, int min, int max)
    {
        // Ensure we have enough range to get unique integers
        if (max - min < length)
        {
            throw new ArgumentException("Range is too small for the requested number of unique integers.");
        }

        HashSet<int> uniqueInts = new HashSet<int>();
        while (uniqueInts.Count < length)
        {
            uniqueInts.Add(GetInt(min, max));
        }

        return uniqueInts.ToArray();
    }
}

internal class CustomMutators : MutationBase
{
    private float[] Probabilities = new float[3];
    private UniformMutation WholeGeneUniformMutation = new UniformMutation(true);

    public CustomMutators(float weightAdd, float weightRemove, float weightRandom)
    {
        float total = weightAdd + weightRemove + weightRandom;
        Probabilities[0] = weightAdd / total;
        Probabilities[1] = weightRemove / total;
        Probabilities[2] = weightRandom / total;
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

        for (int i = 0; i < Probabilities.Length; i++)
        {
            cumulativeProbability += Probabilities[i];

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
                WholeGeneUniformMutation = new UniformMutation(
                    Enumerable.Range(0, chromosome.Length).ToArray());
                WholeGeneUniformMutation.Mutate(chromosome, probability);
                Debug.Log("Changed Obstacles Mutation");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

//Given an level phenotype generator, population count and level size
// spreads levels manifestations in a grid. Used by all phenotype evalutions
// to trigger the level generations when needed