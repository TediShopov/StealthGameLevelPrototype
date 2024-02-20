using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using System;

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
                    _time = Helpers.TrackExecutionTime(() => _value = Evaluate());
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
        private PatrolPath _debugEnenmy;

        public OverlappingGuardCoverage(GameObject level) : base(level, "Relative Level Coverage", 0)
        {
            Grid = Phenotype.GetComponentInChildren<Grid>(false);
            ObstacleLayerMask = LayerMask.GetMask("Obstacles");
        }

        public static Vector2[] GetFovOBB(FieldOfView fieldOfView)
        {
            float vd = fieldOfView.EnemyProperties.ViewDistance;
            float fov = fieldOfView.EnemyProperties.FOV;

            Vector2 minLeft = (Helpers.GetVectorFromAngle(fov / 2.0f) * vd);
            Vector2 maxRight = (Helpers.GetVectorFromAngle(-fov / 2.0f) * vd);

            Vector2[] localSpaceBox = new Vector2[]
            {
                new Vector2(0,0), new Vector2(vd,0),
                minLeft, maxRight
            }
            ;
            return localSpaceBox;
        }

        //public static Bounds GetFovBounds(FutureTransform ft, float vd)
        //{
        //    Bounds bounds = new Bounds(ft.Position, new Vector3(0, 0, 0));
        //    Vector2 minLeft = ft.Position + Vector2.Perpendicular(ft.Direction) * vd;
        //    Gizmos.DrawLine(ft.Position, minLeft);
        //    Vector2 maxRight = ft.Position + Vector2.Perpendicular(-ft.Direction) * vd;
        //    maxRight += ft.Direction * vd;

        //    Gizmos.DrawLine(ft.Position, maxRight);

        //    Vector2 actualMin = new Vector2(Mathf.Min(minLeft.x, maxRight.x), Mathf.Min(minLeft.y, maxRight.y));
        //    Vector2 actualMax = new Vector2(Mathf.Max(minLeft.x, maxRight.x), Mathf.Max(minLeft.y, maxRight.y));

        //    Gizmos.DrawSphere(actualMin, 0.1f);
        //    Gizmos.DrawSphere(actualMax, 0.1f);

        //    //            bounds.Encapsulate(minLeft);
        //    //            bounds.Encapsulate(maxRight);
        //    bounds.Encapsulate(actualMin);
        //    bounds.Encapsulate(actualMax);
        //    return bounds;
        //}
        public static Bounds GetFovBounds(FutureTransform ft, float vd, float fov)
        {
            Vector2 boundsCenter = ft.Position + ft.Direction * vd / 2.0f;
            Bounds bounds = new Bounds(boundsCenter, new Vector3(0, 0, 0));

            Vector2 fovPeak = ft.Position + ft.Direction * vd;
            Vector2 fovPos = ft.Position;
            Vector2 fovBoundTwo = ft.Position + (Vector2)(Quaternion.AngleAxis(fov / 2.0f, Vector3.forward) * ft.Direction * vd);
            Vector2 fovBoundOne = ft.Position + (Vector2)(Quaternion.AngleAxis(-fov / 2.0f, Vector3.forward) * ft.Direction * vd);
            Gizmos.DrawSphere(fovPeak, 0.1f);
            Gizmos.DrawSphere(fovPos, 0.1f);
            Gizmos.DrawSphere(fovBoundOne, 0.1f);
            Gizmos.DrawSphere(fovBoundTwo, 0.1f);

            //            bounds.Encapsulate(minLeft);
            //            bounds.Encapsulate(maxRight);
            bounds.Encapsulate(fovPeak);
            bounds.Encapsulate(fovPos);
            bounds.Encapsulate(fovBoundOne);
            bounds.Encapsulate(fovBoundTwo);
            return bounds;
        }

        public override void OnSelected()
        {
            if (_debugEnenmy is null) return;
            //Get enemy bounding box at 0
            Bounds bounds = GetFovBounds(
                _debugEnenmy.GetFutureTransform(0),
                _debugEnenmy.EnemyProperties.ViewDistance,
                _debugEnenmy.EnemyProperties.FOV);
            //Visualize bounding box
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            //            Vector2[] points = GetFovOBB(_debugEnenmy.FieldOfView);
            //            foreach (var p in points)
            //            {
            //                Gizmos.DrawSphere(p, 0.1f);
            //            }
        }

        public override float Evaluate()
        {
            //Get Future level instance
            var futureLevel = Phenotype.GetComponentInChildren<IFutureLevel>(false);
            var enemiesInLevel = Phenotype.GetComponentsInChildren<PatrolPath>();
            _debugEnenmy = enemiesInLevel.FirstOrDefault();
            NativeGrid<bool> native = new NativeGrid<bool>(Grid, Helpers.GetLevelBounds(Phenotype));
            native.SetAll((x, y, n) => false);
            float maxTime = ((ContinuosFutureLevel)futureLevel).EnemyPatrolPaths.Max(x => x.GetTimeToTraverse());

            //            for (float i = 0; i <= maxTime; i += futureLevel.Step)
            //            {
            //                foreach (var e in enemiesInLevel)
            //                {
            //                    foreach (var othere in enemiesInLevel)
            //                    {
            //                        if (e == othere) continue;
            //                        FutureTransform fte = e.GetFutureTransform(i);
            //                        FutureTransform ftothere = othere.GetFutureTransform(i);
            //                        float distanceBetweenEnemies = Vector2.Distance(fte.Position, ftothere.Position);
            //                        if (distanceBetweenEnemies > e.EnemyProperties.ViewDistance * 2)
            //                            continue;
            //
            //                        List<Vector3Int> epossibleAffected = EnemyDiscretizer.GetPossibleAffectedCells
            //                            (e, Grid, i);
            //                        List<Vector3Int> eotherpossibleAffected = EnemyDiscretizer.GetPossibleAffectedCells
            //                            (othere, Grid, i);
            //                        var combinedSet = eotherpossibleAffected.Union<Vector3Int>(eotherpossibleAffected).ToList();
            //
            //                        foreach (var cell in combinedSet)
            //                        {
            //                            Vector3 worldPos = Grid.GetCellCenterWorld(cell);
            //                            bool se = FieldOfView.TestCollision(worldPos, fte,
            //                                e.EnemyProperties.FOV, e.EnemyProperties.ViewDistance, ObstacleLayerMask);
            //                            bool sother = FieldOfView.TestCollision(worldPos, ftothere,
            //                                othere.EnemyProperties.FOV, othere.EnemyProperties.ViewDistance, ObstacleLayerMask);
            //                            if (e == true && sother == true)
            //                            {
            //                                Vector2Int natCoord = native.GetNativeCoord(new Vector2Int(cell.x, cell.y));
            //                                native.Set(natCoord.x, natCoord.y, true);
            //                            }
            //                        }
            //                    }
            //                }
            //            }

            return 0;
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

        return relCovarageEval.Value;
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