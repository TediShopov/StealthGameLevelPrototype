using System;
using System.Collections;
using System.Collections.Generic;
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

    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo)
    {
        bool hitStatic = Physics2D.Linecast(from, to, ObstacleLayerMask);
        if (hitStatic)
            return true;

        int timeSteps = Mathf.FloorToInt((timeTo - timeFrom) / (float)Step);
        foreach (var p in EnemyPatrolPaths)
        {
            BacktrackPatrolPath patrol = new BacktrackPatrolPath(p.BacktrackPatrolPath);
            float time = timeFrom;
            patrol.MoveAlong(timeFrom);
            for (int i = 0; i <= timeSteps; i++)
            {
                time += Step;
                time = Mathf.Clamp(time, timeFrom, timeTo);
                //Small Inaccuracy
                patrol.MoveAlong(Step);

                float rel = Mathf.InverseLerp(timeFrom, timeTo, time);
                Vector2 positionInTime = Vector2.Lerp(from, to, rel);
                FutureTransform ft = PatrolPath.GetPathOrientedTransform(patrol);
                bool hitEnemyCone = FieldOfView.TestCollision(positionInTime, ft,
                    p.EnemyProperties.FOV, p.EnemyProperties.ViewDistance, ObstacleLayerMask);
                if (hitEnemyCone)
                {
                    return true;
                }
            }
        }
        return false;
    }
}