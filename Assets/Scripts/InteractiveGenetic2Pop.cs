using GeneticSharp.Domain;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GeneticSharp
{
    public class InteractiveGenetic2Pop : InteractiveGeneticAlgorithm
    {
        public FeasibleSelectionWrapper FeasibleSelection;
        public FeasibleSelectionWrapper InfeasibleSelection;

        public override void SetupGA()
        {
            base.SetupGA();

            this.FeasibleSelection = new FeasibleSelectionWrapper(2, true, this.Selection);
            this.InfeasibleSelection = new FeasibleSelectionWrapper(2, false, this.Selection);
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
            var feasible = EvolveIsland(FeasibleSelection);
            var infeasbile = EvolveIsland(InfeasibleSelection);

            var combined = feasible.Concat(infeasbile).ToList();
            PopulationPhenotypeLayout.CreateNewGeneration(combined);
            //return EndCurrentGeneration();
        }

        private IList<IChromosome> EvolveIsland(ISelection selection)
        {
            var pop = PopulationPhenotypeLayout;
            var parents =
                selection.SelectChromosomes(pop.MinSize, pop.CurrentGeneration);

            var offspring =
                OperatorsStrategy.Cross(pop, Crossover, CrossoverProbability, parents);

            OperatorsStrategy.Mutate(Mutation, MutationProbability, offspring);

            var newGenerationChromosomes =
                Reinsertion.SelectChromosomes(pop, offspring, parents);
            return newGenerationChromosomes;
        }
    }

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
                    generation.Chromosomes.Where(x => ((LevelChromosomeBase)x).IsFeasible() == Feasibility).ToList());

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