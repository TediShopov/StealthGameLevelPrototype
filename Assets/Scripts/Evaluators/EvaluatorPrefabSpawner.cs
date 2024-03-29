using GeneticSharp;
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
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();

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
            MeasureMono[] Evaluators = evaluator.GetComponents<MeasureMono>();

            //Run Validators
            Evaluators = Evaluators.OrderByDescending(x => x.IsValidator).ToArray();
            foreach (var e in Evaluators)
            {
                e.Init(levelObject.gameObject);
                e.DoMeasure(levelObject.gameObject);
                if (e.IsTerminating)
                    break;
            }
            var measurementData = new MeasurementsData(Evaluators.Select(x => x.Result).ToArray());

            //Assign actual measurement to the chromose object
            levelChromosome.Measurements = measurementData;

            //Attach mono behaviour to visualize the measurements
            ChromoseMeasurementsVisualizer.AttachDataVisualizer(levelObject.gameObject);

            //TODO Apply a proper fitness formula

            //Attaching fitness evaluation information to the object itself
            if (measurementData.FitnessEvaluations.Any(x => x.IsValidation && float.Parse(x.Value) == 0.0f))
                return 0;
            return measurementData.FitnessEvaluations
                .Where(x => x.IsValidation == false)
                .Sum(x => float.Parse(x.Value));
        }
        else
        {
            throw new System.ArgumentException("Expected level chromosome.");
        }
    }
}