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
using Codice.CM.SEIDInfo;
using StealthLevelEvaluation;
using UnityEditor;
using GeneticSharp.Domain.Randomizations;
using System.Text;
using System.Runtime.CompilerServices;

internal class CustomMutators : MutationBase
{
    public bool IsOrdered => true;
    private UniformMutation WholeGeneUniformMutation = new UniformMutation(true);
    private float[] Probabilities = new float[3];

    public CustomMutators(float weightAdd, float weightRemove, float weightRandom)
    {
        float total = weightAdd + weightRemove + weightRandom;
        Probabilities[0] = weightAdd / total;
        Probabilities[1] = weightRemove / total;
        Probabilities[2] = weightRandom / total;
    }

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
            WholeGeneUniformMutation.Mutate(chromosome, probability);
            Debug.Log("Changed Obstacles Mutation");
        }
    }
}

//Given an level phenotype generator, population count and level size
// spreads levels manifestations in a grid. Used by all phenotype evalutions
// to trigger the level generations when needed
public class GridObjectLayout
{
    public GridObjectLayout(LevelProperties levelProperties)
    {
        this.LevelProperties = levelProperties;
    }

    private LevelProperties LevelProperties;
    private GameObject[,] LevelObjects;

    private int currentIndex = -1;
    private int GridDimension;

    public GameObject GetNextLevelObject()
    {
        currentIndex++;
        if (currentIndex >= GridDimension * GridDimension)
            return null;
        return LevelObjects[currentIndex / GridDimension, currentIndex % GridDimension];
    }

    public Vector2 ExtraSpacing = Vector2.zero;

    public void SpawnGrid(int populationCount, Transform transform)
    {
        GridDimension = Mathf.CeilToInt(Mathf.Sqrt(populationCount));

        //Setup Generator Prototype
        LevelObjects = new GameObject[GridDimension, GridDimension];

        for (int i = 0; i < GridDimension; i++)
        {
            for (int j = 0; j < GridDimension; j++)
            {
                Vector3 levelGridPosition =
                    new Vector3(
                        i * (LevelProperties.LevelSize.x + ExtraSpacing.x),
                        j * (LevelProperties.LevelSize.y + ExtraSpacing.y),
                        0);
                var g = new GameObject($"{i * GridDimension + j}");
                g.transform.position = levelGridPosition;
                g.transform.parent = transform;
                LevelObjects[i, j] = g;
            }
        }
    }

    public void PrepareForNewGeneration()
    {
        //Clearing old data
        DisposeOldPopulation();
        //Resetting index
        currentIndex = -1;
    }

    //Once a new population has been started the gameobject generated must be cleared
    private void DisposeOldPopulation()
    {
        Debug.Log("Disposing generated artefacts of previous levels");
        foreach (var item in LevelObjects)
        {
            var tempList = item.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
}

public class PopulationLevelGridInitalizer : MonoBehaviour
{
    //public int Rows = 5; // Number of rows in the grid
    //public int Columns = 5; // Number of columns in the grid

    public int PopulationCount;
    public int AimedGenerations = 10;

    [Range(0, 1)]
    public float MutationProb = 10;

    [Range(0, 1)]
    public float CrossoverProb = 10;

    public EvaluatorPrefabSpawner PhenotypeEvaluator;
    public LevelPhenotypeGenerator Generator;
    public LevelProperties LevelProperties;
    private GridObjectLayout GridPopulation;

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
    public Vector2 ExtraSpacing;

    public void Start()
    {
        if (RandomizeSeed)
            Seed = new System.Random().Next();
        RandomSeedGenerator = new System.Random(Seed);
        if (LogExecutions)
        {
            string algoName = $"GEN_{AimedGenerations}_POP{PopulationCount}_SZ{LevelProperties.LevelSize}";
            float[] runs = Helpers.TrackExecutionTime(Run, LogExecutionTimes);
            Helpers.SaveRunToCsv($"Tests/{algoName}.txt", runs);
        }
        else
        {
            Run();
        }
    }

    private void ManifestTopLevels(List<IChromosome> chromosomes)
    {
        //Start with y down
        Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);
        for (int i = 0; i < chromosomes.Count; i++)
        {
            TopLevelsPos += new Vector3(25, 0, 0);
            var level = Instantiate(Generator, TopLevelsPos,
                Quaternion.identity, this.transform);
            level.gameObject.name = $"Top {i} - {chromosomes[i].Fitness}";
            var levelChromosome = (LevelChromosome)chromosomes[i];
            levelChromosome.PhenotypeGenerator.Generate(levelChromosome, level.gameObject);
            //level.Generate(levelChromosome);

            //Assign fitness info object showing the exact values achieved
            //without need to recalcuate (RRT may produce different resutls)
            var infoObj = FitnessInfoVisualizer.AttachInfo(level.gameObject,
                levelChromosome.FitnessInfo);

            //Try to recreate evaluators with given data
            //            GameObject evaluator = new GameObject("Evaluator");
            //            evaluator.transform.parent = level.transform;
            //            foreach (var e in infoObj.FitnessEvaluations)
            //            {
            //                evaluator.AddComponent(e.GetType());
            //            }
        }
    }

    private void ChekcAvailabilitOfFitnessInfo()
    {
        List<IChromosome> chromosomes = this.GeneticAlgorithm.Population.Generations.SelectMany(x => x.Chromosomes).ToList();
        //Start with y down
        Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);
        for (int i = 0; i < chromosomes.Count; i++)
        {
            var levelChromosome = (LevelChromosome)chromosomes[i];
            foreach (var e in levelChromosome.FitnessInfo.FitnessEvaluations)
            {
                if (e.Phenotype == null)
                {
                    int a = 3;
                }
            }
        }
    }

    private void Run()
    {
        var selection = new TournamentSelection(3, true);
        var crossover = new TwoPointCrossover();
        var mutation = new CustomMutators(1, 1, 1);
        //var chromosome = new FloatingPointChromosome(0,1,35,8);
        var chromosome = new LevelChromosome(Generator, RandomSeedGenerator);
        var population = new Population(PopulationCount, PopulationCount, chromosome);

        if (GridPopulation != null)
        {
            GridPopulation.PrepareForNewGeneration();
        }
        else
        {
            GridPopulation = new GridObjectLayout(LevelProperties);
            GridPopulation.ExtraSpacing = ExtraSpacing;
            GridPopulation.SpawnGrid(PopulationCount, this.transform);
        }
        PhenotypeEvaluator.GridLevelObjects = GridPopulation;

        GeneticAlgorithm = new GeneticAlgorithm(population, PhenotypeEvaluator, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = MutationProb;
        GeneticAlgorithm.CrossoverProbability = CrossoverProb;
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
        int n = 0;
        foreach (var top in topN)
        {
            n++;
            Debug.Log($"Top {n} - Fitness {top.Fitness}");
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
        StringBuilder header = new StringBuilder();
        var bestInfo = (LevelChromosome)GeneticAlgorithm.BestChromosome;
        header.Append($"Chromosome Hash,");
        foreach (var e in bestInfo.FitnessInfo.FitnessEvaluations)
        {
            header.Append($"{e.Name} Evaluation,");
            header.Append($"{e.Name} Time,");
        }
        header.Remove(header.Length - 1, 0);

        string values = string.Empty;
        foreach (var gen in GeneticAlgorithm.Population.Generations)
        {
            foreach (var c in gen.Chromosomes)
            {
                values += $"{c.GetHashCode()},";
                FitnessInfo info = ((LevelChromosome)c).FitnessInfo;

                foreach (var e in info.FitnessEvaluations)
                {
                    values += $"{e.Value},";
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
        ChekcAvailabilitOfFitnessInfo();
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
            GridPopulation.PrepareForNewGeneration();
        }
    }
}