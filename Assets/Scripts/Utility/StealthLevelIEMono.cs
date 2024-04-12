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
public class GridObjectLayout
{
    public GridObjectLayout(LevelProperties levelProperties)
    {
        this.LevelProperties = levelProperties;
    }

    private LevelProperties LevelProperties;
    private GameObject[,] LevelObjects;

    private int currentIndex = -1;

    public struct SelectionDetails
    {
        private int Generation;
        private LevelChromosome Chromosome;
    }

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

        float step = 0.1f;
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
            // GameObject.DestroyImmediate(item.GetComponent<ContinuosFutureLevel>());
            // GameObject.DestroyImmediate(item.GetComponent<FloodfilledRoadmapGenerator>());
            // GameObject.DestroyImmediate(item.GetComponent<Grid>());
            //            foreach (var comp in item.GetComponents<Component>())
            //            {
            //                if (!(comp is Transform))
            //                {
            //                    GameObject.DestroyImmediate(comp);
            //                }
            //            }
            var tempList = item.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
    }
}

public class ChromosomeSelection
{
    public int GenerationNumber { get; }
    public LevelChromosomeBase LevelChromosome { get; }
    public float Step { get; }
    public List<float> NewPreferences = new List<float>();
    public List<float> OldPrefenreces = new List<float>();

    public ChromosomeSelection(
        LevelChromosomeBase levelChromosome,
        List<float> oldPref,
        int gen, float step)
    {
        this.LevelChromosome = levelChromosome;
        NewPreferences = new List<float>(oldPref);
        OldPrefenreces = new List<float>(oldPref);
        this.GenerationNumber = gen;
        this.Step = step;
    }

    public List<float> ChangePreferenceModel(List<float> avgLevelProperties)
    {
        //Change the wieght preference of the evaluator

        List<float> measures = LevelChromosome.AestheticProperties;
        for (int i = 0; i < measures.Count; i++)
        {
            var changeInWeight = Step *
                (measures[i] - avgLevelProperties[i]);
            NewPreferences[i] = OldPrefenreces[i] + changeInWeight;
        }
        return NewPreferences;
    }

    public override bool Equals(object obj)
    {
        if (obj is ChromosomeSelection)

        {
            var other = (ChromosomeSelection)obj;
            return this.LevelChromosome.Equals(other.LevelChromosome)
                && this.GenerationNumber.Equals(other.GenerationNumber);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
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

    public InteractiveEvalutorMono PhenotypeEvaluator;
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
    public float Step;

    public InteractiveGeneticAlgorithm GeneticAlgorithm;
    public Vector2 ExtraSpacing;

    public List<float> UserPreferences;
    public List<ChromosomeSelection> InteractiveSelections;

    public void RefreshPreferencesWeight()
    {
        UserPreferences = PreferencesDefault;
    }

    public List<float> PreferencesDefault =>
        new List<float>() { 0.3333f, 0.3333f, 0.3333f };

    public void Awake()
    {
        InteractiveSelections = new List<ChromosomeSelection>();
        Dispose();
        //        if (LogExecutions)
        //        {
        //            string algoName = $"GEN_{AimedGenerations}_POP{PopulationCount}_SZ{LevelProperties.LevelSize}";
        //            float[] runs = Helpers.TrackExecutionTime(Run, LogExecutionTimes);
        //            Helpers.SaveRunToCsv($"Tests/{algoName}.txt", runs);
        //        }
        //        else
        //        {
        //            Run();
        //        }
    }

    public bool IsRunning => GeneticAlgorithm != null
        && GeneticAlgorithm.IsRunning;

    public void RandomizeSeed()
    {
        Seed = new System.Random().Next();
    }

    public void SelectChromosome(LevelChromosome chromosome)
    {
        List<float> avgLevelProperties = null;
        try
        {
            avgLevelProperties = AverageLevelPreferences();
            if (GeneticAlgorithm.IsRunning)
            {
                int previousSelectionIndex =
                    InteractiveSelections.FindIndex(0, x => x.LevelChromosome.Equals(chromosome));
                if (previousSelectionIndex != -1)
                {
                    ChromosomeSelection previousSelection = InteractiveSelections[previousSelectionIndex];
                    UserPreferences = previousSelection.OldPrefenreces;
                    //Deselect
                    List<ChromosomeSelection> afterSelection =
                        InteractiveSelections.Skip(previousSelectionIndex + 1)
                        .ToList();

                    InteractiveSelections = InteractiveSelections.GetRange(0, previousSelectionIndex);

                    foreach (var item in afterSelection)
                    {
                        InteractiveSelections.Add(item);
                        UserPreferences = item.ChangePreferenceModel(avgLevelProperties);
                    }
                    Debug.Log($"Reaplied user selections count: {afterSelection.Count}");
                }
                else
                {
                    //Select and update user preferences
                    ChromosomeSelection newSelection = new ChromosomeSelection(
                        chromosome,
                        UserPreferences,
                        GeneticAlgorithm.GenerationsNumber,
                        0.2f);
                    UserPreferences = newSelection.ChangePreferenceModel(avgLevelProperties);
                    InteractiveSelections.Add(newSelection);
                }
            }
        }
        catch (Exception)
        {
            int a = 3;
            throw;
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

    public List<float> AverageLevelPreferences()
    {
        var allValidProperties =
        this.GeneticAlgorithm.Population.CurrentGeneration.Chromosomes
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
            PhenotypeEvaluator.IE = this;
            RefreshPreferencesWeight();
            this.InteractiveSelections = new List<ChromosomeSelection>();
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