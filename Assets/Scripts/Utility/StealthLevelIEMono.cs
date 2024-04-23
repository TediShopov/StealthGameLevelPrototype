using CGALDotNet;
using Codice.Client.Common;
using GeneticSharp;
using GeneticSharp.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.TextCore.Text;
using UnityEditor.UIElements;
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

public class StealthLevelIEMono : MonoBehaviour
{
    //public int Rows = 5; // Number of rows in the grid
    //public int Columns = 5; // Number of columns in the grid

    //public int PopulationCount;
    public int AimedGenerations = 10;

    [Range(0, 1)]
    public float MutationProb = 10;

    [Range(0, 1)]
    public float CrossoverProb = 10;

    public InteractiveEvalutorMono PhenotypeEvaluator;
    public LevelPhenotypeGenerator Generator;
    public LevelProperties LevelProperties;
    public PopulationPhenotypeLayout PopulationPhenotypeLayout;
    //private GridObjectLayout GridPopulation;

    [Header("Seed")]
    public int Seed;

    public System.Random RandomSeedGenerator;

    [Header("Logging")]
    public bool LogMeasurements = false;

    public int LogEveryGenerations = 5;
    public int IndependentRuns = 5;
    public GAGenerationLogger GAGenerationLogger;

    //public bool LogIndividualExecutions = false;
    //public int LogExecutionTimes = 0;
    public int TopNLevels = 5;

    public float Step;

    public InteractiveGeneticAlgorithm GeneticAlgorithm;
    public Vector2 ExtraSpacing;

    [HideInInspector] public List<Tuple<int, List<float>>> UserPreferencesOverGenerations;
    [SerializeField] public List<float> UserPreferences;
    public List<LevelChromosomeBase> GenerationSelecitons;
    public List<List<LevelChromosomeBase>> InteractiveSelections;

    public event EventHandler FinishIESetup;

    public void RefreshPreferencesWeight()
    {
        UserPreferences = PreferencesDefault;
    }

    public void NormalizeUserPreferences()
    {
        float sum = UserPreferences.Sum();
        for (int i = 0; i < UserPreferences.Count; i++)
        {
            UserPreferences[i] = UserPreferences[i] / sum;
        }
    }

    public List<float> PreferencesDefault =>
        new List<float>() { 0.3333f, 0.3333f, 0.3333f };

    public void Awake()
    {
        InteractiveSelections = new List<List<LevelChromosomeBase>>();
        Dispose();
    }

    public bool IsRunning => GeneticAlgorithm != null
        && GeneticAlgorithm.IsRunning;

    public void RandomizeSeed()
    {
        Seed = new System.Random().Next();
    }

    public void RunWithSyntheticModel()
    {
        for (int i = 0; i < IndependentRuns; i++)
        {
            Dispose();
            PhenotypeEvaluator.IE = this;
            this.GenerationSelecitons = new List<LevelChromosomeBase>();
            this.InteractiveSelections = new List<List<LevelChromosomeBase>>();
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

    public void ApplyChangesToPreferenceModel()
    {
        if (GenerationSelecitons.Count == 0) return;
        var avgGenerationProps = AverageLevelPreferences(
            this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes.Select(x => (LevelChromosomeBase)x).ToList());

        var avgSelectionProps = AverageLevelPreferences(GenerationSelecitons);
        this.UserPreferencesOverGenerations.Add(
            new Tuple<int, List<float>>(
                this.GeneticAlgorithm.Population.CurrentGeneration.Number,
                new List<float>(UserPreferences)));

        for (int i = 0; i < avgSelectionProps.Count; i++)
        {
            var changeInWeight = Step *
                (avgSelectionProps[i] - avgGenerationProps[i]);

            UserPreferences[i] = UserPreferences[i] + changeInWeight;
            UserPreferences[i] = Mathf.Clamp01(UserPreferences[i]);
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

    private void ChekcAvailabilitOfFitnessInfo()
    {
        List<IChromosome> chromosomes = this.GeneticAlgorithm.Population.Generations
            .SelectMany(x => x.Chromosomes).ToList();
        //Start with y down
        Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);
        for (int i = 0; i < chromosomes.Count; i++)
        {
            var levelChromosome = (OTEPSLevelChromosome)chromosomes[i];
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
        GAGenerationLogger = null;
        RandomSeedGenerator = new System.Random(Seed);
        var selection = new RouletteWheelSelection();
        var crossover = new TwoPointCrossover();
        var mutation = new CustomMutators(1, 1, 1);
        //var chromosome = new FloatingPointChromosome(0,1,35,8);
        var chromosome = Generator.GetAdamChromosome(RandomSeedGenerator.Next());
        //var population = new Population(PopulationCount, PopulationCount, chromosome);
        var population = new PopulationPhenotypeLayout(PopulationPhenotypeLayout, this.gameObject, chromosome);

        GeneticAlgorithm = new
            InteractiveGeneticAlgorithm(population, PhenotypeEvaluator, selection, crossover, mutation);
        GeneticAlgorithm.MutationProbability = MutationProb;
        GeneticAlgorithm.CrossoverProbability = CrossoverProb;
        GeneticAlgorithm.Termination = new GenerationNumberTermination(AimedGenerations);
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

    public List<float> AverageLevelPreferences(List<LevelChromosomeBase> chromosomes)
    {
        var allValidProperties =
            chromosomes
            .Select(x => ((LevelChromosomeBase)x).AestheticProperties)
            .ToList();

        List<float> avgProperties = new List<float>(allValidProperties.First());

        try
        {
            for (int i = 0; i < avgProperties.Count; i++)
            {
                avgProperties[i] = allValidProperties.Average(x => x[i]);
            }
        }
        catch (Exception)
        {
            throw;
        }
        return avgProperties;
    }

    public void DoGeneration()
    {
        if (GeneticAlgorithm.State == GeneticAlgorithmState.Started)
        {
            //If interaction has occurred

            NormalizeUserPreferences();
            ApplyChangesToPreferenceModel();

            InteractiveSelections.Add(GenerationSelecitons);
            GenerationSelecitons.Clear();

            GeneticAlgorithm.EndCurrentGeneration();
            GeneticAlgorithm.EvolveOneGeneration();

            //Evaluates fitness but also manifest the level
            // in the unity scene
            GeneticAlgorithm.EvaluateFitness();
        }
        else
        {
            PhenotypeEvaluator.IE = this;
            this.GenerationSelecitons = new List<LevelChromosomeBase>();
            this.InteractiveSelections = new List<List<LevelChromosomeBase>>();
            this.UserPreferencesOverGenerations = new List<Tuple<int, List<float>>>();
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
        //this.GridPopulation = null;
        this.GeneticAlgorithm = null;
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

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        List<IChromosome> chromosomes = GetTopLevelsFitness();
        ManifestTopLevels(chromosomes);
        ChekcAvailabilitOfFitnessInfo();
    }

    private void Ga_GenerationRan(object sender, EventArgs e)
    {
        Debug.Log($"{GeneticAlgorithm.GenerationsNumber} Generation Ran");
    }
}