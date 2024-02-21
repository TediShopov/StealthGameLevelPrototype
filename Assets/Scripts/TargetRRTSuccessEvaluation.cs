using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using System;
using Codice.CM.WorkspaceServer;
using UnityEngine.Profiling;

namespace StealthLevelEvaluation
{
    public abstract class PhenotypeFitnessEvaluation
    {
        private bool _evaluted;
        public string Name { get; }
        protected double _value;
        protected double _time;
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
                    Profiler.BeginSample(Name);
                    _time = Helpers.TrackExecutionTime(() => _value = Evaluate());
                    Profiler.EndSample();
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
            return $"{Name}: {Value}, For: {_time} \n";
        }

        public virtual void OnSelected()
        { }
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

    public class RelativeLevelCoverage : PhenotypeFitnessEvaluation
    {
        private Grid Grid;
        private LayerMask ObstacleLayerMask;

        public RelativeLevelCoverage(GameObject level) : base(level, "Relative Level Coverage", 0)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacles");
        }

        public override float Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            var _staticObstacleGrid = new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            _staticObstacleGrid.SetAll((row, col, ngrid) =>
            {
                if (Helpers.IsColidingCell(ngrid.GetWorldPosition(row, col), Grid.cellSize, ObstacleLayerMask))
                    return true;
                return false;
            });

            List<Vector2> allCells = new List<Vector2>();
            for (int i = 0; i < _staticObstacleGrid.GetRows(); i++)
            {
                for (int j = 0; j < _staticObstacleGrid.GetCols(); j++)
                {
                    if (_staticObstacleGrid.Get(i, j) == false)
                        allCells.Add(_staticObstacleGrid.GetWorldPosition(i, j));
                }
            }

            var continuosFuturelevel = (ContinuosFutureLevel)futureLevel;

            float maxTime = continuosFuturelevel.EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());
            var notcolliding = continuosFuturelevel.AreNotCollidingDynamicDiscrete(allCells, 0, maxTime);
            var _visibilityCountGrid = new NativeGrid<bool>(_staticObstacleGrid);
            _visibilityCountGrid.SetAll((x, y, _visibilityCountGrid) => false);
            foreach (var worldPos in notcolliding)
            {
                Vector2Int nativeCoord = _visibilityCountGrid.GetNativeCoord((Vector2Int)Grid.WorldToCell(new Vector3(worldPos.x, worldPos.y)));
                _visibilityCountGrid.Set(nativeCoord.x, nativeCoord.y, true);
            }

            int maxCells = _visibilityCountGrid.GetCols() * _visibilityCountGrid.GetRows();
            int colliding = maxCells - notcolliding.Count;
            float relCoverage = (float)colliding / (float)maxCells;
            return relCoverage * 100;
        }
    }

    public class OverlappingGuardCoverage : PhenotypeFitnessEvaluation
    {
        private Grid Grid;
        private LayerMask ObstacleLayerMask;
        private PatrolPath[] _debugEnenmies;

        public OverlappingGuardCoverage(GameObject level) : base(level, "Average realtive overlapping areas", 0)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacle");
        }

        private void DebugDrawDiscreteBounds(Bounds bounds, Color color)
        {
            Gizmos.color = color;
            foreach (var cells in DiscretBoundsCells(bounds))
            {
                Gizmos.DrawSphere(Grid.GetCellCenterWorld(cells), 0.1f);
            }
        }

        private List<Vector3Int> DiscretBoundsCells(Bounds bounds)
        {
            List<Vector3Int> worldPositions = new List<Vector3Int>();
            Vector3Int gridMin = Grid.WorldToCell(bounds.min);
            Vector3Int gridMax = Grid.WorldToCell(bounds.max);
            for (int rows = gridMin.y; rows < gridMax.y; rows++)
            {
                for (int cols = gridMin.x; cols < gridMax.x; cols++)
                {
                    worldPositions.Add((new Vector3Int(cols, rows, 0)));
                }
            }
            return worldPositions;
        }

        public override void OnSelected()
        {
            if (_debugEnenmies is null) return;
            for (int i = 0; i < _debugEnenmies.Length - 1; i++)
            {
                for (int j = i + 1; j < _debugEnenmies.Length; j++)
                {
                    var e = _debugEnenmies[i];
                    var othere = _debugEnenmies[j];
                    float vd = e.EnemyProperties.ViewDistance;
                    float fov = e.EnemyProperties.FOV;
                    Bounds bounds = FieldOfView.GetFovBounds(
                        e.GetFutureTransform(0),
                    e.EnemyProperties.ViewDistance,
                    e.EnemyProperties.FOV);
                    Bounds otherBounds = FieldOfView.GetFovBounds(
                        othere.GetFutureTransform(0),
                        othere.EnemyProperties.ViewDistance,
                        othere.EnemyProperties.FOV);
                    if (bounds.Intersects(otherBounds))
                    {
                        var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                        DebugDrawDiscreteBounds(overlapp, Color.magenta);
                        List<Vector3Int> visibleCoordinates =
                            DiscretBoundsCells(overlapp)
                            .Where(x =>
                            {
                                var pos = Grid.GetCellCenterWorld(x);
                                bool one = FieldOfView.TestCollision(pos, e.GetFutureTransform(0), fov, vd, ObstacleLayerMask);
                                bool other = FieldOfView.TestCollision(pos, othere.GetFutureTransform(0), fov, vd, ObstacleLayerMask);
                                if (one && other)
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawSphere(pos, 0.1f);
                                }
                                return one && other;
                            }).ToList();
                    }
                }
            }
        }

        public override float Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            _debugEnenmies = Phenotype.GetComponentsInChildren<PatrolPath>();
            NativeGrid<bool> native = new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            native.SetAll((x, y, n) => false);
            float maxTime = ((ContinuosFutureLevel)futureLevel)
                .EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());

            float vd = _debugEnenmies[0].EnemyProperties.ViewDistance;
            float fov = _debugEnenmies[0].EnemyProperties.FOV;
            //Formula: angel in radians multipled by radius on the power of 2
            float maxOverlappArea = Mathf.Deg2Rad * fov * vd * vd;
            float accumulatedOverlapp = 0;
            Helpers.LogExecutionTime(() => accumulatedOverlapp = NewAccumualtedOverlapp(futureLevel, maxTime, vd, fov, maxOverlappArea), "New Overlapp");
            float avgRelOverlapp = accumulatedOverlapp / maxTime;
            return -avgRelOverlapp * 100;
        }

        private float NewAccumualtedOverlapp(IFutureLevel futureLevel, float maxTime, float vd, float fov, float maxOverlappArea)
        {
            List<BacktrackPatrolPath> simulatedPaths = _debugEnenmies
                .Select(x => new BacktrackPatrolPath(x.BacktrackPatrolPath)).ToList();

            float accumulatedOverlapp = 0;
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                //Move all paths
                simulatedPaths.ForEach(x => x.MoveAlong(futureLevel.Step * _debugEnenmies[0].EnemyProperties.Speed));
                for (int i = 0; i < _debugEnenmies.Length - 1; i++)
                {
                    FutureTransform enemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[i]);
                    Bounds bounds = FieldOfView.GetFovBounds(enemyFT, vd, fov);
                    for (int j = i + 1; j < _debugEnenmies.Length; j++)
                    {
                        FutureTransform otherEnemyFT = PatrolPath.GetPathOrientedTransform(simulatedPaths[j]);
                        Bounds otherBounds = FieldOfView.GetFovBounds(otherEnemyFT, vd, fov);
                        if (bounds.Intersects(otherBounds))
                        {
                            Profiler.BeginSample("Bounds intersecting");
                            var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                            Profiler.EndSample();
                            Profiler.BeginSample("Cell visibility checking");
                            List<Vector3Int> visibleCoordinates =
                                DiscretBoundsCells(overlapp)
                                .Where(x =>
                                {
                                    var pos = Grid.GetCellCenterWorld(x);
                                    bool one = FieldOfView.TestCollision(pos, enemyFT, fov, vd, ObstacleLayerMask);
                                    bool other = FieldOfView.TestCollision(pos, otherEnemyFT, fov, vd, ObstacleLayerMask);
                                    return one && other;
                                }).ToList();
                            Profiler.EndSample();
                            float estimatedOverlappArea = visibleCoordinates.Count * (Grid.cellSize.x * Grid.cellSize.y);
                            float relativeOverlappArea = estimatedOverlappArea / maxOverlappArea;
                            accumulatedOverlapp += relativeOverlappArea;
                        }
                    }
                }
            }

            return accumulatedOverlapp;
        }

        private float OldEvaluation(IFutureLevel futureLevel, float maxTime, float vd, float fov, float maxOverlappArea)
        {
            float accumulatedOverlapp = 0;
            for (float time = 0; time <= maxTime; time += futureLevel.Step)
            {
                for (int i = 0; i < _debugEnenmies.Length - 1; i++)
                {
                    for (int j = i + 1; j < _debugEnenmies.Length; j++)
                    {
                        var e = _debugEnenmies[i];
                        var othere = _debugEnenmies[j];
                        Bounds bounds = FieldOfView.GetFovBounds(e.GetFutureTransform(time), vd, fov);
                        Bounds otherBounds = FieldOfView.GetFovBounds(othere.GetFutureTransform(time), vd, fov);
                        if (bounds.Intersects(otherBounds))
                        {
                            Profiler.BeginSample("Bounds intersecting");
                            var overlapp = Helpers.IntersectBounds(bounds, otherBounds);
                            Profiler.EndSample();
                            Profiler.BeginSample("Cell visibility checking");
                            List<Vector3Int> visibleCoordinates =
                                DiscretBoundsCells(overlapp)
                                .Where(x =>
                                {
                                    var pos = Grid.GetCellCenterWorld(x);
                                    bool one = FieldOfView.TestCollision(pos, e.GetFutureTransform(time), fov, vd, ObstacleLayerMask);
                                    bool other = FieldOfView.TestCollision(pos, othere.GetFutureTransform(time), fov, vd, ObstacleLayerMask);
                                    return one && other;
                                }).ToList();
                            Profiler.EndSample();
                            float estimatedOverlappArea = visibleCoordinates.Count * (Grid.cellSize.x * Grid.cellSize.y);
                            float relativeOverlappArea = estimatedOverlappArea / maxOverlappArea;
                            accumulatedOverlapp += relativeOverlappArea;
                        }
                    }
                }
            }

            return accumulatedOverlapp;
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
        StealthLevelEvaluation.PhenotypeFitnessEvaluation relCovarageEval =
            new StealthLevelEvaluation.RelativeLevelCoverage(generator.gameObject);
        StealthLevelEvaluation.PhenotypeFitnessEvaluation overlappingCoveredArea =
            new StealthLevelEvaluation.OverlappingGuardCoverage(generator.gameObject);
        //double evaluatedFitness = EvaluateDifficultyMeasureOfSuccesful(chromosome);
        var infoObj = FitnessInfoVisualizer.AttachInfo(generator.gameObject,
            new FitnessInfo(eval, relCovarageEval, overlappingCoveredArea));
        levelChromose.FitnessInfo = infoObj;

        //Attaching fitness evaluation information to the object itself
        return infoObj.FitnessEvaluations.Sum(x => x.Value);
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