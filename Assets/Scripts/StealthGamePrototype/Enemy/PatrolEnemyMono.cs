using Codice.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemyMono : MonoBehaviour, IPredictableThreat
{
    private Patrol Patrol;
    public DefaultEnemyProperties EnemyProperties;
    public List<Vector2> PathPoints;

    public FieldOfView FieldOfView;
    private Rigidbody2D _rigidBody2D;

    public float Time => Patrol.Time;

    public Patrol GetPatrol()
    { return Patrol; }

    // Start is called before the first frame update
    private void Awake()
    {
        _rigidBody2D = this.GetComponent<Rigidbody2D>();
        InitPatrol(PathPoints);
        //        if (BacktrackPatrolPath == null)
        //            if (Transforms.Count > 2)
        //                SetPatrolPath(Transforms.Select(x => (Vector2)x.position).ToList());
    }

    public void InitPatrol(List<Vector2> points)
    {
        PathPoints = points;
        Patrol = new Patrol(EnemyProperties, points);
    }

    private void FixedUpdate()
    {
        if (Patrol == null) return;
        this.TimeMove(UnityEngine.Time.fixedDeltaTime);
        _rigidBody2D.position =
            GetTransform().Position;
        transform.rotation =
            Quaternion.Euler(0, 0, Helpers.GetAngleFromVectorFloat(GetTransform().Direction));
    }

    //    public void DrawAllSegmentes()
    //    {
    //        if (BacktrackPatrolPath == null) return;
    //        var pathCount = BacktrackPatrolPath.GetPath().Count;
    //        for (int i = 0; i <= pathCount - 1; i++)
    //        {
    //            var seg = BacktrackPatrolPath.GetSegment(i);
    //            Gizmos.DrawLine(seg.Item1, seg.Item2);
    //        }
    //    }

    //    public void OnDrawGizmosSelected()
    //    {
    //        Gizmos.color = Color.yellow;
    //        DrawAllSegmentes();
    //    }

    //    public float GetTimeToTraverse()
    //    {
    //        if (BacktrackPatrolPath is not null)
    //        {
    //            float length = BacktrackPatrolPath.GetTotalLength();
    //            return length * EnemyProperties.Speed;
    //        }
    //        return 0;
    //    }

    //Given a patrol path return the position and the direction facing the direction
    // of the current traversed segment

    public void TimeMove(float deltaTime)
    {
        Patrol.TimeMove(deltaTime);
    }

    public FutureTransform GetTransform()
    {
        if (Patrol == null) return new FutureTransform
        { Position = this.transform.position, Direction = Vector2.right };
        return Patrol.GetTransform();
    }

    public bool TestThreat(Vector2 collision)
    {
        if (Patrol == null) return false;
        return Patrol.TestThreat(collision);
    }

    public Bounds GetBounds()
    {
        if (Patrol == null) return new Bounds();
        return Patrol.GetBounds();
    }
}