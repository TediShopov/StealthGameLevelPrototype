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

public class StealthLevelIEMono : MonoBehaviour
{
    //public int Rows = 5; // Number of rows in the grid
    //public int Columns = 5; // Number of columns in the grid

    //public int PopulationCount;
    public int AimedGenerations = 10;

    public int SyntheticGenerations = 5;

    [Range(0, 1)]
    public float CrossoverProb = 10;

    public Vector2 ExtraSpacing;

    public GAGenerationLogger GAGenerationLogger;

    public List<LevelChromosomeBase> GenerationSelecitons;

    public LevelPhenotypeGenerator Generator;

    public InteractiveGeneticAlgorithm GeneticAlgorithm;

    public int IndependentRuns = 5;

    public List<List<LevelChromosomeBase>> InteractiveSelections;

    public LevelProperties LevelProperties;

    public int LogEveryGenerations = 5;

    [Header("Logging")]
    public bool LogMeasurements = false;

    [Range(0, 1)]
    public float MutationProb = 10;

    public InteractiveEvalutorMono PhenotypeEvaluator;
    public PopulationPhenotypeLayout PopulationPhenotypeLayout;
    //private GridObjectLayout GridPopulation;

    public System.Random RandomSeedGenerator;

    [Header("Seed")]
    public int Seed;

    public float Step;

    public int TopNLevels = 5;

    //public DynamicUserPreferenceModel UserPreferences;
    public UserPreferenceModel UserPreferences;

    public UserPrefereneceTracker PreferenceTracker;

    public event EventHandler FinishIESetup;

    public bool IsRunning => GeneticAlgorithm != null
        && GeneticAlgorithm.IsRunning;

    public void ApplyChangesToPreferenceModel()
    {
        UserPreferences.Step = Step;
        List<LevelChromosomeBase> unselected =
            this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes
            .Select(x => (LevelChromosomeBase)x)
            .Where(x => GenerationSelecitons.Contains(x) == false) //Must not be contained by selections
            .ToList();

        if (GenerationSelecitons.Count == 0) return;

        UserPreferences.AlterPreferences(GenerationSelecitons[0], unselected);
        //UserPreferences.Alter(GenerationSelecitons, unselected);
    }

    public void Awake()
    {
        InteractiveSelections = new List<List<LevelChromosomeBase>>();
        DisposeOldPopulation();
    }

    public void DisposeOldPopulation()
    {
        var tempList = this.transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }

    public void EndGA()
    {
        this.GeneticAlgorithm = null;
        GenerationSelecitons = new List<LevelChromosomeBase>();
        InteractiveSelections = new List<List<LevelChromosomeBase>>();

        DisposeOldPopulation();
        RefreshPreferencesWeight();
    }

    public void StartGA()
    {
        this.DisposeOldPopulation();

        this.SetupGA();

        //Validate and change the preference model to default if needed
        if (IsValidUserPreferenceModel
            (UserPreferences, PhenotypeEvaluator.GetCountOfLevelProperties()) == false)
        {
            SetUserPreferencesToDefault();
        }

        //Normalize the user preferences
        UserPreferences.Normalize(UserPreferences.Weights);

        PhenotypeEvaluator.UserPreferenceModel = UserPreferences;

        GeneticSharp.RandomizationProvider.Current = new NativeRandom(Seed);

        //Attach tracker that keeps track of the weight of the user preference model
        //and their changes throughout the generations
        AttachUserPreferenceLogger();

        //Clear selections
        this.GenerationSelecitons = new List<LevelChromosomeBase>();
        this.InteractiveSelections = new List<List<LevelChromosomeBase>>();

        //Startup the interactive evolutionary algoritm
        GeneticAlgorithm.State = GeneticAlgorithmState.Started;
        GeneticAlgorithm.Population.CreateInitialGeneration();
        GeneticAlgorithm.EvaluateFitness();
    }

    public void DoGeneration()
    {
        if (GeneticAlgorithm.State == GeneticAlgorithmState.Started)
        {
            ApplyChangesToPreferenceModel();
            for (int i = 0; i < SyntheticGenerations; i++)
            {
                InteractiveSelections.Add(GenerationSelecitons);
                GenerationSelecitons.Clear();

                GeneticAlgorithm.EndCurrentGeneration();
                GeneticAlgorithm.EvolveOneGeneration();

                //Evaluates fitness but also manifest the level
                // in the unity scene
                GeneticAlgorithm.EvaluateFitness();
            }
        }
    }

    public void DoSyntheticRuns(int number)
    {
    }

    public void NameAllPhenotypeGameobjects()
    {
        Vector2 placement = new Vector2(5, 5);
        foreach (var chromosome in this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes)
        {
            var levelChromosome = chromosome as LevelChromosomeBase;
            //Attach mono behaviour to visualize the measurements
            ChromoseMeasurementsVisualizer.AttachDataVisualizer(
                levelChromosome.Phenotype,
                new Vector2(5, 5));
            //Clear objects name and replace it with new fitnessj
            this.Generator.ClearName(levelChromosome);
            this.Generator.AppendFitnessToName(levelChromosome);
        }
    }

    private static bool IsValidUserPreferenceModel(UserPreferenceModel ue, int necessaryWerghtCount)
    {
        return ue is not null && ue.Weights.Count == necessaryWerghtCount;
    }

    private void SetUserPreferencesToDefault()
    {
        if (UserPreferences is null)
            UserPreferences.SetToDefault();
        else
            this.UserPreferences =
                new UserPreferenceModel(PhenotypeEvaluator.GetCountOfLevelProperties());
    }

    private void AttachUserPreferenceLogger()
    {
        PreferenceTracker = new UserPrefereneceTracker(this.GeneticAlgorithm);
        UserPreferences.Attach(PreferenceTracker);
    }

    public void RandomizeSeed()
    {
        Seed = new System.Random().Next();
    }

    public void RefreshPreferencesWeight()
    {
        //Validate and change the preference model to default if needed
        if (IsValidUserPreferenceModel
            (UserPreferences, PhenotypeEvaluator.GetCountOfLevelProperties()) == false)
        {
            SetUserPreferencesToDefault();
        }
        //Normalize the user preferences
        UserPreferences.Normalize(UserPreferences.Weights);

        PhenotypeEvaluator.UserPreferenceModel = UserPreferences;
    }

    public void RunWithSyntheticModel()
    {
        for (int i = 0; i < IndependentRuns; i++)
        {
            DisposeOldPopulation();
            //PhenotypeEvaluator.IE = this;
            PhenotypeEvaluator.UserPreferenceModel = this.UserPreferences;
            this.GenerationSelecitons = new List<LevelChromosomeBase>();
            this.InteractiveSelections = new List<List<LevelChromosomeBase>>();

            GeneticSharp.RandomizationProvider.Current = new NativeRandom(Seed);

            SetupGA();
            GeneticAlgorithm.State = GeneticAlgorithmState.Started;
            GeneticAlgorithm.Population.CreateInitialGeneration();
            GeneticAlgorithm.EvaluateFitness();
            while (GeneticAlgorithm.State != GeneticAlgorithmState.TerminationReached)
            {
                DoGeneration();
            }
        }
    }

    public void SelectChromosome(LevelChromosomeBase chromosome)
    {
        if (GenerationSelecitons.Contains(chromosome))
        {
            GenerationSelecitons.Remove(chromosome);
        }
        else
        {
            GenerationSelecitons.Add(chromosome);
        }
    }

    public void SetupGA()
    {
        GAGenerationLogger = null;
        RandomSeedGenerator = new System.Random(Seed);
        var selection = new TournamentSelection(3);
        var crossover = new TwoPointCrossover();
        var mutation = new CustomMutators(1, 1, 1);
        var chromosome = Generator.GetAdamChromosome(RandomSeedGenerator.Next());
        var population = new PopulationPhenotypeLayout(PopulationPhenotypeLayout, this.gameObject, chromosome);

        GeneticAlgorithm = new
            InteractiveGeneticAlgorithm(population, PhenotypeEvaluator, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = MutationProb;
        GeneticAlgorithm.CrossoverProbability = CrossoverProb;
        GeneticAlgorithm.Termination = new GenerationNumberTermination(AimedGenerations);
        //Assign events
        GeneticAlgorithm.AfterEvaluationStep += Ga_AfterEvaluation;
        GeneticAlgorithm.GenerationRan += Ga_GenerationRan;
        GeneticAlgorithm.TerminationReached += Ga_TerminationReached;

        var handler = FinishIESetup;
        handler?.Invoke(this, EventArgs.Empty);

        if (this.LogMeasurements && GAGenerationLogger == null)
        {
            GAGenerationLogger = new GAGenerationLogger(LogEveryGenerations);
            GAGenerationLogger.BindTo(this);
        }
    }

    private void Ga_AfterEvaluation(object sender, EventArgs e)
    {
        NameAllPhenotypeGameobjects();
    }

    private void Ga_GenerationRan(object sender, EventArgs e)
    {
        //        //Clear all measurements ran before current generations
        //        foreach (var chromosome in this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes)
        //        {
        //            var levelChromosome = chromosome as LevelChromosomeBase;
        //            levelChromosome.Measurements =
        //                new List<StealthLevelEvaluation.MeasureResult>();
        //        }
        //
        NameAllPhenotypeGameobjects();
        Debug.Log($"{GeneticAlgorithm.GenerationsNumber} Generation Ran");
    }

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        List<IChromosome> chromosomes = GetTopLevelsFitness();
        ManifestTopLevels(chromosomes);
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
            var levelChromosome = (OTEPSLevelChromosome)chromosomes[i];
            levelChromosome.PhenotypeGenerator.Generate(levelChromosome, level.gameObject);
            ChromoseMeasurementsVisualizer.AttachDataVisualizer(level.gameObject);
        }
    }
}

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