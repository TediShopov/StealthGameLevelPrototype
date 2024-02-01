using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GeneticSharp.Domain.Chromosomes;

public class PopulationLevelGridInitalizer : MonoBehaviour
{
    public int Rows = 5; // Number of rows in the grid
    public int Columns = 5; // Number of columns in the grid
    public int Seed;
    public bool RandomizeSeed;

    public TargetRRTSuccessEvaluation targetRRTSuccessEvaluation;
    public Vector2 LevelSize = new Vector2(1.0f, 1.0f); // Size of each object
    public System.Random RandomSeedGenerator;
    public void Start()
    {
        var selection = new TournamentSelection();
        var crossover = new TwoPointCrossover();
        var mutation = new ReverseSequenceMutation();
        var chromosome = new FloatingPointChromosome(0,1,35,8);
        var population = new Population(Rows*Columns,Rows*Columns, chromosome);

        var ga = new GeneticAlgorithm(population, targetRRTSuccessEvaluation, selection, crossover, mutation);
        ga.Termination = new GenerationNumberTermination(2);
        Debug.Log("GA running...");
        ga.Start();
        Debug.Log($"Best solution found has {ga.BestChromosome.Fitness} fitness.");
    }
    public void OnGenerationCompleted() 
    {

    }

    //public SpawnRandomStealthLevel LevelSpawnerPrefab;
    // Start is called before the first frame update
}