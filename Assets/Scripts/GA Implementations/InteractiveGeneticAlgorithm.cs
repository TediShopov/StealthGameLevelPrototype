using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GeneticSharp.Domain
{
    [ExecuteInEditMode]
    public class InteractiveGeneticAlgorithm : MonoBehaviour, IGeneticAlgorithm
    {
        public int IndependentRuns = 5; //Number of runs with synthetic model
        public int AimedGenerations = 10; //Generation until termination
        public int SyntheticGenerations = 5;//Number of generations after user selection
        public int TopNLevels = 5;//Number of levels to keep track

        [SerializeField, Range(0, 1)]
        public float CrossoverProbability;

        [SerializeField, Range(0, 1)]
        public float MutationProbability;

        public LevelProperties LevelProperties;
        public LevelPhenotypeGenerator Generator;
        public InteractiveEvalutorMono PhenotypeEvaluator;

        //Population for both chromosomes and phenotypes
        //Manges lifetime of phenotypes in unity scene
        public PopulationPhenotypeLayout PopulationPhenotypeLayout;

        #region UserSelections

        //Hold the user prefferecd - slected chromosomed
        public List<LevelChromosomeBase> GenerationSelecitons
            = new List<LevelChromosomeBase>();

        //Hold all the user selections chronologically
        public List<List<LevelChromosomeBase>> InteractiveSelections =
            new List<List<LevelChromosomeBase>>();

        //Tracker for user preference changes
        public UserPrefereneceTracker PreferenceTracker;

        #endregion UserSelections

        #region Randomness

        public System.Random RandomSeedGenerator;
        public int Seed;

        #endregion Randomness

        #region Constants

        /// <summary>
        /// The default crossover probability.
        /// </summary>
        public const float DefaultCrossoverProbability = 0.75f;

        /// <summary>
        /// The default mutation probability.
        /// </summary>
        public const float DefaultMutationProbability = 0.1f;

        #endregion Constants

        #region Fields

        private GeneticAlgorithmState m_state;
        private bool m_stopRequested;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.this"/> class.
        /// </summary>
        /// <param name="population">The chromosomes population.</param>
        /// <param name="fitness">The fitness evaluation function.</param>
        /// <param name="selection">The selection operator.</param>
        /// <param name="crossover">The crossover operator.</param>
        /// <param name="mutation">The mutation operator.</param>
        public InteractiveGeneticAlgorithm(
                          IFitness fitness,
                          ISelection selection,
                          ICrossover crossover,
                          IMutation mutation)
        {
            this.CreateFrom(fitness, selection, crossover, mutation);
        }

        public void Awake()
        {
            this.State = GeneticAlgorithmState.NotStarted;
            EndGA();
        }

        public void CreateFrom(
                          IFitness fitness,
                          ISelection selection,
                          ICrossover crossover,
                          IMutation mutation)
        {
            ExceptionHelper.ThrowIfNull("fitness", fitness);
            ExceptionHelper.ThrowIfNull("selection", selection);
            ExceptionHelper.ThrowIfNull("crossover", crossover);
            ExceptionHelper.ThrowIfNull("mutation", mutation);

            Selection = selection;
            Crossover = crossover;
            Mutation = mutation;
            Reinsertion = new ElitistReinsertion();
            Termination = new GenerationNumberTermination(1);

            TimeEvolving = TimeSpan.Zero;
            State = GeneticAlgorithmState.NotStarted;
            TaskExecutor = new LinearTaskExecutor();
            OperatorsStrategy = new DefaultOperatorsStrategy();
        }

        #endregion Constructors

        #region Events

        public event EventHandler AfterEvaluationStep;

        public event EventHandler FinishIESetup;

        public event EventHandler GenerationRan;

        public event EventHandler InteractiveStepReached;

        public event EventHandler Stopped;

        public event EventHandler TerminationReached;

        #endregion Events

        #region AestheticProperties

        public IChromosome BestChromosome
        {
            get
            {
                return Population.BestChromosome;
            }
        }

        public ICrossover Crossover { get; set; }
        public IFitness Fitness => PhenotypeEvaluator;
        public int GenerationsNumber
        {
            get
            {
                return Population.GenerationsNumber;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        public bool IsRunning
        {
            get
            {
                return State == GeneticAlgorithmState.Started || State == GeneticAlgorithmState.Resumed;
            }
        }

        public IMutation Mutation { get; set; }
        public IOperatorsStrategy OperatorsStrategy { get; set; }
        public IPopulation Population => PopulationPhenotypeLayout;
        public int Property
        {
            get => default;
            set
            {
            }
        }

        public IReinsertion Reinsertion { get; set; }
        public ISelection Selection { get; set; }
        /// <summary>
        /// Gets the state.
        /// </summary>
        public GeneticAlgorithmState State
        {
            get
            {
                return m_state;
            }

            set
            {
                var shouldStop = Stopped != null && m_state != value && value == GeneticAlgorithmState.Stopped;

                m_state = value;

                if (shouldStop)
                    Stopped.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the task executor which will be used to execute fitness evaluation.
        /// </summary>
        public ITaskExecutor TaskExecutor { get; set; }

        public ITermination Termination { get; set; }
        /// <summary>
        /// Gets the time evolving.
        /// </summary>
        public TimeSpan TimeEvolving { get; private set; }

        public List<IChromosome> UserSelectedChromose { get; set; }

        #endregion AestheticProperties

        #region Methods

        public void ApplyChangesToPreferenceModel()
        {
            List<LevelChromosomeBase> unselected =
                this.Population.CurrentGeneration.Chromosomes
                .Select(x => (LevelChromosomeBase)x)
                .Where(x => GenerationSelecitons.Contains(x) == false) //Must not be contained by selections
                .ToList();

            if (GenerationSelecitons.Count == 0) return;

            PhenotypeEvaluator
                .UserPreferenceModel
                .AlterPreferences(GenerationSelecitons[0], unselected);
            //UserPreferences.Alter(GenerationSelecitons, unselected);
        }
        public void DisposeOldPopulation()
        {
            var tempList = this.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
        }
        public void DoGeneration()
        {
            if (this.State == GeneticAlgorithmState.Started)
            {
                ApplyChangesToPreferenceModel();
                for (int i = 0; i < SyntheticGenerations; i++)
                {
                    InteractiveSelections.Add(GenerationSelecitons);
                    GenerationSelecitons.Clear();

                    this.EvolveOneGeneration();

                    //Evaluates fitness but also manifest the level
                    // in the unity scene
                    this.EvaluateFitness();

                    this.EndCurrentGeneration();
                }
            }
        }
        /// <summary>
        /// Ends the current generation.
        /// </summary>
        /// <returns><c>true</c>, if current generation was ended, <c>false</c> otherwise.</returns>
        public virtual bool EndCurrentGeneration()
        {
            //EvaluateFitness();
            Population.EndCurrentGeneration();

            var handler = GenerationRan;
            handler?.Invoke(this, EventArgs.Empty);

            if (Termination.HasReached(this))
            {
                State = GeneticAlgorithmState.TerminationReached;

                handler = TerminationReached;
                handler?.Invoke(this, EventArgs.Empty);

                return true;
            }

            if (m_stopRequested)
            {
                TaskExecutor.Stop();
                State = GeneticAlgorithmState.Stopped;
            }

            return false;
        }
        public void EndGA()
        {
            GenerationSelecitons = new List<LevelChromosomeBase>();
            InteractiveSelections = new List<List<LevelChromosomeBase>>();
            DisposeOldPopulation();
            //RefreshPreferencesWeight();
            this.State = GeneticAlgorithmState.NotStarted;
        }
        public void EvaluateFitness(Population pop)
        {
            try
            {
                var allChromomsome = pop.CurrentGeneration.Chromosomes;
                var groupedChromosomes = pop.CurrentGeneration.Chromosomes
                    .GroupBy(x => x);

                //var chromosomesWithoutFitness = Population.CurrentGeneration.Chromosomes.Where(c => !c.Fitness.HasValue).ToList();

                foreach (var groups in groupedChromosomes)
                {
                    var groupLeader = (LevelChromosomeBase)
                        groups.First(x => ((LevelChromosomeBase)x).Manifestation);
                    TaskExecutor.Add(() =>
                    {
                        //RunEvaluateFitness(c);
                        if (groupLeader.Manifestation is null &&
                        groupLeader.Fitness.HasValue)
                        {
                            UnityEngine.Debug.Log(
                                $"Replicated: {groupLeader.Manifestation.name}");
                        }
                        if (groupLeader.Fitness.HasValue)
                        {
                            ReevaluateGroup(groups);
                        }
                        else
                        {
                            RunEvaluateGroupOfIndiviudal(groups);
                        }
                    });
                }

                if (!TaskExecutor.Start())
                {
                    throw new TimeoutException("The fitness evaluation reached the {0} timeout.".With(TaskExecutor.Timeout));
                }
            }
            finally
            {
                TaskExecutor.Stop();
                TaskExecutor.Clear();
            }
            pop.CurrentGeneration.Chromosomes =
                Population.CurrentGeneration.Chromosomes.OrderByDescending(c => c.Fitness.Value).ToList();
            //GeneratePhenotypeForAll();
        }
        public virtual void EvaluateFitness()
        {
            EvaluateFitness(PopulationPhenotypeLayout);
            ReorderTransformHierarchy(PopulationPhenotypeLayout);
            var handle = AfterEvaluationStep;
            handle?.Invoke(this, EventArgs.Empty);
        }
        public virtual void EvolveOneGeneration()
        {
            var parents = SelectParents();
            var offspring = Cross(parents);
            Mutate(offspring);
            var newGenerationChromosomes = Reinsert(offspring, parents);
            Population.CreateNewGeneration(newGenerationChromosomes);
            //return EndCurrentGeneration();
        }
        public void NameAllPhenotypeGameobjects()
        {
            Vector2 placement = new Vector2(5, 5);

            foreach (var chromosome in Population.CurrentGeneration.Chromosomes)
            {
                var levelChromosome = chromosome as LevelChromosomeBase;

                if (levelChromosome.Manifestation != null)
                {
                    if (levelChromosome.Manifestation.GetComponentInChildren<ChromoseMeasurementsVisualizer>()
                        == null)
                    {
                        //Attach mono behaviour to visualize the measurements
                        ChromoseMeasurementsVisualizer.AttachDataVisualizer(
                            levelChromosome.Manifestation,
                            new Vector2(5, 5));
                    }

                    //Clear objects name and replace it with new fitnessj
                    this.Generator.ClearName(levelChromosome);
                    this.Generator.AppendFitnessToName(levelChromosome);
                }
            }
        }
        public void RandomizeSeed()
        {
            Seed = new System.Random().Next();
        }
        public void ReorderTransformHierarchy(Population population)

        {
            int index = 0;

            //On th assumption that fitness is assigned on all group members
            var groupLeadersOrder = population.CurrentGeneration.Chromosomes.GroupBy(x => x)
                .OrderByDescending(x => x.First().Fitness).ToList();

            foreach (var group in groupLeadersOrder)
            {
                var groupLeader = (LevelChromosomeBase)
                    group.First(x => ((LevelChromosomeBase)x).Manifestation);
                if (groupLeader is LevelChromosomeBase)
                {
                    var levelChromosome = (LevelChromosomeBase)groupLeader;
                    levelChromosome.Manifestation.transform.SetAsLastSibling();
                    UnityEngine.Debug.Log($"Level group Fitness: {groupLeader.Fitness} placed at {index}");
                    levelChromosome.Manifestation.transform.SetSiblingIndex(index);
                    index++;
                }
            }
        }
        public void RunWithSyntheticModel()
        {
            for (int i = 0; i < IndependentRuns; i++)
            {
                StartGA();
                for (int j = 0; j < AimedGenerations; j++)
                {
                    this.EvolveOneGeneration();
                    //Evaluates fitness but also manifest the level
                    // in the unity scene
                    this.EvaluateFitness();
                    this.EndCurrentGeneration();
                }

                //Do not remove ga result in last run
                if (i != IndependentRuns - 1)
                    EndGA();
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
        public virtual void SetupGA()
        {
            RandomSeedGenerator = new System.Random(Seed);
            var selection = new TournamentSelection(3);
            var crossover = new TwoPointCrossover();
            var mutation = new OTEPSVariableLenghtMutator(1, 1, 1);
            var chromosome = Generator.GetAdamChromosome(RandomSeedGenerator.Next());
            PopulationPhenotypeLayout =
                new PopulationPhenotypeLayout(PopulationPhenotypeLayout, this.gameObject, chromosome);

            this.CreateFrom(
                PhenotypeEvaluator,
                selection,
                crossover,
                mutation);

            this.Termination =
                new GenerationNumberTermination(AimedGenerations);

            //Assign events
            this.AfterEvaluationStep += Ga_AfterEvaluation;
            this.GenerationRan -= Ga_GenerationRan;
            this.GenerationRan += Ga_GenerationRan;
            this.TerminationReached -= Ga_TerminationReached;
            this.TerminationReached += Ga_TerminationReached;

            var handler = FinishIESetup;
            handler?.Invoke(this, EventArgs.Empty);
        }
        public void StartGA()
        {
            DisposeOldPopulation();
            SetupGA();

            //Normalize the user preferences
            PhenotypeEvaluator.Prepare();

            //Attach tracker that keeps track of the weight of the user preference model
            //and their changes throughout the generations
            AttachUserPreferenceLogger();

            //Seed the randomizer used in mutators and
            GeneticSharp.RandomizationProvider.Current =
                new NativeRandomProvider(Seed);

            //Clear selections
            GenerationSelecitons =
                new List<LevelChromosomeBase>();
            InteractiveSelections =
                new List<List<LevelChromosomeBase>>();

            //Startup the interactive evolutionary algoritm
            State = GeneticAlgorithmState.Started;
            Population.CreateInitialGeneration();
            EvaluateFitness();
        }
        private void AttachUserPreferenceLogger()
        {
            PreferenceTracker = new UserPrefereneceTracker(this);
            PhenotypeEvaluator.UserPreferenceModel.
                Attach(PreferenceTracker);
        }
        private IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            return OperatorsStrategy.Cross(Population, Crossover, CrossoverProbability, parents);
        }
        private void Ga_AfterEvaluation(object sender, EventArgs e)
        {
            UnityEngine.Debug.Log($"_DEB_ AfterEval");
            NameAllPhenotypeGameobjects();
        }
        private void Ga_GenerationRan(object sender, EventArgs e)
        {
            NameAllPhenotypeGameobjects();

            UnityEngine.Debug.Log($"_DEB_ GenRan");
            UnityEngine.Debug.Log($"{this.GenerationsNumber} Generation Ran");
        }
        private void Ga_TerminationReached(object sender, EventArgs e)
        //Given an level phenotype generator, population count and level size
        // spreads levels manifestations in a grid. Used by all phenotype evalutions
        // to trigger the level generations when needed
        {
            UnityEngine.Debug.Log($"_DEB_ Termination");
            List<IChromosome> chromosomes = GetTopLevelsFitness();
            ManifestTopLevels(chromosomes);
        }
        private List<IChromosome> GetTopLevelsFitness()
        {
            List<IChromosome> testC = this.Population.Generations.SelectMany(x => x.Chromosomes).ToList();

            List<IChromosome> topN = this.Population.Generations.SelectMany(x => x.Chromosomes)
                .
                OrderByDescending(x => x.Fitness).Take(TopNLevels).ToList();
            int n = 0;
            foreach (var top in topN)
            {
                n++;
                UnityEngine.Debug.Log($"Top {n} - Fitness {top.Fitness}");
            }
            return topN;
        }
        private GameObject[] TopLevelManifestations = new GameObject[1];
        private void ManifestTopLevels(List<IChromosome> chromosomes)
        {
            //Start with y down
            Vector3 TopLevelsPos = this.transform.position - new Vector3(0, 30, 0);

            foreach (var top in TopLevelManifestations)
            {
                DestroyImmediate(top);
            }

            TopLevelManifestations = new GameObject[chromosomes.Count];
            for (int i = 0; i < chromosomes.Count; i++)
            {
                TopLevelsPos += new Vector3(25, 0, 0);
                var levelChromosome =
                    (OTEPSLevelChromosome)chromosomes[i];
                var manifestation =
                    new GameObject($"Top {i} - {chromosomes[i].Fitness}");
                manifestation.transform.position = TopLevelsPos;
                manifestation.transform.SetParent(this.transform, false);
                levelChromosome.Manifestation = manifestation;
                levelChromosome.
                    PhenotypeGenerator
                    .Generate(levelChromosome, levelChromosome.Manifestation);

                TopLevelManifestations[i] = levelChromosome.Manifestation;
            }
            Physics2D.SyncTransforms();
        }
        private void Mutate(IList<IChromosome> chromosomes)
        {
            OperatorsStrategy.Mutate(Mutation, MutationProbability, chromosomes);
        }

        /// Runs the evaluate fitness.
        private void RunEvaluateGroupOfIndiviudal(IEnumerable<IChromosome> identicalChromosomes)
        {
            var groupLeader = identicalChromosomes.First(x => ((LevelChromosomeBase)x).Manifestation);
            try
            {
                double fitness = Fitness.Evaluate(groupLeader);
                foreach (var groupsMembers in identicalChromosomes)
                {
                    groupsMembers.Fitness = fitness;
                }
            }
            catch (Exception ex)
            {
                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
            }
        }
        /// Runs the evaluate fitness.
        private void ReevaluateGroup(IEnumerable<IChromosome> identicalChromosomes)
        {
            try
            {
                var groupLeader = identicalChromosomes.First(x => ((LevelChromosomeBase)x).Manifestation);
                groupLeader.Fitness = PhenotypeEvaluator.Reevaluate(groupLeader);
                foreach (var groupsMembers in identicalChromosomes)
                {
                    groupsMembers.Fitness = groupLeader.Fitness;
                }
            }
            catch (Exception ex)
            {
                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
            }
        }
        private IList<IChromosome> Reinsert(IList<IChromosome> offspring, IList<IChromosome> parents)
        {
            return Reinsertion.SelectChromosomes(Population, offspring, parents);
        }
        private IList<IChromosome> SelectParents()
        {
            return Selection.SelectChromosomes(Population.MinSize, Population.CurrentGeneration);
        }

        #endregion Methods
    }
}