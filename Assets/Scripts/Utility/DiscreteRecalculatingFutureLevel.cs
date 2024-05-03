using JetBrains.Annotations;
using PlasticPipe.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

[Serializable]
public class DynamicLevelSimulation
{
    [SerializeField] private float From;
    [SerializeField] private float To;
    [SerializeField] private float TimeStep;
    [SerializeField] public IEnumerable<IPredictableThreat> Threats;

    public int StepCount =>
      Mathf.FloorToInt((To - From) / (float)TimeStep);

    public float CurrentTime = 0;
    public bool IsFinished => CurrentTime > To;

    public DynamicLevelSimulation(
        IEnumerable<IPredictableThreat> threats,
        float from,
        float to,
        float globalTimestep)
    {
        Threats = threats;
        foreach (var t in Threats)
            t.Reset();

        From = MathF.Floor(from / globalTimestep) * globalTimestep;
        To = MathF.Ceiling(to / globalTimestep) * globalTimestep;

        //        From = from;
        //        To = to;
        TimeStep = globalTimestep;
        foreach (var threat in Threats)
            threat.TimeMove(From);
        CurrentTime = From;
    }

    public void Progress()
    {
        CurrentTime += TimeStep;
        foreach (var threat in Threats)
            threat.TimeMove(TimeStep);
    }
}

public interface IPrototypable<T>
{
    T PrototypeComponent(GameObject to);
}

public interface IClusterable
{
    //Runs the level simulation in discrete timesteps and log each
    // time a guards oversee a certain cell.
    //Return normalized values
    NativeGrid<float> PredicableThreatHeatmap(UnboundedGrid grid);

    NativeGrid<float> GetHeatmap();
}

[Serializable]
public class DiscreteRecalculatingFutureLevel :
    IFutureLevel
{
    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    [SerializeReference] public Transform LevelManifest;
    private Collider2D _boundary;

    [SerializeField]
    public NativeGrid<float> _clusteredThreats;

    public DiscreteRecalculatingFutureLevel(
        float step, float iterations, LevelProperties levelProperties)
    {
        this.ObstacleLayerMask = levelProperties.ObstacleLayerMask;
        this.BoundaryLayerMask = levelProperties.BoundaryLayerMask;
    }

    public DiscreteRecalculatingFutureLevel(
        float step, float iterations)
    {
        this._step = step;
        this._iter = iterations;
    }

    public DiscreteRecalculatingFutureLevel()
    {
    }

    public NativeGrid<float> GetThreatHeatmap() => new NativeGrid<float>(_clusteredThreats);

    //public PatrolPath[] EnemyPatrolPaths;
    [SerializeReference] private List<IPredictableThreat> _threats;

    public List<IPredictableThreat> DynamicThreats
    {
        get { return _threats; }
        set { _threats = value; }
    }

    [SerializeField] protected float _step = 0.2f;
    [SerializeField] protected float _iter = 50;
    private UnboundedGrid _grid;

    public float Step => _step;

    public float Iterations => _iter;

    public Transform GlobalTransform
    {
        get { return LevelManifest; }
        set { LevelManifest = value; }
    }

    public DynamicLevelSimulation GetFullSimulation()
    {
        return new DynamicLevelSimulation(DynamicThreats, 0, GetMaxSimulationTime(), Step);
    }

    public virtual void Generate(LevelPhenotype levelPhenotype)
    {
        Bounds = CalculateBounds(levelPhenotype);
        _grid = levelPhenotype.Zones.Grid;
        SolutionPaths = new List<List<Vector3>>();
        DynamicThreats = levelPhenotype.Threats;
        foreach (var threat in DynamicThreats)
            threat.GlobalTransform = GlobalTransform;
        _clusteredThreats = PredicableThreatHeatmap(_grid);
        //StartCoroutine(RefreshLevelSolutionObjects());
    }

    [SerializeField, HideInInspector] private Bounds Bounds;

    public Bounds GetBounds()
    {
        return Bounds;
    }

    public Bounds CalculateBounds(LevelPhenotype levelPhenotype)
    {
        //Vector3 min = levelPhenotype.Zones.WorldMin;
        //Vector3 max = levelPhenotype.Zones.WorldMax;

        Vector3 min = levelPhenotype.Zones.WorldMin;
        Vector3 max = levelPhenotype.Zones.WorldMax;
        min.z = 0;
        max.z = Iterations * Step;
        Bounds bounds = new Bounds();
        bounds.min = min;
        bounds.max = max;
        return bounds;
    }

    //    // Start is called before the first frame update
    //    public virtual void Init()
    //    {
    //        Profiler.BeginSample("Continuos Representation");
    //        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
    //        _grid = level.GetComponentInChildren<LevelChromosomeMono>().Chromosome.Phenotype.Zones.Grid;
    //
    //        SolutionPaths = new List<List<Vector3>>();
    //        DynamicThreats = level.GetComponentsInChildren<IPredictableThreat>();
    //        _clusteredThreats = PredicableThreatHeatmap(_grid);
    //        //EnemyPatrolPaths = GetEnemyPatrolPaths();
    //        //enemyPaths[i].BacktrackPatrolPath = new BacktrackPatrolPath(paths[i]);
    //        StartCoroutine(RefreshLevelSolutionObjects());
    //        Profiler.EndSample();
    //    }

    public HashSet<Vector2Int> UniqueVisibleCells(
        UnboundedGrid grid,
        float timeFrom,
        float timeTo,
        float step = float.MaxValue)
    {
        //TODO redo logic
        if (step == float.MaxValue) step = Step;
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        var simulation = new DynamicLevelSimulation(
            DynamicThreats, timeFrom, timeTo, step);

        while (!simulation.IsFinished)
        {
            foreach (var threat in simulation.Threats)
            {
                Bounds threatBounds = threat.GetBounds();
                var gridBound = new BoundsInt();
                gridBound.min = grid.WorldToCell(threatBounds.min);
                gridBound.max = grid.WorldToCell(threatBounds.max);
                for (int y = gridBound.min.y; y < gridBound.max.y; y++)
                {
                    for (int x = gridBound.min.x; x < gridBound.max.x; x++)
                    {
                        Vector2Int gridCoordinate = new Vector2Int(x, y);
                        if (cells.Contains(gridCoordinate)) continue;

                        Vector3 worldPos = grid.GetCellCenterWorld(
                            new Vector3Int(x, y));

                        if (threat.TestThreat(worldPos))
                            cells.Add(gridCoordinate);
                    }
                }
            }
            simulation.Progress();
        }

        return cells;
    }

    public Vector2 GetLerpepPositionInTime(Vector3 startT, Vector3 endT, float t)
    {
        float rel = Mathf.InverseLerp(startT.z, endT.z, t);
        return Vector2.Lerp(startT, endT, rel);
    }

    public virtual bool IsStaticCollision(Vector3 from, Vector3 to)
    {
        from = GlobalTransform.TransformPoint(from);
        to = GlobalTransform.TransformPoint(to);
        return Physics2D.Linecast(from, to, ObstacleLayerMask);
    }

    public virtual bool IsDynamicCollision(Vector3 from, Vector3 to)
    {
        var simulation = new DynamicLevelSimulation(DynamicThreats, from.z, to.z, Step);
        while (!simulation.IsFinished)
        {
            //Get 2d position
            //float passedTim e = simulation.CurrentTime - from.z;
            float rel = Mathf.InverseLerp(from.z, to.z, simulation.CurrentTime);

            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
            foreach (var threat in simulation.Threats)
                if (threat.TestThreat(positionInTime))
                    return true;
            simulation.Progress();
        }
        return false;
    }

    public bool IsColliding(Vector3 from, Vector3 to)
    {
        //No colliision with geomtry
        if (IsStaticCollision(from, to))
            return true;

        //No threats to any of the interpolatied possition
        //in the simulation
        if (IsDynamicCollision(from, to))
            return true;
        return false;
    }

    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo)
    {
        return IsColliding(
            new Vector3(from.x, from.y, timeFrom),
            new Vector3(to.x, to.y, timeTo));
    }

    public float GetMaxSimulationTime()
    {
        return _step * _iter;
    }

    private List<List<Vector3>> SolutionPaths;

    public NativeGrid<float> PredicableThreatHeatmap
        (UnboundedGrid grid)
    {
        NativeGrid<float> nativeGrid = new NativeGrid<float>(grid, Bounds);
        //Sets all initial values to 0
        nativeGrid.SetAll((x, y, g) => 0);

        var simulation = new DynamicLevelSimulation(
            DynamicThreats, 0, GetMaxSimulationTime(), Step);

        while (!simulation.IsFinished)
        {
            foreach (var threat in simulation.Threats)
            {
                Bounds threatBounds = threat.GetBounds();
                var gridBound = new BoundsInt();
                gridBound.min = grid.WorldToCell(threatBounds.min);
                gridBound.max = grid.WorldToCell(threatBounds.max);
                for (int y = gridBound.min.y; y < gridBound.max.y; y++)
                {
                    for (int x = gridBound.min.x; x < gridBound.max.x; x++)
                    {
                        Vector3 worldPos = grid.GetCellCenterWorld(
                            new Vector3Int(x, y));

                        if (threat.TestThreat(worldPos))
                        {
                            //Index of the same element in the native grid
                            Vector2Int nativeCoord = nativeGrid
                                .GetNativeCoord(new Vector2Int(x, y));
                            nativeGrid.Set(nativeCoord.x, nativeCoord.y,
                                nativeGrid.Get(nativeCoord.x, nativeCoord.y) + 1);
                        }
                    }
                }
            }
            simulation.Progress();
        }

        nativeGrid.SetAll((x, y, g) =>
        {
            return g.Get(x, y) / (float)Iterations;
        });

        return nativeGrid;
    }

    public object Clone()
    {
        var other = new DiscreteRecalculatingFutureLevel(Step, Iterations);
        other.BoundaryLayerMask = this.BoundaryLayerMask;
        other.ObstacleLayerMask = this.ObstacleLayerMask;
        other._iter = this._iter;
        other._step = this._step;
        return other;
    }

    public NativeGrid<float> GetHeatmap()
    {
        return this._clusteredThreats;
    }

    public Transform GetGlobalTransform()
    {
        return LevelManifest;
    }
}