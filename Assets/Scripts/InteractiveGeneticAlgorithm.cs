using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GeneticSharp.Domain
{
    public sealed class InteractiveGeneticAlgorithm : IGeneticAlgorithm
    {
        public const float DefaultCrossoverProbability = 0.75f;

        public const float DefaultMutationProbability = 0.1f;

        private bool m_stopRequested;

        private readonly object m_lock = new object();

        private GeneticAlgorithmState m_state;

        private Stopwatch m_stopwatch;

        public IOperatorsStrategy OperatorsStrategy { get; set; }

        public IPopulation Population { get; private set; }

        public IFitness Fitness { get; private set; }

        public ISelection Selection { get; set; }

        public ICrossover Crossover { get; set; }

        public float CrossoverProbability { get; set; }

        public IMutation Mutation { get; set; }

        public float MutationProbability { get; set; }

        public IReinsertion Reinsertion { get; set; }

        public ITermination Termination { get; set; }

        public int GenerationsNumber => Population.GenerationsNumber;

        public IChromosome BestChromosome => Population.BestChromosome;

        public TimeSpan TimeEvolving { get; private set; }

        public GeneticAlgorithmState State
        {
            get
            {
                return m_state;
            }
            private set
            {
                bool num = this.Stopped != null && m_state != value && value == GeneticAlgorithmState.Stopped;
                m_state = value;
                if (num)
                {
                    this.Stopped?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                if (State != GeneticAlgorithmState.Started)
                {
                    return State == GeneticAlgorithmState.Resumed;
                }

                return true;
            }
        }

        public ITaskExecutor TaskExecutor { get; set; }

        public event EventHandler GenerationRan;

        public event EventHandler TerminationReached;

        public event EventHandler Stopped;

        public InteractiveGeneticAlgorithm(IPopulation population,
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
            CrossoverProbability = 0.75f;
            MutationProbability = 0.1f;
            TimeEvolving = TimeSpan.Zero;
            State = GeneticAlgorithmState.NotStarted;
            TaskExecutor = new LinearTaskExecutor();
            OperatorsStrategy = new DefaultOperatorsStrategy();
        }

        public void Start()
        {
            lock (m_lock)
            {
                State = GeneticAlgorithmState.Started;
                m_stopwatch = Stopwatch.StartNew();
                Population.CreateInitialGeneration();
                m_stopwatch.Stop();
                TimeEvolving = m_stopwatch.Elapsed;
            }

            Resume();
        }

        public void Resume()
        {
            try
            {
                lock (m_lock)
                {
                    m_stopRequested = false;
                }

                if (Population.GenerationsNumber == 0)
                {
                    throw new InvalidOperationException("Attempt to resume a genetic algorithm which was not yet started.");
                }

                if (Population.GenerationsNumber > 1)
                {
                    if (Termination.HasReached(this))
                    {
                        throw new InvalidOperationException("Attempt to resume a genetic algorithm with a termination ({0}) already reached. Please, specify a new termination or extend the current one.".With(Termination));
                    }

                    State = GeneticAlgorithmState.Resumed;
                }

                if (EndCurrentGeneration())
                {
                    return;
                }

                bool flag = false;
                while (!m_stopRequested)
                {
                    m_stopwatch.Restart();
                    flag = EvolveOneGeneration();
                    m_stopwatch.Stop();
                    TimeEvolving += m_stopwatch.Elapsed;
                    if (flag)
                    {
                        break;
                    }
                }
            }
            catch
            {
                State = GeneticAlgorithmState.Stopped;
                throw;
            }
        }

        public void Stop()
        {
            if (Population.GenerationsNumber == 0)
            {
                throw new InvalidOperationException("Attempt to stop a genetic algorithm which was not yet started.");
            }

            lock (m_lock)
            {
                m_stopRequested = true;
            }
        }

        private bool EvolveOneGeneration()
        {
            IList<IChromosome> parents = SelectParents();
            IList<IChromosome> list = Cross(parents);
            Mutate(list);
            IList<IChromosome> chromosomes = Reinsert(list, parents);
            Population.CreateNewGeneration(chromosomes);
            return EndCurrentGeneration();
        }

        private bool EndCurrentGeneration()
        {
            EvaluateFitness();
            Population.EndCurrentGeneration();
            this.GenerationRan?.Invoke(this, EventArgs.Empty);
            if (Termination.HasReached(this))
            {
                State = GeneticAlgorithmState.TerminationReached;
                this.TerminationReached?.Invoke(this, EventArgs.Empty);
                return true;
            }

            if (m_stopRequested)
            {
                TaskExecutor.Stop();
                State = GeneticAlgorithmState.Stopped;
            }

            return false;
        }

        private void EvaluateFitness()
        {
            try
            {
                List<IChromosome> list = Population.CurrentGeneration.Chromosomes.Where((IChromosome c) => !c.Fitness.HasValue).ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    IChromosome c2 = list[i];
                    TaskExecutor.Add(delegate
                    {
                        RunEvaluateFitness(c2);
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

            Population.CurrentGeneration.Chromosomes = Population.CurrentGeneration.Chromosomes.OrderByDescending((IChromosome c) => c.Fitness.Value).ToList();
        }

        private void RunEvaluateFitness(object chromosome)
        {
            IChromosome chromosome2 = chromosome as IChromosome;
            try
            {
                chromosome2.Fitness = Fitness.Evaluate(chromosome2);
            }
            catch (Exception ex)
            {
                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
            }
        }

        private IList<IChromosome> SelectParents()
        {
            return Selection.SelectChromosomes(Population.MinSize, Population.CurrentGeneration);
        }

        private IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            return OperatorsStrategy.Cross(Population, Crossover, CrossoverProbability, parents);
        }

        private void Mutate(IList<IChromosome> chromosomes)
        {
            OperatorsStrategy.Mutate(Mutation, MutationProbability, chromosomes);
        }

        private IList<IChromosome> Reinsert(IList<IChromosome> offspring, IList<IChromosome> parents)
        {
            return Reinsertion.SelectChromosomes(Population, offspring, parents);
        }
    }
}