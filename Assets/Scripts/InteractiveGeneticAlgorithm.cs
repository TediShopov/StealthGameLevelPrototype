using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GeneticSharp.Domain
{
    public sealed class InteractiveGeneticAlgorithm : IGeneticAlgorithm
    {
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

        private bool m_stopRequested;
        private readonly object m_lock = new object();
        private GeneticAlgorithmState m_state;
        private readonly Stopwatch m_stopwatch = new Stopwatch();

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.GeneticAlgorithm"/> class.
        /// </summary>
        /// <param name="population">The chromosomes population.</param>
        /// <param name="fitness">The fitness evaluation function.</param>
        /// <param name="selection">The selection operator.</param>
        /// <param name="crossover">The crossover operator.</param>
        /// <param name="mutation">The mutation operator.</param>
        public InteractiveGeneticAlgorithm(
                          IPopulation population,
                          IFitness fitness,
                          ISelection selection,
                          ICrossover crossover,
                          IMutation mutation)
        {
            ExceptionHelper.ThrowIfNull("population", population);
            ExceptionHelper.ThrowIfNull("fitness", fitness);
            ExceptionHelper.ThrowIfNull("selection", selection);
            ExceptionHelper.ThrowIfNull("crossover", crossover);
            ExceptionHelper.ThrowIfNull("mutation", mutation);

            Population = population;
            Fitness = fitness;
            Selection = selection;
            Crossover = crossover;
            Mutation = mutation;
            Reinsertion = new ElitistReinsertion();
            Termination = new GenerationNumberTermination(1);

            CrossoverProbability = DefaultCrossoverProbability;
            MutationProbability = DefaultMutationProbability;
            TimeEvolving = TimeSpan.Zero;
            State = GeneticAlgorithmState.NotStarted;
            TaskExecutor = new LinearTaskExecutor();
            OperatorsStrategy = new DefaultOperatorsStrategy();
        }

        #endregion Constructors

        #region Events

        public event EventHandler InteractiveStepReached;

        /// <summary>
        /// Occurs when generation ran.
        /// </summary>
        public event EventHandler GenerationRan;

        /// <summary>
        /// Occurs when termination reached.
        /// </summary>
        public event EventHandler TerminationReached;

        /// <summary>
        /// Occurs when evaluation of all objects has been performed.
        /// </summary>
        public event EventHandler AfterEvaluationStep;

        /// <summary>
        /// Occurs when stopped.
        /// </summary>
        public event EventHandler Stopped;

        #endregion Events

        #region AestheticProperties

        /// <summary>
        /// </summary>
        public List<IChromosome> UserSelectedChromose { get; set; }

        /// <summary>
        /// Gets the operators strategy
        /// </summary>
        public IOperatorsStrategy OperatorsStrategy { get; set; }

        /// <summary>
        /// Gets the population.
        /// </summary>
        /// <value>The population.</value>
        public IPopulation Population { get; private set; }

        /// <summary>
        /// Gets the fitness function.
        /// </summary>
        public IFitness Fitness { get; private set; }

        /// <summary>
        /// Gets or sets the selection operator.
        /// </summary>
        public ISelection Selection { get; set; }

        /// <summary>
        /// Gets or sets the crossover operator.
        /// </summary>
        /// <value>The crossover.</value>
        public ICrossover Crossover { get; set; }

        /// <summary>
        /// Gets or sets the crossover probability.
        /// </summary>
        public float CrossoverProbability { get; set; }

        /// <summary>
        /// Gets or sets the mutation operator.
        /// </summary>
        public IMutation Mutation { get; set; }

        /// <summary>
        /// Gets or sets the mutation probability.
        /// </summary>
        public float MutationProbability { get; set; }

        /// <summary>
        /// Gets or sets the reinsertion operator.
        /// </summary>
        public IReinsertion Reinsertion { get; set; }

        /// <summary>
        /// Gets or sets the termination condition.
        /// </summary>
        public ITermination Termination { get; set; }

        /// <summary>
        /// Gets the generations number.
        /// </summary>
        /// <value>The generations number.</value>
        public int GenerationsNumber
        {
            get
            {
                return Population.GenerationsNumber;
            }
        }

        /// <summary>
        /// Gets the best chromosome.
        /// </summary>
        /// <value>The best chromosome.</value>
        public IChromosome BestChromosome
        {
            get
            {
                return Population.BestChromosome;
            }
        }

        /// <summary>
        /// Gets the time evolving.
        /// </summary>
        public TimeSpan TimeEvolving { get; private set; }

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

        /// <summary>
        /// Gets or sets the task executor which will be used to execute fitness evaluation.
        /// </summary>
        public ITaskExecutor TaskExecutor { get; set; }

        public int Property
        {
            get => default;
            set
            {
            }
        }

        #endregion AestheticProperties

        #region Methods

        public void EvolveOneGeneration()
        {
            var parents = SelectParents();
            var offspring = Cross(parents);
            Mutate(offspring);
            var newGenerationChromosomes = Reinsert(offspring, parents);
            Population.CreateNewGeneration(newGenerationChromosomes);
            //return EndCurrentGeneration();
        }

        /// <summary>
        /// Ends the current generation.
        /// </summary>
        /// <returns><c>true</c>, if current generation was ended, <c>false</c> otherwise.</returns>
        public bool EndCurrentGeneration()
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

        public void IntectiveEvalutionStep()
        {
        }

        //
        //        //Manifest or generates all the chromoses in this generation in the unity scene
        //        public void GeneratePhenotypeForAll()
        //        {
        //            foreach (var chromo in Population.CurrentGeneration.Chromosomes)
        //            {
        //                if (chromo is LevelChromosomeBase)
        //                {
        //                    var unityManifestableChromo = chromo as LevelChromosomeBase;
        //                    unityManifestableChromo.
        //                        PhenotypeGenerator.Generate(unityManifestableChromo, unityManifestableChromo.Manifestation);
        //                }
        //            }
        //        }

        public void ReorderTransformHierarchy()
        {
            int index = 0;

            //On th assumption that fitness is assigned on all group members
            var groupLeadersOrder = Population.CurrentGeneration.Chromosomes.GroupBy(x => x)
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

            //            for (int i = 0; i < Population.CurrentGeneration.Chromosomes.Count; i++)
            //            {
            //                var chromo = Population.CurrentGeneration.Chromosomes[i];
            //                if (chromo is LevelChromosomeBase)
            //                {
            //                    var levelChromosome = (LevelChromosomeBase)chromo;
            //                    levelChromosome.Manifestation.transform.SetSiblingIndex(i);
            //                }
            //            }
        }

        /// <summary>
        /// Evaluates the fitness.
        /// </summary>
        public void EvaluateFitness()
        {
            try
            {
                //GeneratePhenotypeForAll();

                var allChromomsome = Population.CurrentGeneration.Chromosomes;
                var groupedChromosomes = Population.CurrentGeneration.Chromosomes
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

                //                for (int i = 0; i < allChromomsome.Count; i++)
                //                {
                //                    var c = allChromomsome[i];
                //
                //                    TaskExecutor.Add(() =>
                //                    {
                //                        //RunEvaluateFitness(c);
                //                        if (c.Fitness.HasValue)
                //                        {
                //                            InteractiveEvalutorMono f = (InteractiveEvalutorMono)Fitness;
                //                            c.Fitness = f.Reevaluate(c);
                //                        }
                //                        else
                //                        {
                //                            RunEvaluateGroupOfIndiviudal();
                //                        }
                //                    });
                //                }

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

            Population.CurrentGeneration.Chromosomes
               = Population.CurrentGeneration.Chromosomes.OrderByDescending(c => c.Fitness.Value).ToList();
            ReorderTransformHierarchy();
            var handle = AfterEvaluationStep;
            handle?.Invoke(this, EventArgs.Empty);
        }

        //        /// Runs the evaluate fitness.
        //        private void RunEvaluateFitness(object chromosome)
        //        {
        //            var c = chromosome as IChromosome;
        //
        //            try
        //            {
        //                c.Fitness = Fitness.Evaluate(c);
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
        //            }
        //        }

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
                InteractiveEvalutorMono f = (InteractiveEvalutorMono)Fitness;
                groupLeader.Fitness = f.Reevaluate(groupLeader);
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

        //        private void RunChangeAestheticScore(object chromosome)
        //        {
        //            var c = chromosome as IChromosome;
        //            try
        //            {
        //                c.Fitness = Fitness.Evaluate(c);
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
        //            }
        //        }

        /// <summary>
        /// Selects the parents.
        /// </summary>
        /// <returns>The parents.</returns>
        private IList<IChromosome> SelectParents()
        {
            return Selection.SelectChromosomes(Population.MinSize, Population.CurrentGeneration);
        }

        /// <summary>
        /// Crosses the specified parents.
        /// </summary>
        /// <param name="parents">The parents.</param>
        /// <returns>The result chromosomes.</returns>
        private IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            return OperatorsStrategy.Cross(Population, Crossover, CrossoverProbability, parents);
        }

        /// <summary>
        /// Mutate the specified chromosomes.
        /// </summary>
        /// <param name="chromosomes">The chromosomes.</param>
        private void Mutate(IList<IChromosome> chromosomes)
        {
            OperatorsStrategy.Mutate(Mutation, MutationProbability, chromosomes);
        }

        /// <summary>
        /// Reinsert the specified offspring and parents.
        /// </summary>
        /// <param name="offspring">The offspring chromosomes.</param>
        /// <param name="parents">The parents chromosomes.</param>
        /// <returns>
        /// The reinserted chromosomes.
        /// </returns>
        private IList<IChromosome> Reinsert(IList<IChromosome> offspring, IList<IChromosome> parents)
        {
            return Reinsertion.SelectChromosomes(Population, offspring, parents);
        }

        #endregion Methods
    }
}