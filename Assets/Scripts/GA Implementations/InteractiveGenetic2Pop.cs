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
            //Evolve feasible
            var feasible = EvolveIsland(_feasibleSelection);
            var infeasbile = EvolveIsland(_infeasibleSelection);

            Debug.Log($"_DEB_ Feasible: {feasible.Count}");
            Debug.Log($"_DEB_ Infeasible: {infeasbile.Count}");

            var combined = feasible.Concat(infeasbile).ToList();
            Debug.Log($"_DEB_ Combined: {combined.Count}");

            var newGenerationChromosomes =
                Reinsertion.SelectChromosomes(PopulationPhenotypeLayout,
                combined, PopulationPhenotypeLayout.CurrentGeneration.Chromosomes);
            Debug.Log($"_DEB_ After Insertion: {newGenerationChromosomes.Count}");
            PopulationPhenotypeLayout.CreateNewGeneration(newGenerationChromosomes);
            //return EndCurrentGeneration();
        }

        private IList<IChromosome> EvolveIsland(ISelection selection)
        {
            var pop = PopulationPhenotypeLayout;
            var parents =
                selection.SelectChromosomes(Mathf.FloorToInt(pop.MinSize * 0.5f), pop.CurrentGeneration);

            var offspring =
                OperatorsStrategy.Cross(pop, Crossover, CrossoverProbability, parents);

            OperatorsStrategy.Mutate(Mutation, MutationProbability, offspring);

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
                    generation.Chromosomes.Where(x => ((LevelChromosomeBase)x).Feasibility == Feasibility).ToList());

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