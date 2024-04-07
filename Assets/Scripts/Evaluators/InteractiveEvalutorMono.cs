using GeneticSharp;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct UserPreferenceModel
{
    public float OverallPathRisk;
    public float SuccessChance;
    public float PathUniqeness;
}

public class InteractiveEvalutorMono : EvaluatorMono
{
    //private List<MeasureMono> Evaluators = new List<MeasureMono>();
    public LevelProperties LevelProperties;

    public bool DoObjectiveDifficultyEvaluation;
    public StealthLevelIEMono IE;

    public void AppendAestheticMeasureToObject(GameObject level, LevelChromosomeBase chromosomeBase)
    {
        var asm = level.AddComponent<AestheticCriteriaMeasure>();
        asm.LevelProperties = LevelProperties;
        asm.Measure(level);
        chromosomeBase.AestheticProperties = new List<float>(asm.RealAestheticsMeasures);
    }

    public override double Evaluate(IChromosome chromosome)
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
            if (measurementData == null)
            {
                int b = 3;
            }
            //Assign actual measurement to the chromose object
            levelChromosome.Measurements = measurementData;

            //Attach mono behaviour to visualize the measurements
            ChromoseMeasurementsVisualizer.AttachDataVisualizer(levelObject.gameObject);

            Transform data = levelObject.transform.Find("Data");
            if (data is not null)
            {
                AppendAestheticMeasureToObject(data.gameObject, levelChromosome);
            }

            //TODO Apply a proper fitness formula

            double eval = 0;

            //Attaching fitness evaluation information to the object itself

            if (Evaluators.Any(x => x.IsTerminating))
                eval = 0.5f;
            else
            {
                if (DoObjectiveDifficultyEvaluation)
                {
                    var riskMeasure = levelObject.GetComponentInChildren<RiskMeasure>();
                    var pathUniqueness = levelObject.GetComponentInChildren<PathZoneUniqueness>();
                    var solver = levelObject.GetComponentInChildren<RRTSolverDifficultyEvaluation>();
                }
                else
                {
                    for (int i = 0; i < levelChromosome.AestheticProperties.Count; i++)
                    {
                        eval += levelChromosome.AestheticProperties[i] * IE.UserPreferences[i];
                    }
                    eval *= 10;
                }
            }

            return eval;
        }
        else
        {
            throw new System.ArgumentException("Expected level chromosome.");
        }
    }
}

public struct LevelMeasuredProperties
{
    public float SuccessChance;
    public float PathUniqeness;
}