using CGALDotNet;
using GeneticSharp;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.TextCore.Text;
using UnityEngine;

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
            if (item == null) continue;
            var tempList = item.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
}

public class StealthLevelIEMono : MonoBehaviour
{
    //public int Rows = 5; // Number of rows in the grid
    //public int Columns = 5; // Number of columns in the grid

    public int PopulationCount;
    public int AimedGenerations = 10;

    [Range(0, 1)]
    public float MutationProb = 10;

    [Range(0, 1)]
    public float CrossoverProb = 10;

    public EvaluatorMono PhenotypeEvaluator;
    public LevelPhenotypeGenerator Generator;
    public LevelProperties LevelProperties;
    private GridObjectLayout GridPopulation;

    [Header("Seed")]
    public int Seed;

    public System.Random RandomSeedGenerator;

    [Header("Logging")]
    public bool LogExecutions = false;

    public bool LogIndividualExecutions = false;
    public int LogExecutionTimes = 0;
    public int TopNLevels = 5;

    private InteractiveGeneticAlgorithm GeneticAlgorithm;
    public Vector2 ExtraSpacing;

    public void Start()
    {
        InteractiveSelections = new HashSet<Tuple<int, LevelChromosome>>();
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

    public void RandomizeSeed()
    {
        Seed = new System.Random().Next();
    }

    private HashSet<Tuple<int, LevelChromosome>> InteractiveSelections;

    public void SelectChromosome(LevelChromosome chromosome)
    {
        if (GeneticAlgorithm.IsRunning)
        {
            InteractiveSelections.Add(
                new Tuple<int, LevelChromosome>(
                    GeneticAlgorithm.GenerationsNumber,
                    chromosome));

            //Change the wieght preference of the evaluator
            LevelMeasuredProperties levelMeasuredProperties = chromosome.Properties;
            float step = 0.2f;
            var changeInWeight = step *
                (levelMeasuredProperties.SuccessChance - AverageLevelPreferences().SuccessChance);
            var newWeight = levelMeasuredProperties.SuccessChance + changeInWeight;
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
            ChromoseMeasurementsVisualizer.AttachDataVisualizer(level.gameObject);
        }
    }

    private void ChekcAvailabilitOfFitnessInfo()
    {
        List<IChromosome> chromosomes = this.GeneticAlgorithm.Population.Generations
            .SelectMany(x => x.Chromosomes).ToList();
        //Start with y down
        Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);
        for (int i = 0; i < chromosomes.Count; i++)
        {
            var levelChromosome = (LevelChromosome)chromosomes[i];
            if (levelChromosome is null || levelChromosome.Measurements is null)
            {
                int b = 3;
            }
        }
    }

    public void Run()
    {
        SetupGA();
        GeneticAlgorithm.Population.CreateInitialGeneration();
        GeneticAlgorithm.State = GeneticAlgorithmState.Started;
    }

    public void SetupGA()
    {
        RandomSeedGenerator = new System.Random(Seed);
        var selection = new RouletteWheelSelection();
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

        GeneticAlgorithm = new
            InteractiveGeneticAlgorithm(population, PhenotypeEvaluator, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = MutationProb;
        GeneticAlgorithm.CrossoverProbability = CrossoverProb;
        GeneticAlgorithm.Termination = new GenerationNumberTermination(AimedGenerations);
        GeneticAlgorithm.GenerationRan += Ga_GenerationRan;
        GeneticAlgorithm.TerminationReached += Ga_TerminationReached; ;
    }

    public LevelMeasuredProperties AverageLevelPreferences()
    {
        var allValidProperties =
        this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes
            .Select(x => (LevelChromosomeBase)x);

        LevelMeasuredProperties avgProperties = new LevelMeasuredProperties();
        avgProperties.PathUniqeness = allValidProperties.Select(x => x.Properties.PathUniqeness).Average();
        avgProperties.SuccessChance = allValidProperties.Select(x => x.Properties.SuccessChance).Average();
        return avgProperties;
    }

    public void DoGeneration()
    {
        if (GeneticAlgorithm.State == GeneticAlgorithmState.Started)
        {
            //If interaction has occurred
            GeneticAlgorithm.EndCurrentGeneration();
            GridPopulation.PrepareForNewGeneration();
            GeneticAlgorithm.EvolveOneGeneration();
            //Evaluates fitness but also manifest the level
            // in the unity scene
            GeneticAlgorithm.EvaluateFitness();
        }
        else
        {
            //Refresh selection list
            this.InteractiveSelections = new HashSet<Tuple<int, LevelChromosome>>();
            GeneticAlgorithm.State = GeneticAlgorithmState.Started;
            GeneticAlgorithm.Population.CreateInitialGeneration();
            GeneticAlgorithm.EvaluateFitness();
        }
    }

    public void Dispose()
    {
        var tempList = this.transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
        this.GridPopulation = null;
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

    private void OutputEvaluationTimesToCsv()
    {
        StringBuilder header = new StringBuilder();
        var bestInfo = (LevelChromosome)GeneticAlgorithm.BestChromosome;
        header.Append($"Chromosome Hash,");
        foreach (var e in bestInfo.Measurements.FitnessEvaluations)
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
                MeasurementsData info = ((LevelChromosome)c).Measurements;
                if (info != null)
                {
                    foreach (var e in info.FitnessEvaluations)
                    {
                        values += $"{e.Value},";
                        values += $"{e.Time},";
                    }
                    values += "\n";
                }
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
    }
}