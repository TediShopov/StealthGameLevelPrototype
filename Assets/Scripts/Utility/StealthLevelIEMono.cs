using GeneticSharp;
using GeneticSharp.Domain;
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public interface IObserver<T>
{
    void Update(T sub);
}

// Declares a subject interface
public interface ISubject<T>
{
    void Attach(IObserver<T> observer);

    void Detach(IObserver<T> observer);

    void Notify(T subject);
}

public interface IPreferenceModel<T> : ISubject<IPreferenceModel<T>>
{
    IList<float> Weights { get; }

    //public void AddObjectToEvalaute(IAestheticMeasurable<T> measurabel);
    //Serves as an update to the model. All measurables change their values
    public void AlterPreferences(
        IAestheticMeasurable<T> selected,
        IEnumerable<IAestheticMeasurable<T>> notSelected);
}

public interface IAestheticMeasurable<T> : IObserver<IPreferenceModel<T>>
{
    float AestheticScore { get; set; }

    PropertyMeasurements GetMeasurements();

    public float UpdateAestheticScore(IPreferenceModel<T> model, PropertyMeasurements properties);
}

public class UserPreferenceModel : IPreferenceModel<LevelChromosomeBase>
{
    private List<IObserver<IPreferenceModel<LevelChromosomeBase>>> Observers;
    private IList<float> _weights;
    public float Step;

    public IList<float> Weights
    {
        get { return _weights; }
        set { _weights = value; }
    }

    public UserPreferenceModel(int preferencesCount)
    {
        this.Weights = GetDefault(preferencesCount);
    }

    public List<float> GetDefault(int measures)
    {
        var equalWeightProperties = new List<float>();
        for (int i = 0; i < measures; i++)
        {
            equalWeightProperties.Add(1.0f);
        }
        Normalize(equalWeightProperties);
        return equalWeightProperties;
    }

    public void AlterPreferences(
        IAestheticMeasurable<LevelChromosomeBase> selected,
        IEnumerable<IAestheticMeasurable<LevelChromosomeBase>> notSelected)
    {
        var avgUnselectedProps =
            PropertyMeasurements.Average(notSelected.Select(x => x.GetMeasurements()).ToList());

        float maxAS = 0;
        maxAS = Mathf.Max(selected.AestheticScore, maxAS);
        foreach (var item in notSelected)
        {
            Mathf.Max(item.AestheticScore, maxAS);
        }
        float prevDistanceToBest =
            MathF.Abs(maxAS - selected.AestheticScore);
        float distanceToBestAestheticScore =
            MathF.Abs(maxAS - selected.AestheticScore);

        const int maxIterations = 100;
        for (int z = 0; z < maxIterations; z++)
        {
            //Apply a single step in all weights
            for (int i = 0; i < avgUnselectedProps.Count; i++)
            {
                var changeInWeight = Step *
                    (selected.GetMeasurements()[i] - avgUnselectedProps[i]);

                this.Weights[i] = this.Weights[i] + changeInWeight;
            }
            //Reeavalute the AESTHETIC SCORE
            //Send message to all observer
            //All aesthetic measurables must update their scores
            Normalize(this.Weights);
            Notify(this);

            maxAS = 0;
            maxAS = Mathf.Max(selected.AestheticScore, maxAS);
            foreach (var item in notSelected)
            {
                Mathf.Max(item.AestheticScore, maxAS);
            }
            //Update distance to best aesthetic score
            prevDistanceToBest = distanceToBestAestheticScore;
            distanceToBestAestheticScore =
                MathF.Abs(maxAS - selected.AestheticScore);

            if (distanceToBestAestheticScore < prevDistanceToBest)
            {
                //Preference model is no longer gaining progress
                break;
            }

            if (Mathf.Approximately(distanceToBestAestheticScore, 0))
            {
                //If selected item has the highest aesthetic score
                break;
            }
        }
        //Normalize(this.Weights);
    }

    public void Normalize(IList<float> weights)
    {
        float sum = weights.Sum();
        for (int i = 0; i < weights.Count; i++)
        {
            weights[i] = weights[i] / sum;
        }
    }

    public void Attach(IObserver<IPreferenceModel<LevelChromosomeBase>> observer)
    {
        if (Observers == null)
            Observers = new List<IObserver<IPreferenceModel<LevelChromosomeBase>>>();
        if (Observers.Contains(observer))
            return;
        Observers.Add(observer);
        observer.Update(this);
    }

    public void Detach(IObserver<IPreferenceModel<LevelChromosomeBase>> observer)
    {
        if (Observers.Contains(observer))
            Observers.Remove(observer);
    }

    public void Notify(IPreferenceModel<LevelChromosomeBase> subject)
    {
        foreach (var item in Observers)
        {
            item.Update(this);
        }
    }
}

//Given all the generation phenotypes and user selection amongst them
//shifts the weights to user selected important properties
//public class DynamicUserPreferenceModel
//{
//    public float Step = 0.2f;
//    public List<List<float>> PreferencesForGeneration;
//
//    public List<float> Current()
//    {
//        return this.PreferencesForGeneration.Last();
//    }
//
//    public DynamicUserPreferenceModel(int measureCount)
//    {
//        PreferencesForGeneration = new List<List<float>>();
//        PreferencesForGeneration.Add(GetDefault(measureCount));
//    }
//
//    private Dictionary<LevelChromosomeBase, float> CurrentAestheticMapping;
//
//    //Returns the max aesthetic score
//    public float UpdateAestheticMeasures(
//        List<float> weights,
//        List<LevelChromosomeBase> selected,
//        List<LevelChromosomeBase> unselected)
//    {
//        if (CurrentAestheticMapping is null)
//        {
//            CurrentAestheticMapping = new Dictionary<LevelChromosomeBase, float>();
//        }
//        float maxAs = 0;
//
//        foreach (var level in selected)
//        {
//            float AS = CalculateAestheticScore(weights, level.AestheticProperties);
//            maxAs = MathF.Max(maxAs, AS);
//            CurrentAestheticMapping[level] = AS;
//        }
//        foreach (var level in unselected)
//        {
//            float AS = CalculateAestheticScore(weights, level.AestheticProperties);
//            maxAs = MathF.Max(maxAs, AS);
//            CurrentAestheticMapping[level] = AS;
//        }
//        return maxAs;
//    }
//
//    public void Alter(List<LevelChromosomeBase> selected, List<LevelChromosomeBase> unselected)
//    {
//        var avgSelectedProps = AverageLevelPreferences(selected);
//        var avgUnselectedProps = AverageLevelPreferences(unselected);
//
//        CurrentAestheticMapping = new Dictionary<LevelChromosomeBase, float>();
//
//        float maxAestheticScore =
//            UpdateAestheticMeasures(this.Current(), selected, unselected);
//
//        List<float> newPreferences = new List<float>(PreferencesForGeneration.Last());
//
//        float prevDistanceToBest =
//            MathF.Abs(maxAestheticScore - CurrentAestheticMapping[selected[0]]);
//        float distanceToBestAestheticScore =
//            MathF.Abs(maxAestheticScore - CurrentAestheticMapping[selected[0]]);
//
//        const int maxIterations = 100;
//        for (int z = 0; z < maxIterations; z++)
//        {
//            //Apply a single step in all weights
//            for (int i = 0; i < avgSelectedProps.Count; i++)
//            {
//                var changeInWeight = Step *
//                    (avgSelectedProps[i] - avgUnselectedProps[i]);
//
//                newPreferences[i] = newPreferences[i] + changeInWeight;
//            }
//            //Reeavalute the AESTHETIC SCORE
//            maxAestheticScore = UpdateAestheticMeasures(newPreferences, selected, unselected);
//
//            //Update distance to best aesthetic score
//
//            prevDistanceToBest = distanceToBestAestheticScore;
//            distanceToBestAestheticScore =
//                MathF.Abs(maxAestheticScore - CurrentAestheticMapping[selected[0]]);
//
//            if (distanceToBestAestheticScore < prevDistanceToBest)
//            {
//                //Preference model is no longer gaining progress
//                break;
//            }
//
//            if (Mathf.Approximately(distanceToBestAestheticScore, 0))
//            {
//                //If selected item has the highest aesthetic score
//                break;
//            }
//        }
//        Normalize(newPreferences);
//        PreferencesForGeneration.Add(newPreferences);
//    }
//
////    public static float CalculateAestheticScore(
////        List<float> weights,
////        PropertyMeasurements aestheticMeasurements)
////    {
////        float aestheticScore = 0;
////        for (int i = 0; i < aestheticMeasurements.Count; i++)
////        {
////            aestheticScore += aestheticMeasurements[i] * weights[i];
////        }
////        return aestheticScore;
////    }
////
////    public float CalculateAestheticScore(
////        PropertyMeasurements aestheticMeasurements)
////    {
////        return CalculateAestheticScore(this.Current(), aestheticMeasurements);
////    }
//
//    public List<float> AverageLevelPreferences(List<LevelChromosomeBase> chromosomes)
//    {
//        var allValidProperties =
//            chromosomes
//            .Select(x => x.AestheticProperties)
//            .ToList();
//        return PropertyMeasurements.Average(allValidProperties);
//    }
//
//    public List<float> GetDefault(int measures)
//    {
//        var equalWeightProperties = new List<float>();
//        for (int i = 0; i < measures; i++)
//        {
//            equalWeightProperties.Add(1.0f);
//        }
//        Normalize(equalWeightProperties);
//        return equalWeightProperties;
//    }
//
//    public float AveragePropertyDistance(int genIndex, int genIndexOther)
//    {
//        return AveragePropertyDistance(
//            PreferencesForGeneration[genIndex],
//            PreferencesForGeneration[genIndexOther]
//            );
//    }
//
//    public static float AveragePropertyDistance(
//        List<float> one,
//        List<float> two
//        )
//
//    {
//        if (one.Count != two.Count) return 0.0f;
//        float avgDitance = 0;
//        for (int i = 0; i < one.Count; i++)
//        {
//            avgDitance += MathF.Abs(one[i] - two[i]);
//        }
//        avgDitance /= (float)one.Count;
//        return avgDitance;
//    }
//
//    public void Normalize(List<float> weights)
//    {
//        float sum = weights.Sum();
//        for (int i = 0; i < weights.Count; i++)
//        {
//            weights[i] = weights[i] / sum;
//        }
//    }
//}

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

    public event EventHandler FinishIESetup;

    public bool IsRunning => GeneticAlgorithm != null
        && GeneticAlgorithm.IsRunning;

    public List<float> PreferencesDefault =>
        new List<float>() { 0.3333f, 0.3333f, 0.3333f };

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
        Dispose();
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

    public void DoGeneration()
    {
        if (GeneticAlgorithm.State == GeneticAlgorithmState.Started)
        {
            ApplyChangesToPreferenceModel();

            InteractiveSelections.Add(GenerationSelecitons);
            GenerationSelecitons.Clear();

            GeneticAlgorithm.EndCurrentGeneration();
            GeneticAlgorithm.EvolveOneGeneration();

            //Evaluates fitness but also manifest the level
            // in the unity scene
            GeneticAlgorithm.EvaluateFitness();
            foreach (var item in GeneticAlgorithm.Population.CurrentGeneration.Chromosomes)
            {
                var level = (LevelChromosomeBase)item;
                UserPreferences.Attach(level);
            }

            for (int i = 0; i < SyntheticGenerations; i++)
            {
                //ApplyChangesToPreferenceModel();

                InteractiveSelections.Add(GenerationSelecitons);
                GenerationSelecitons.Clear();

                GeneticAlgorithm.EndCurrentGeneration();
                GeneticAlgorithm.EvolveOneGeneration();

                //Evaluates fitness but also manifest the level
                // in the unity scene
                GeneticAlgorithm.EvaluateFitness();
                foreach (var item in GeneticAlgorithm.Population.CurrentGeneration.Chromosomes)
                {
                    var level = (LevelChromosomeBase)item;
                    UserPreferences.Attach(level);
                }
            }
        }
        else
        {
            PhenotypeEvaluator.IE = this;
            this.GenerationSelecitons = new List<LevelChromosomeBase>();
            this.InteractiveSelections = new List<List<LevelChromosomeBase>>();
            //            UserPreferences =
            //                new DynamicUserPreferenceModel(PhenotypeEvaluator.GetCountOfLevelProperties());
            UserPreferences =
                new UserPreferenceModel(PhenotypeEvaluator.GetCountOfLevelProperties());
            GeneticAlgorithm.State = GeneticAlgorithmState.Started;
            GeneticAlgorithm.Population.CreateInitialGeneration();
            GeneticAlgorithm.EvaluateFitness();
            foreach (var item in GeneticAlgorithm.Population.CurrentGeneration.Chromosomes)
            {
                var level = (LevelChromosomeBase)item;
                UserPreferences.Attach(level);
            }
        }
    }

    public void RandomizeSeed()
    {
        Seed = new System.Random().Next();
    }

    public void RefreshPreferencesWeight()
    {
        UserPreferences =
            new UserPreferenceModel(PhenotypeEvaluator.GetCountOfLevelProperties());
        //UserPreferences = new DynamicUserPreferenceModel(3);
    }

    public void Run()
    {
        SetupGA();
        GeneticAlgorithm.Population.CreateInitialGeneration();
        GeneticAlgorithm.State = GeneticAlgorithmState.Started;
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
        var selection = new RouletteWheelSelection();
        var crossover = new TwoPointCrossover();
        var mutation = new CustomMutators(1, 1, 1);
        var chromosome = Generator.GetAdamChromosome(RandomSeedGenerator.Next());
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

    private void Ga_GenerationRan(object sender, EventArgs e)
    {
        Debug.Log($"{GeneticAlgorithm.GenerationsNumber} Generation Ran");
    }

    private void Ga_TerminationReached(object sender, EventArgs e)
    {
        List<IChromosome> chromosomes = GetTopLevelsFitness();
        ManifestTopLevels(chromosomes);
        ChekcAvailabilitOfFitnessInfo();
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