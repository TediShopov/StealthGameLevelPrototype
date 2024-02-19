using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;

namespace StealthLevelEvaluation
{
    public abstract class PhenotypeFitnessEvaluation
    {
        private bool _evaluted;
        public string Name { get; }
        protected double _value;
        protected GameObject Phenotype;

        public PhenotypeFitnessEvaluation(GameObject phenotype, string name, double defValue)
        {
            Phenotype = phenotype;
            Name = name;
            _value = defValue;
            _evaluted = false;
        }

        public double Value
        {
            get
            {
                if (!_evaluted)
                {
                    _evaluted = true;
                    _value = Evaluate();
                }
                else
                {
                }
                return _value;
            }
        }

        //Accepts the phenotype of a generated level and assigns a fitness value
        public abstract float Evaluate();

        public override string ToString()
        {
            //Get the value getter so value is sure to be calculated
            return $"{Name}: {Value}";
        }
    }

    public class RiskMeasureOfSolutionEvaluation : PhenotypeFitnessEvaluation
    {
        public RiskMeasureOfSolutionEvaluation(GameObject level) : base(level, "Risk Measure of solutions", 0)
        {
        }

        public override float Evaluate()
        {
            var RRTVisualizers = Phenotype.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
            var enemyPatrolPaths = Phenotype.GetComponentsInChildren<PatrolPath>();
            //var voxelizedLevel = generator.GetComponentInChildren<VoxelizedLevel>();
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>();
            float total = 0;
            int succeeded = 0;
            foreach (var x in RRTVisualizers)
            {
                if (x.RRT.Succeeded())
                {
                    var solutionPath =
                        new SolutionPath(x.RRT.ReconstructPathToSolution());
                    var riskMeasure = new FieldOfViewRiskMeasure(
                        solutionPath,
                        enemyPatrolPaths.ToList(),
                        enemyPatrolPaths[0].EnemyProperties,
                        LayerMask.GetMask("Obstacles"));
                    float overallRisk = riskMeasure.OverallRisk(futureLevel.Step);
                    total += overallRisk;
                    succeeded++;
                }
            }
            float avg = total / (float)succeeded;
            return avg;
        }
    }
}

public class TargetRRTSuccessEvaluation : MonoBehaviour, IFitness
{
    public LevelPhenotypeGenerator LevelGeneratorPrototype;
    public Vector2 LevelSize;

    //Levels must be physically spawned in a scene to be evaluated.
    private LevelPhenotypeGenerator[,] levelGenerators;

    private int currentIndex = 0;

    //Only one as it is assumed it is a square
    public int GridDimension;

    private LevelPhenotypeGenerator GetCurrentGenerator()
    {
        if (currentIndex >= GridDimension * GridDimension)
            return null;

        return levelGenerators[currentIndex / GridDimension, currentIndex % GridDimension];
    }

    public void SpawnGridOfEmptyGenerators(int populationCount)
    {
        GridDimension = Mathf.CeilToInt(Mathf.Sqrt(populationCount));

        //Setup Generator Prototype
        LevelGeneratorPrototype.isRandom = true;
        LevelGeneratorPrototype.RunOnStart = false;
        if (levelGenerators != null)
        {
            this.PrepareForNewGeneration();
        }
        levelGenerators = new LevelPhenotypeGenerator[GridDimension, GridDimension];

        for (int i = 0; i < GridDimension; i++)
        {
            for (int j = 0; j < GridDimension; j++)
            {
                Vector3 pos = new Vector3(i * LevelSize.x, j * LevelSize.y, 0);
                var g = Instantiate(this.LevelGeneratorPrototype, pos, Quaternion.identity, this.transform);
                levelGenerators[i, j] = g;
            }
        }
    }

    public double Evaluate(IChromosome chromosome)
    {
        var generator = GetCurrentGenerator();
        currentIndex++;
        if (generator == null) return 0;

        var levelChromose = (LevelChromosome)chromosome;

        generator.Generate(levelChromose);
        StealthLevelEvaluation.PhenotypeFitnessEvaluation eval =
            new StealthLevelEvaluation.RiskMeasureOfSolutionEvaluation(generator.gameObject);
        //double evaluatedFitness = EvaluateDifficultyMeasureOfSuccesful(chromosome);
        var infoObj = FitnessInfoVisualizer.AttachInfo(generator.gameObject,
            new FitnessInfo(eval));
        levelChromose.FitnessInfo = infoObj;
        //Attaching fitness evaluation information to the object itself

        return eval.Value;
    }

    public void PrepareForNewGeneration()
    {
        //Clearing old data
        DisposeOldPopulation();
        //Resetting index
        currentIndex = 0;
    }

    //Once a new population has been started the gameobject generated must be cleared
    private void DisposeOldPopulation()
    {
        Debug.Log("Disposing previous population generators");
        foreach (var generator in levelGenerators)
        {
            generator.Dispose();
        }
    }
}