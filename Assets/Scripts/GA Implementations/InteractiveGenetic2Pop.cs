using GeneticSharp.Domain;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GeneticSharp
{
    public class InteractiveGenetic2Pop : InteractiveGeneticAlgorithm
    {
        private FeasibleSelectionWrapper _feasibleSelection;
        private FeasibleSelectionWrapper _infeasibleSelection;

        public override void SetupGA()
        {
            base.SetupGA();

            this._feasibleSelection =
                new FeasibleSelectionWrapper(2, true, this.Selection);
            this._infeasibleSelection =
                new FeasibleSelectionWrapper(2, false, this.Selection);
        }

        public InteractiveGenetic2Pop(
            IFitness fitness,
            ISelection selection,
            ICrossover crossover,
            IMutation mutation) : base(fitness, selection, crossover, mutation)
        {
            this.CreateFrom(fitness, selection, crossover, mutation);
        }

        public override void EvolveOneGeneration()
        {
            //Catch a bug where feasibilkity is false but fitness is not -100

            //Evolve feasible
            int eMin = Mathf.FloorToInt(Population.MinSize * 0.5f);
            int eMax = Mathf.FloorToInt(Population.MaxSize * 0.5f);
            var feasible = EvolveIsland(eMin, eMax, _feasibleSelection);

            //Infeasible offspring occupy the needed space to fill the
            //population size  marks
            var infeasbile = EvolveIsland(
                Population.MinSize - feasible.Count,
                Population.MaxSize - feasible.Count,
                _infeasibleSelection);

            Debug.Log($"_DEB_ Feasible: {feasible.Count}");
            Debug.Log($"_DEB_ Infeasible: {infeasbile.Count}");

            var combined = feasible.Concat(infeasbile).ToList();
            Debug.Log($"_DEB_ Combined: {combined.Count}");
            PopulationPhenotypeLayout.CreateNewGeneration(combined);
            CheckValidityOfFeasibles();

            //            Debug.Log($"_DEB_ After Insertion: {newGenerationChromosomes.Count}");
            //            PopulationPhenotypeLayout.CreateNewGeneration(newGenerationChromosomes);
            //return EndCurrentGeneration();
        }

        private IList<IChromosome> EvolveIsland(int min, int max, ISelection selection)
        {
            var pop = PopulationPhenotypeLayout;
            int oldMin = pop.Min;
            int oldMax = pop.Max;

            Population.MinSize = min;
            Population.MaxSize = max;

            var parents =
                selection.SelectChromosomes(Population.MinSize, pop.CurrentGeneration);

            var offspring =
                OperatorsStrategy.Cross(pop, Crossover, CrossoverProbability, parents);

            OperatorsStrategy.Mutate(Mutation, MutationProbability, offspring);

            //Modulate minsize to and maxsize of population to reinsert correctyl
            offspring =
                Reinsertion.SelectChromosomes(PopulationPhenotypeLayout,
                offspring, parents);

            Population.MinSize = oldMin;
            Population.MaxSize = oldMax;
            return offspring;
        }
    }

    /// <summary>
    /// A wrapper of a selection strategies that filters chromosome in generation
    /// by their feasibility
    /// </summary>
    public class FeasibleSelectionWrapper : SelectionBase
    {
        private ISelection PrimarySelectionStrategy;
        private bool Feasibility;

        public FeasibleSelectionWrapper(int min, bool feasibility, ISelection other)
            : base(min)
        {
            PrimarySelectionStrategy = other;
            Feasibility = feasibility;
        }

        protected override IList<IChromosome> PerformSelectChromosomes(int number, Generation generation)
        {
            try
            {
                var feasibleGeneration = new Generation(generation.Number,
                    generation.Chromosomes.Where(
                        x => ((LevelChromosomeBase)x).Feasibility == Feasibility).ToList());

                Debug.Log($"Feasibility {Feasibility}, Generaiton Count: {feasibleGeneration.Chromosomes.Count}");

                return PrimarySelectionStrategy.SelectChromosomes(number, feasibleGeneration);
            }
            catch (System.Exception)
            {
                return new List<IChromosome>();
            }
        }
    }
}