using GeneticSharp;
using log4net.Appender;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EvaluatorMono : MonoBehaviour, IFitness
{
    public GameObject EvaluatorHolder;
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();

    public LevelChromosomeBase CheckValidLevelChromosome(IChromosome chromosome)
    {
        if (chromosome == null)
            throw new System.ArgumentException("Level chromosome is null");
        if (chromosome is LevelChromosomeBase)
        {
            LevelChromosomeBase levelChromosome = (LevelChromosomeBase)chromosome;

            //Only after level chromosome has been manifested to phenotype
            if (levelChromosome.Phenotype == null)
                throw new System.ArgumentException("Level evaluator operatos on level that have their phenotype derived");
            return levelChromosome;
        }
        throw new System.ArgumentException("Level evaluator require ohe chromosome to inherite from LevelChromosomeBase");
    }

    public virtual double Evaluate(IChromosome chromosome)
    {
        return 0;
        //        var levelChromosome = CheckValidLevelChromosome(chromosome);
        //        GameObject levelObject = levelChromosome.Phenotype;
        //        //Run the generators --> the game object is now tagged as level
        //        levelChromosome.PhenotypeGenerator.Generate(levelChromosome, levelObject);
        //
        //        var evaluator = Instantiate(EvaluatorHolder, levelObject.transform);
        //        //Get all evaluators from  the prefab
        //        MeasureMono[] Evaluators = evaluator.GetComponents<MeasureMono>();
        //
        //        //Run Validators
        //        Evaluators = Evaluators.OrderByDescending(x => x.IsValidator).ToArray();
        //        foreach (var e in Evaluators)
        //        {
        //            e.Init(levelObject.gameObject);
        //            e.DoMeasure(levelObject.gameObject);
        //            if (e.IsTerminating)
        //                break;
        //        }
        //        var measurementData = new List<MeasureResult>(Evaluators.Select(x => x.Result).ToArray());
        //
        //        if (measurementData == null)
        //        {
        //            int b = 3;
        //        }
        //        //Assign actual measurement to the chromose object
        //        levelChromosome.Measurements = measurementData;
        //
        //        //Attach mono behaviour to visualize the measurements
        //        ChromoseMeasurementsVisualizer.AttachDataVisualizer(levelObject.gameObject);
        //
        //        //TODO Apply a proper fitness formula
        //
        //        double eval = 0;
        //
        //        //Attaching fitness evaluation information to the object itself
        //        if (Evaluators.Any(x => x.IsTerminating))
        //            eval = 0.5f;
        //        else
        //        {
        //            var riskMeasure = levelObject.GetComponentInChildren<RiskMeasure>();
        //            var pathUniqueness = levelObject.GetComponentInChildren<PathZoneUniqueness>();
        //            var solver = levelObject.GetComponentInChildren<RRTSolverDifficultyEvaluation>();
        //            if (riskMeasure != null && pathUniqueness != null)
        //            {
        //                if (pathUniqueness.SeenPaths.Count == 0)
        //                    eval = 0.5f;
        //                else
        //                {
        //                    eval = riskMeasure.RiskMeasures.Min()
        //                        * pathUniqueness.SeenPaths.Count
        //                        * solver.Chance * 30;
        //                }
        //            }
        //            else
        //            {
        //                //                    eval = measurementData.FitnessEvaluations
        //                //                        .Where(x => x.IsValidation == false)
        //                //                        .Sum(x => float.Parse(x.Value));
        //                eval = 0;
        //            }
        //        }
        //
        //        return eval;
    }
}