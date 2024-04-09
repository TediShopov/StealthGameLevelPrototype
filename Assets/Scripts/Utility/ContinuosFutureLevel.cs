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

public class DynamicLevelSimulation
{
    private float From;
    private float To;
    private float TimeStep;
    public IEnumerable<IPredictableThreat> Threats;

    public int StepCount =>
      Mathf.FloorToInt((To - From) / (float)TimeStep);

    public float CurrentTime = 0;
    public bool IsFinished => CurrentTime > To;

    public DynamicLevelSimulation(
        IEnumerable<IPredictableThreat> threats,
        float from,
        float to,
        float timeStep)
    {
        Threats = threats;
        foreach (var t in Threats)
            t.Reset();
        From = from;
        To = to;
        TimeStep = timeStep;
        foreach (var threat in Threats)
            threat.TimeMove(from);
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

[ExecuteAlways]
[RequireComponent(typeof(Grid))]
public class ContinuosFutureLevel : MonoBehaviour, IFutureLevel, IPrototypable<ContinuosFutureLevel>

{
    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    private Collider2D _boundary;

    //public PatrolPath[] EnemyPatrolPaths;
    public IPredictableThreat[] DynamicThreats;

    [SerializeField] private float _step = 0.2f;
    [SerializeField] private float _iter = 50;

    public float Step => _step;

    public float Iterations => _iter;
    public bool EnableSetLevel = true;
    public bool EnableDiscreteTimes = true;
    public float SetTime = 0.0f;

    public DynamicLevelSimulation GetFullSimulation()
    {
        return new DynamicLevelSimulation(DynamicThreats, 0, GetMaxSimulationTime(), Step);
    }

    public Bounds GetBounds()
    {
        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        var _boundary = Physics2D.OverlapPoint(this.transform.position, BoundaryLayerMask);
        if (_boundary != null)
        {
            min = _boundary.bounds.min;
            max = _boundary.bounds.max;

            min.z = 0;
            max.z = Iterations * Step;
        }
        Bounds bounds = new Bounds();
        bounds.min = min;
        bounds.max = max;
        return bounds;
    }

    // Start is called before the first frame update
    public void Init()
    {
        Profiler.BeginSample("Continuos Representation");
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        SolutionPaths = new List<List<Vector3>>();
        DynamicThreats = level.GetComponentsInChildren<IPredictableThreat>();
        //EnemyPatrolPaths = GetEnemyPatrolPaths();
        //enemyPaths[i].BacktrackPatrolPath = new BacktrackPatrolPath(paths[i]);
        StartCoroutine(RefreshLevelSolutionObjects());
        Profiler.EndSample();
    }

    public HashSet<Vector2Int> UniqueVisibleCells(
        Grid grid,
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

    public bool IsStaticCollision(Vector3 from, Vector3 to)
    {
        return Physics2D.Linecast(from, to, ObstacleLayerMask);
    }

    public bool IsDynamicCollision(Vector3 from, Vector3 to)
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

    private IEnumerator RefreshLevelSolutionObjects()
    {
        while (true)
        {
            var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
            var rrts = level.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();

            SolutionPaths = rrts.Select(x => x.RRT)
               .Where(x => x.Succeeded())
               .Select(x => x.ReconstructPathToSolution())
               .ToList();

            yield return new WaitForSecondsRealtime(2.0f);
        }
    }

    private Vector2 GetPosition(List<Vector3> solutionPath, float time)
    {
        if (time > solutionPath[solutionPath.Count - 1].z)
            return Vector2.zero;

        int index = 0;
        while (index <= solutionPath.Count - 1)
        {
            //If current time is smaller than the time of the path in the next node
            if (time < solutionPath[index].z)
            {
                if (index == 0) return Vector2.zero;

                //Position is on this segment
                float relTime = Mathf.InverseLerp(solutionPath[index - 1].z, solutionPath[index].z, time);
                Vector2 pos = Vector2.Lerp(solutionPath[index - 1], solutionPath[index], relTime);
                return pos;
            }
            index++;
        }
        return Vector2.zero;
    }

    public void Update()
    {
        if (DynamicThreats == null) return;
        foreach (var threa in DynamicThreats)
        {
            threa.Reset();
            if (EnableDiscreteTimes)
            {
                float discreteTime = Step * Mathf.CeilToInt(SetTime / Step);
                threa.TimeMove(discreteTime);
            }
            else
            {
                threa.TimeMove(SetTime);
            }
        }
    }

    public void OnDrawGizmosSelected()
    {
        if (EnableSetLevel == false) return;
        if (SolutionPaths == null) return;
        foreach (var path in SolutionPaths)
        {
            Vector2 position = GetPosition(path, SetTime);
            if (EnableDiscreteTimes)
            {
                float discreteTime = Step * Mathf.CeilToInt(SetTime / Step);
                position = GetPosition(path, discreteTime);
            }
            Gizmos.DrawSphere(position, 0.1f);
        }
    }

    public ContinuosFutureLevel PrototypeComponent(GameObject to)
    {
        var other = to.AddComponent<ContinuosFutureLevel>();
        other.BoundaryLayerMask = this.BoundaryLayerMask;
        other.ObstacleLayerMask = this.ObstacleLayerMask;
        other._iter = this._iter;
        other._step = this._step;
        return other;
    }
}