using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Linq;
using UnityEngine;

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

    public TargetRRTSuccessEvaluation(int populationCount)
    {
        SpawnGridOfEmptyGenerators(populationCount);
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
        return EvaluateDifficultyMeasureOfSuccesful(chromosome);
    }

    private double EvaluateRRTSuccesRate(IChromosome chromosome)
    {
        var generator = GetCurrentGenerator();
        currentIndex++;
        if (generator == null) return 0;
        generator.Generate((LevelChromosome)chromosome);
        var RRTVisualizers = generator.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
        int successful = RRTVisualizers.Count(x => x.RRT.Succeeded() == true);
        double successRate = (double)successful / (double)RRTVisualizers.Count();
        Debug.Log($"Evaluated at {successRate}");
        //Generator.Dispose();
        return successRate;
    }

    private double EvaluateDifficultyMeasureOfSuccesful(IChromosome chromosome)
    {
        var generator = GetCurrentGenerator();
        currentIndex++;
        if (generator == null) return 0;
        generator.Generate((LevelChromosome)chromosome);
        var RRTVisualizers = generator.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
        var enemyPatrolPaths = generator.GetComponentsInChildren<PatrolPath>();
        //var voxelizedLevel = generator.GetComponentInChildren<VoxelizedLevel>();
        var futureLevel = generator.GetComponentInChildren<IFutureLevel>();
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
        Debug.Log($"Minimum overall risk is:  {avg}");
        //Generator.Dispose();
        return avg;
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