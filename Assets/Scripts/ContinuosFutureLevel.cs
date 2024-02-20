using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

public class ContinuosFutureLevel : MonoBehaviour, IFutureLevel
{
    public LayerMask ObstacleLayerMask;
    public LayerMask BoundaryLayerMask;
    private Collider2D _boundary;
    public PatrolPath[] EnemyPatrolPaths;
    [SerializeField] private float _step = 0.2f;
    [SerializeField] private float _iter = 50;

    public float Step => _step;

    public float Iterations => _iter;

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

    public PatrolPath[] GetEnemyPatrolPaths()
    {
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        return level.GetComponentsInChildren<PatrolPath>();
    }

    // Start is called before the first frame update
    private void Start()
    {
    }

    public void Init()
    {
        Profiler.BeginSample("Continuos Representation");
        EnemyPatrolPaths = GetEnemyPatrolPaths();
        Profiler.EndSample();
    }

    public List<Vector2> AreNotCollidingDynamicDiscrete(List<Vector2> positions, float timeFrom, float timeTo, float step = float.MaxValue)
    {
        if (step == float.MaxValue) step = Step;
        //        List<Vector2> _uncollidedPositions = new List<Vector2>(positions);
        //        _uncollidedPositions = _uncollidedPositions.Where(x =>
        //             !Physics2D.OverlapBox(x, area, ObstacleLayerMask)
        //        ).ToList();

        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)step);
        foreach (var p in EnemyPatrolPaths)
        {
            BacktrackPatrolPath patrol = null;
            if (p.BacktrackPatrolPath != null)
            {
                patrol = new BacktrackPatrolPath(p.BacktrackPatrolPath);
                patrol.MoveAlong(timeFrom * p.EnemyProperties.Speed);
            }
            float time = timeFrom;
            for (int i = 0; i <= timeSteps; i++)
            {
                time += Step;
                time = Mathf.Clamp(time, timeFrom, timeTo);
                //Small Inaccuracy
                if (patrol != null)
                    patrol.MoveAlong(Step * p.EnemyProperties.Speed);
                positions = positions.Where(x =>
                {
                    FutureTransform ft;
                    if (patrol is not null)
                        ft = PatrolPath.GetPathOrientedTransform(patrol);
                    else
                        ft = p.GetFutureTransform(0);
                    return !FieldOfView.TestCollision(x, ft,
                        p.EnemyProperties.FOV, p.EnemyProperties.ViewDistance, ObstacleLayerMask);
                }).ToList();
            }
        }
        return positions;
    }

    private bool IsCollidingWithStationeryEnemy(Vector2 from, Vector2 to, float timeFrom, float timeTo, PatrolPath path)
    {
        if (path == null) return false;
        if (path.BacktrackPatrolPath is not null) throw new ArgumentException();

        FutureTransform ft = path.GetFutureTransform(0);
        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
        for (int i = 0; i <= timeSteps; i++)
        {
            float time = timeFrom;
            time += Step;
            time = Mathf.Clamp(time, timeFrom, timeTo);
            float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
            bool hitEnemyCone = FieldOfView.TestCollision(positionInTime, ft,
                    path.EnemyProperties.FOV, path.EnemyProperties.ViewDistance, ObstacleLayerMask);
            if (hitEnemyCone)
                return true;
        }
        return false;
    }

    private bool IsCollidingWithPatrol(Vector2 from, Vector2 to, float timeFrom, float timeTo, PatrolPath path)
    {
        BacktrackPatrolPath patrol = new BacktrackPatrolPath(path.BacktrackPatrolPath);
        float time = timeFrom;
        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
        patrol.MoveAlong(timeFrom * path.EnemyProperties.Speed);
        for (int i = 0; i <= timeSteps; i++)
        {
            time += Step;
            time = Mathf.Clamp(time, timeFrom, timeTo);
            //Small Inaccuracy
            patrol.MoveAlong(Step * path.EnemyProperties.Speed);

            float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
            FutureTransform ft = PatrolPath.GetPathOrientedTransform(patrol);
            bool hitEnemyCone = FieldOfView.TestCollision(positionInTime, ft,
                    path.EnemyProperties.FOV, path.EnemyProperties.ViewDistance, ObstacleLayerMask);
            if (hitEnemyCone)
                return true;
        }
        return false;
    }

    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo)
    {
        bool hitStatic = Physics2D.Linecast(from, to, ObstacleLayerMask);
        if (hitStatic)
            return true;

        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
        foreach (var p in EnemyPatrolPaths)
        {
            if (p.BacktrackPatrolPath == null)
            {
                if (IsCollidingWithStationeryEnemy(from, to, timeFrom, timeTo, p))
                    return true;
            }
            else
            {
                if (IsCollidingWithPatrol(from, to, timeFrom, timeTo, p))
                    return true;
            }
        }
        return false;
    }
}