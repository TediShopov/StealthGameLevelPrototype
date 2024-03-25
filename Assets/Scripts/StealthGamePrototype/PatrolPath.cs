using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.XPath;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

public struct FutureTransform
{
    public Vector2 Position;
    public Vector2 Direction;
}

public interface IFutureTransform
{
    public FutureTransform GetFutureTransform(float time);
}

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolPath : MonoBehaviour, IFutureTransform
{
    public DefaultEnemyProperties EnemyProperties;
    public List<Transform> Transforms = new List<Transform>();
    public BacktrackPatrolPath BacktrackPatrolPath;
    public bool Randomized = true;
    [HideInInspector] public Vector2 Velocity;
    public FieldOfView FieldOfView;
    private Rigidbody2D _rigidBody2D;

    // Start is called before the first frame update
    private void Awake()
    {
        _rigidBody2D = this.GetComponent<Rigidbody2D>();
        //        if (BacktrackPatrolPath == null)
        //            if (Transforms.Count > 2)
        //                SetPatrolPath(Transforms.Select(x => (Vector2)x.position).ToList());
    }

    public void SetPatrolPath(List<Vector2> points)
    {
        if (points.Count >= 2)
        {
            BacktrackPatrolPath = new BacktrackPatrolPath(points, 0);
        }
        else if (points.Count == 1)
        {
            this.transform.position = points[0];
        }
    }

    private void FixedUpdate()
    {
        if (BacktrackPatrolPath == null) { return; }
        float travelDistance = EnemyProperties.Speed * Time.fixedDeltaTime;
        BacktrackPatrolPath.MoveAlong(travelDistance);
        FutureTransform futureTransform = GetPathOrientedTransform(BacktrackPatrolPath);
        _rigidBody2D.position = futureTransform.Position;
        LookAtPosition(futureTransform.Direction);
    }

    public void LookAtPosition(Vector3 lookAt)
    {
        // the second argument, upwards, defaults to Vector3.up
        //if (Positions.Count == 1) return;
        Quaternion rotation = Quaternion.Euler(0, 0, Helpers.GetAngleFromVectorFloat(lookAt));
        transform.rotation = rotation;
    }

    public void DrawAllSegmentes()
    {
        if (BacktrackPatrolPath == null) return;
        var pathCount = BacktrackPatrolPath.GetPath().Count;
        for (int i = 0; i <= pathCount - 1; i++)
        {
            var seg = BacktrackPatrolPath.GetSegment(i);
            Gizmos.DrawLine(seg.Item1, seg.Item2);
        }
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        DrawAllSegmentes();
    }

    public float GetTimeToTraverse()
    {
        if (BacktrackPatrolPath is not null)
        {
            float length = BacktrackPatrolPath.GetTotalLength();
            return length * EnemyProperties.Speed;
        }
        return 0;
    }

    //Given a patrol path return the position and the direction facing the direction
    // of the current traversed segment
    public static FutureTransform GetPathOrientedTransform(BacktrackPatrolPath path)
    {
        FutureTransform futureTransform = new FutureTransform();
        futureTransform.Position = path.GetCurrent();
        var seg = path.GetSegment();
        futureTransform.Direction = (seg.Item2 - seg.Item1).normalized;
        return futureTransform;
    }

    public Tuple<Vector2, Vector2> CalculateFuturePosition(float time)
    {
        FutureTransform futureTransform = GetFutureTransform(time);
        return new Tuple<Vector2, Vector2>(futureTransform.Position, futureTransform.Direction);
    }

    public FutureTransform GetFutureTransform(float time)
    {
        Profiler.BeginSample("Future Transforms");
        if (BacktrackPatrolPath == null)
            return new FutureTransform()
            {
                Position = this.transform.position,
                Direction = this.transform.forward
            };
        float distanceCovered = EnemyProperties.Speed * time;
        BacktrackPatrolPath pathCopy = new BacktrackPatrolPath(BacktrackPatrolPath);
        pathCopy.MoveAlong(distanceCovered);
        Profiler.EndSample();
        return GetPathOrientedTransform(pathCopy);
    }
}