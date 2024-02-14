using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Domain;
using System;
using UnityEngine;

public class PopulationLevelGridInitalizer : MonoBehaviour
{
    public int Rows = 5; // Number of rows in the grid
    public int Columns = 5; // Number of columns in the grid
    public int Seed;
    public bool RandomizeSeed;

    public TargetRRTSuccessEvaluation targetRRTSuccessEvaluation;
    public Vector2 LevelSize = new Vector2(1.0f, 1.0f); // Size of each object
    public System.Random RandomSeedGenerator;
    public int AimedGenerations = 10;
    public GeneticAlgorithm GeneticAlgorithm;
    public bool LogExecutionAvg = false;
    public int LogExecutionTimes = 0;

    //Flag to indicated occured termiantion to stop re-running on terminated algorithm
    private bool _terminated = false;

    public void Start()
    {
        if (LogExecutionAvg)
        {
            string algoName = $"GEN_{AimedGenerations}_POP{Rows * Columns}_SZ{LevelSize}";
            float[] runs = Helpers.TrackExecutionTime(Run, LogExecutionTimes);
            Helpers.SaveRunToCsv($"Tests/{algoName}.txt", runs);
        }
        Run();
    }

    private void Run()
    {
        var selection = new TournamentSelection(3, true);
        var crossover = new TwoPointCrossover();
        var mutation = new UniformMutation(true);
        //var chromosome = new FloatingPointChromosome(0,1,35,8);
        var chromosome = new LevelChromosome(35);
        var population = new Population(Rows * Columns, Rows * Columns, chromosome);
        targetRRTSuccessEvaluation.SpawnGridOfEmptyGenerators(Rows * Columns);

        GeneticAlgorithm = new GeneticAlgorithm(population, targetRRTSuccessEvaluation, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = 0.2f;
        GeneticAlgorithm.Termination = new GenerationNumberTermination(AimedGenerations);
        GeneticAlgorithm.GenerationRan += Ga_GenerationRan;
        GeneticAlgorithm.TerminationReached += Ga_TerminationReached; ;
        GeneticAlgorithm.Start();
    }

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        Debug.Log($"Best solution found has {GeneticAlgorithm.BestChromosome.Fitness} fitness.");
        _terminated = true;
    }

    private void Ga_GenerationRan(object sender, EventArgs e)
    {
        Debug.Log($"{GeneticAlgorithm.GenerationsNumber} Generation Ran");
        //Do not discard the last generation before termination
        if (GeneticAlgorithm.GenerationsNumber != AimedGenerations)
        {
            targetRRTSuccessEvaluation.PrepareForNewGeneration();
        }
        //GeneticAlgorithm.Stop();
    }
}