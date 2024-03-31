using JetBrains.Annotations;
using PlasticPipe.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

public class ContinuosFutureLevel : MonoBehaviour, IFutureLevel
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

    //    public PatrolPath[] GetEnemyPatrolPaths()
    //    {
    //        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
    //        return level.GetComponentsInChildren<PatrolPath>();
    //    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    public void Init()
    {
        Profiler.BeginSample("Continuos Representation");
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        DynamicThreats = level.GetComponentsInChildren<IPredictableThreat>();
        //EnemyPatrolPaths = GetEnemyPatrolPaths();
        //enemyPaths[i].BacktrackPatrolPath = new BacktrackPatrolPath(paths[i]);
        StartCoroutine(RefreshLevelSolutionObjects());
        Profiler.EndSample();
    }

    //    public List<Vector2> AreNotCollidingDynamicDiscrete(List<Vector2> positions, float timeFrom, float timeTo, float step = float.MaxValue)
    //    {
    //        return new List<Vector2>();
    //        //        if (step == float.MaxValue) step = Step;
    //        //        //        List<Vector2> _uncollidedPositions = new List<Vector2>(positions);
    //        //        //        _uncollidedPositions = _uncollidedPositions.Where(x =>
    //        //        //             !Physics2D.OverlapBox(x, area, ObstacleLayerMask)
    //        //        //        ).ToList();
    //        //
    //        //        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)step);
    //        //        foreach (var p in EnemyPatrolPaths)
    //        //        {
    //        //            BacktrackPatrolPath patrol = null;
    //        //            if (p.BacktrackPatrolPath != null)
    //        //            {
    //        //                patrol = new BacktrackPatrolPath(p.BacktrackPatrolPath);
    //        //                patrol.MoveAlong(timeFrom * p.EnemyProperties.Speed);
    //        //            }
    //        //            float time = timeFrom;
    //        //            for (int i = 0; i <= timeSteps; i++)
    //        //            {
    //        //                time += Step;
    //        //                time = Mathf.Clamp(time, timeFrom, timeTo);
    //        //                //Small Inaccuracy
    //        //                if (patrol != null)
    //        //                    patrol.MoveAlong(Step * p.EnemyProperties.Speed);
    //        //                positions = positions.Where(x =>
    //        //                {
    //        //                    FutureTransform ft;
    //        //                    if (patrol is not null)
    //        //                        ft = PatrolPath.GetPathOrientedTransform(patrol);
    //        //                    else
    //        //                        ft = p.GetFutureTransform(0);
    //        //                    return !FieldOfView.TestCollision(x, ft,
    //        //                        p.EnemyProperties.FOV, p.EnemyProperties.ViewDistance, ObstacleLayerMask);
    //        //                }).ToList();
    //        //            }
    //        //        }
    //        //        return positions;
    //    }

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

    //    private bool IsCollidingWithStationeryEnemy(Vector2 from, Vector2 to, float timeFrom, float timeTo, PatrolPath path)
    //    {
    //        if (path == null) return false;
    //        if (path.BacktrackPatrolPath is not null) throw new ArgumentException();
    //
    //        FutureTransform ft = path.GetFutureTransform(0);
    //        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
    //        for (int i = 0; i <= timeSteps; i++)
    //        {
    //            float time = timeFrom;
    //}
    //            time += Step;
    //            time = Mathf.Clamp(time, timeFrom, timeTo);
    //            float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
    //            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
    //            bool hitEnemyCone = FieldOfView.TestCollision(positionInTime, ft,
    //                    path.EnemyProperties.FOV, path.EnemyProperties.ViewDistance, ObstacleLayerMask);
    //            if (hitEnemyCone)
    //                return true;
    //        }
    //        return false;
    //    }
    //
    //    private bool IsCollidingWithPatrol(Vector2 from, Vector2 to, float timeFrom, float timeTo, PatrolPath path)
    //    {
    //        BacktrackPatrolPath patrol = new BacktrackPatrolPath(path.BacktrackPatrolPath);
    //        float time = timeFrom;
    //        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
    //        patrol.MoveAlong(timeFrom * path.EnemyProperties.Speed);
    //        for (int i = 0; i <= timeSteps; i++)
    //        {
    //            time += Step;
    //            time = Mathf.Clamp(time, timeFrom, timeTo);
    //            //Small Inaccuracy
    //            patrol.MoveAlong(Step * path.EnemyProperties.Speed);
    //
    //            float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
    //            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
    //            FutureTransform ft = PatrolPath.GetPathOrientedTransform(patrol);
    //            bool hitEnemyCone = FieldOfView.TestCollision(positionInTime, ft,
    //                    path.EnemyProperties.FOV, path.EnemyProperties.ViewDistance, ObstacleLayerMask);
    //            if (hitEnemyCone)
    //                return true;
    //        }
    //        return false;
    //    }

    //    //Given a 3d position with the z coordinate interpereted as time
    //    //Return a list of interpolated position on that line
    //    public static List<Vector3> GetPathSegmentsInTime(
    //        Vector3 from, Vector3 to, int step)
    //    {
    //        float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
    //        Vector2 positionInTime = Vector2.Lerp(from, to, rel);
    //
    //    }

    public Vector2 GetLerpepPositionInTime(Vector3 startT, Vector3 endT, float t)
    {
        float rel = Mathf.InverseLerp(startT.z, endT.z, t);
        return Vector2.Lerp(startT, endT, rel);
    }

    public bool IsColliding(Vector3 from, Vector3 to)
    {
        if (Physics2D.Linecast(from, to, ObstacleLayerMask))
            return true;

        var simulation = new DynamicLevelSimulation(DynamicThreats, from.z, to.z, Step);
        while (!simulation.IsFinished)
        {
            //Get 2d position
            //float passedTime = simulation.CurrentTime - from.z;
            float rel = Mathf.InverseLerp(from.z, to.z, simulation.CurrentTime);
            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
            foreach (var threat in simulation.Threats)
                if (threat.TestThreat(positionInTime))
                    return true;
            simulation.Progress();
        }
        //No threats to any of the interpolatied possition
        //in the simulation
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
}