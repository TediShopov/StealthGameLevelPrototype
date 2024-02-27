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
    //public int Rows = 5; // Number of rows in the grid
    //public int Columns = 5; // Number of columns in the grid

    public int PopulationCount;
    public int AimedGenerations = 10;
    public TargetRRTSuccessEvaluation PhenotypeEvaluator;
    public LevelProperties LevelProperties;

    [Header("Seed")]
    public bool RandomizeSeed;

    public int Seed;
    public System.Random RandomSeedGenerator;

    [Header("Logging")]
    public bool LogExecutions = false;

    public bool LogIndividualExecutions = false;
    public int LogExecutionTimes = 0;
    public int TopNLevels = 5;

    private GeneticAlgorithm GeneticAlgorithm;

    public void Start()
    {
        if (RandomizeSeed)
            RandomSeedGenerator = new System.Random();
        else
            RandomSeedGenerator = new System.Random(Seed);
        if (LogExecutions)
        {
            string algoName = $"GEN_{AimedGenerations}_POP{PopulationCount}_SZ{LevelProperties.LevelSize}";
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
            var level = Instantiate(PhenotypeEvaluator.LevelGeneratorPrototype, TopLevelsPos,
                Quaternion.identity, this.transform);
            level.gameObject.name = $"Top {i} - {chromosomes[i].Fitness}";
            var levelChromosome = (LevelChromosome)chromosomes[i];
            level.Generate(levelChromosome);

            //Assign fitness info object showing the exact values achieved
            //without need to recalcuate (RRT may produce different resutls)
            var infoObj = FitnessInfoVisualizer.AttachInfo(level.gameObject,
                levelChromosome.FitnessInfo);
        }
    }

    private void Run()
    {
        var selection = new TournamentSelection(3, true);
        var crossover = new TwoPointCrossover();
        var mutation = new UniformMutation(true);
        //var chromosome = new FloatingPointChromosome(0,1,35,8);
        var chromosome = new LevelChromosome(35, RandomSeedGenerator);
        var population = new Population(PopulationCount, PopulationCount, chromosome);
        PhenotypeEvaluator.LevelProperties = LevelProperties;
        PhenotypeEvaluator.SpawnGridOfEmptyGenerators(PopulationCount);
        PhenotypeEvaluator.PrepareForNewGeneration();

        GeneticAlgorithm = new GeneticAlgorithm(population, PhenotypeEvaluator, selection, crossover, mutation);
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
        List<IChromosome> topN = GeneticAlgorithm.Population.Generations.SelectMany(x => x.Chromosomes)
            .Distinct().
            OrderByDescending(x => x.Fitness).Take(TopNLevels).ToList();
        for (int i = 0; i < TopNLevels; i++)
        {
            Debug.Log($"Top {i} - Fitness {topN[i].Fitness}");
        }
        return topN;
    }

    private void LogFinessFunctionInfo()
    {
        foreach (var gen in GeneticAlgorithm.Population.Generations)
        {
            foreach (var c in gen.Chromosomes)
            {
                FitnessInfo info = ((LevelChromosome)c).FitnessInfo;
                string chromosomeInfo = $"Generation {gen.Number} - Fitness {c.Fitness}";
                foreach (var e in info.FitnessEvaluations)
                {
                    chromosomeInfo += $" {e.Name} {e.Value} {e.Time}  ";
                }
                Debug.Log(chromosomeInfo);
            }
        }
    }

    private void OutputEvaluationTimesToCsv()
    {
        string header = string.Empty;
        var bestInfo = (LevelChromosome)GeneticAlgorithm.BestChromosome;
        foreach (var e in bestInfo.FitnessInfo.FitnessEvaluations)
        {
            header += $"{e.Name},";
        }

        string values = string.Empty;
        foreach (var gen in GeneticAlgorithm.Population.Generations)
        {
            foreach (var c in gen.Chromosomes)
            {
                FitnessInfo info = ((LevelChromosome)c).FitnessInfo;
                foreach (var e in info.FitnessEvaluations)
                {
                    values += $"{e.Time},";
                }
                values += "\n";
            }
        }
        string algoName = $"GEN_{AimedGenerations}_POP{PopulationCount}_SZ{LevelProperties.LevelSize}_IndividualTimes";
        Helpers.SaveToCSV($"Tests/{algoName}.txt", header + "\n" + values);
    }

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        List<IChromosome> chromosomes = GetTopLevelsFitness();
        ManifestTopLevels(chromosomes);
        if (LogIndividualExecutions)
        {
            OutputEvaluationTimesToCsv();
        }
    }

    private void Ga_GenerationRan(object sender, EventArgs e)
    {
        Debug.Log($"{GeneticAlgorithm.GenerationsNumber} Generation Ran");
        //Do not discard the last generation before termination
        if (GeneticAlgorithm.GenerationsNumber != AimedGenerations)
        {
            PhenotypeEvaluator.PrepareForNewGeneration();
        }
    }
}