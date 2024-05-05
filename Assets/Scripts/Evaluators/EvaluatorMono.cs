using GeneticSharp;
using log4net.Appender;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EvaluatorMono : MonoBehaviour, IFitness, IPrototypable<EvaluatorMono>
{
    [SerializeField] public GameObject EvaluatorHolder;
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();

    public LevelChromosomeBase TryGetValidLevelChromosome(IChromosome chromosome)
    {
        if (chromosome == null)
            throw new System.ArgumentException("Level chromosome is null");
        if (chromosome is LevelChromosomeBase)
        {
            LevelChromosomeBase levelChromosome = (LevelChromosomeBase)chromosome;

            //Only after level chromosome has been manifested to phenotype
            if (levelChromosome.Manifestation == null)
                throw new System.ArgumentException("Level evaluator operatos on level that have their phenotype derived");
            return levelChromosome;
        }
        throw new System.ArgumentException("Level evaluator require ohe chromosome to inherite from LevelChromosomeBase");
    }

    public double AttachToAndEvaluate(IChromosome chromosome)
    {
        LevelChromosomeBase levelChromosome = TryGetValidLevelChromosome(chromosome);
        EvaluatorMono evaluatorPrototype = this.PrototypeComponent(levelChromosome.Manifestation);
        return evaluatorPrototype.Evaluate(chromosome);
    }

    public virtual double Evaluate(IChromosome chromosome)
    {
        return 0;
    }

    public GameObject AttachEvaluatorContainer(GameObject to)
    {
        var containerForEvaluationPrototype = new GameObject(this.name);
        containerForEvaluationPrototype.transform.SetParent(to.transform, false);
        return containerForEvaluationPrototype;
    }

    public virtual EvaluatorMono PrototypeComponent(GameObject to)
    {
        throw new System.NotImplementedException();
    }
}