using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EvaluatorPrefabSpawner : MonoBehaviour, IFitness
{
    public GameObject EvaluatorHolder;
    public GridObjectLayout GridLevelObjects;
    //private List<PhenotypeFitnessEvaluation> Evaluators = new List<PhenotypeFitnessEvaluation>();

    public double Evaluate(IChromosome chromosome)
    {
        if (chromosome is LevelChromosomeBase)
        {
            LevelChromosomeBase levelChromosome = (LevelChromosomeBase)chromosome;

            //Get the gameobject that is to hold
            var levelObject = GridLevelObjects.GetNextLevelObject();
            if (levelChromosome == null) return 0;
            if (levelChromosome.PhenotypeGenerator == null) return 0;

            //Run the generators --> the game object is now tagged as level
            levelChromosome.PhenotypeGenerator.Generate(levelChromosome, levelObject);

            var evaluator = Instantiate(EvaluatorHolder, levelObject.transform);
            //Get all evaluators from  the prefab
            PhenotypeFitnessEvaluation[] Evaluators = evaluator.GetComponents<PhenotypeFitnessEvaluation>();
            var info = FitnessInfoVisualizer.AttachInfo(levelObject.gameObject, new FitnessInfo());
            foreach (var e in Evaluators)
            {
                e.Init(levelObject.gameObject);
                e.Evaluate();
                info.FitnessEvaluations.Add(e);
            }
            levelChromosome.FitnessInfo = info;
            //Attaching fitness evaluation information to the object itself
            return info.FitnessEvaluations.Sum(x => x.Value);
        }
        else
        {
            throw new System.ArgumentException("Expected level chromosome.");
        }
    }
}