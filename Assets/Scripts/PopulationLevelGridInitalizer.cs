using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Domain;
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
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
    public int AimedGenerations = 10;
    public GeneticAlgorithm GeneticAlgorithm;
    public bool LogExecutionAvg = false;
    public int LogExecutionTimes = 0;
    public int TopNLevels = 5;
    //private List<LevelPhenotypeGenerator> _topFiveLevelPhenotypeGenerators;

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

    private void ManifestTopLevels(List<IChromosome> chromosomes)
    {
        //Start with y down
        Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);
        for (int i = 0; i < chromosomes.Count; i++)
        {
            TopLevelsPos += new Vector3(25, 0, 0);
            var level = Instantiate(targetRRTSuccessEvaluation.LevelGeneratorPrototype, TopLevelsPos,
                Quaternion.identity, this.transform);
            level.gameObject.name = $"Top {i} - {chromosomes[i].Fitness}";
            level.Generate((LevelChromosome)chromosomes[i]);
        }
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
        targetRRTSuccessEvaluation.PrepareForNewGeneration();

        GeneticAlgorithm = new GeneticAlgorithm(population, targetRRTSuccessEvaluation, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = 0.2f;
        GeneticAlgorithm.Termination = new GenerationNumberTermination(AimedGenerations);
        GeneticAlgorithm.GenerationRan += Ga_GenerationRan;
        GeneticAlgorithm.TerminationReached += Ga_TerminationReached; ;
        GeneticAlgorithm.Start();
    }

    private List<IChromosome> GetTopLevelsFitness()
    {
        foreach (var gen in GeneticAlgorithm.Population.Generations)
        {
            foreach (var c in gen.Chromosomes)
            {
                Debug.Log($"Generation {gen.Number} - Fitness {c.Fitness}");
            }
        }
        List<IChromosome> top5 = GeneticAlgorithm.Population.Generations.SelectMany(x => x.Chromosomes)
            .Distinct().
            OrderByDescending(x => x.Fitness).Take(TopNLevels).ToList();
        for (int i = 0; i < 5; i++)
        {
            Debug.Log($"Top {i} - Fitness {top5[i].Fitness}");
        }
        return top5;
    }

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        //Debug.Log($"Best solution found has {GeneticAlgorithm.BestChromosome.Fitness} fitness.");
        //        int iter = 0;
        //        foreach (var evaluatedChromosome in targetRRTSuccessEvaluation.TopLevels)
        //        {
        //            iter++;
        //            Debug.Log($"Level soultion {iter} - {evaluatedChromosome.Fitness}");
        //        }
        //Debug.Log($"Best solution found has {GeneticAlgorithm.BestChromosome.Fitness} fitness.");
        List<IChromosome> chromosomes = GetTopLevelsFitness();
        ManifestTopLevels(chromosomes);
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