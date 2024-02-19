using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.XPath;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BacktrackPatrolPath
{
    //Operates as an index, but is continous
    //E.g 1.6f would represent 60% of segments from element 1 to elements 2
    private float relPathPostion = 0;

    public bool traverseForward = true;
    public List<Vector2> Path;

    public BacktrackPatrolPath(List<Vector2> path, float startPos = 0)
    {
        if (path == null) throw new ArgumentNullException("Path cannot be null");
        if (path.Count <= 1) throw new ArgumentException("Pats need to be defined by at least 2 points");
        Path = path;
        relPathPostion = startPos;
    }

    public BacktrackPatrolPath(BacktrackPatrolPath other)
    {
        this.Copy(other);
    }

    public void Copy(BacktrackPatrolPath other)
    {
        this.relPathPostion = other.relPathPostion;
        this.traverseForward = other.traverseForward;
        this.Path = new List<Vector2>(other.Path);
    }

    public int GetNextIndex(int current)
    {
        int next = traverseForward ? current + 1 : current - 1;
        if (next >= Path.Count || next < 0)
        {
            next = traverseForward ? current - 1 : current + 1;
        }
        return next;
    }

    private int GetIndex(float rel)
    {
        return traverseForward ? Mathf.FloorToInt(rel) : Mathf.CeilToInt(rel);
    }

    private Tuple<int, int> GetSegmentIndices(float rel)
    {
        int from = -1;
        int to = -1;
        if (rel < 0 || rel >= Path.Count)
        {
            return null;
        }

        var tempTraverse = traverseForward;
        if (rel % 1 == 0)
        {
            from = GetIndex(rel);
            to = GetNextIndex(from);
        }
        else
        {
            from = traverseForward ? Mathf.FloorToInt(rel) : Mathf.CeilToInt(rel);
            to = traverseForward ? Mathf.CeilToInt(rel) : Mathf.FloorToInt(rel);
        }
        traverseForward = tempTraverse;
        return new Tuple<int, int>(from, to);
    }

    public Tuple<Vector2, Vector2> GetSegment(float rel)
    {
        Tuple<int, int> indices = GetSegmentIndices(rel);
        if (indices != null)
            return new Tuple<Vector2, Vector2>(Path[indices.Item1], Path[indices.Item2]);
        else
            return null;
    }

    public Tuple<Vector2, Vector2> GetSegment()
    {
        Tuple<int, int> indices = GetSegmentIndices(relPathPostion);
        if (indices != null)
            return new Tuple<Vector2, Vector2>(Path[indices.Item1], Path[indices.Item2]);
        else
            return null;
    }

    public float GetSegmentLength(float rel)
    {
        var seg = GetSegment(rel);
        return Vector2.Distance(seg.Item1, seg.Item2);
    }

    public Vector2 GetCurrent()
    {
        Tuple<Vector2, Vector2> segment = GetSegment(relPathPostion);
        // 0--0.2---0.8--1
        // forward lerp(0,1,0.2)
        //backward lerp(1,0,0.2) --> (0,1,0.2)
        //backward lerp(1,0,1) --> (0,1,1)
        float segmentCompletion = Math.Abs(relPathPostion - GetIndex(relPathPostion));

        //        if (traverseForward == false)
        //            return Vector2.Lerp(segment.Item2, segment.Item1, segmentCompletion);

        return Vector2.Lerp(segment.Item1, segment.Item2, segmentCompletion);
    }

    public void MoveAlong(float displacement)
    {
        if (displacement < 0.0f)
        {
            return;
        }

        Tuple<int, int> currentSegment = GetSegmentIndices(relPathPostion);
        float distanceToSegmentEnd = Vector2.Distance(GetCurrent(), Path[currentSegment.Item2]);
        while (displacement >= distanceToSegmentEnd)
        {
            //Travel to the end of the segment
            displacement -= distanceToSegmentEnd;
            relPathPostion = currentSegment.Item2;
            if (currentSegment.Item2 >= Path.Count - 1 || currentSegment.Item2 <= 0)
                traverseForward = !traverseForward;
            //Update current segment and distance to segmente end
            currentSegment = GetSegmentIndices(relPathPostion);
            distanceToSegmentEnd = Vector2.Distance(GetCurrent(), Path[currentSegment.Item2]);
        }
        float f = displacement / Vector2.Distance(Path[currentSegment.Item1], Path[currentSegment.Item2]);
        relPathPostion += traverseForward ? f : -f;
    }

    public float GetTotalLength()
    {
        float total = 0;
        for (int i = 0; i < Path.Count - 1; i++)
            total += Vector2.Distance(Path[i], Path[i + 1]);
        return total;
    }
}

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
        BacktrackPatrolPath = new BacktrackPatrolPath(points, 0);
    }

    private void FixedUpdate()
    {
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
        for (int i = 0; i <= BacktrackPatrolPath.Path.Count - 1; i++)
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
        if (BacktrackPatrolPath == null)
            return new FutureTransform()
            {
                Position = this.transform.position,
                Direction = this.transform.forward
            };
        float distanceCovered = EnemyProperties.Speed * time;
        BacktrackPatrolPath pathCopy = new BacktrackPatrolPath(BacktrackPatrolPath);
        pathCopy.MoveAlong(distanceCovered);
        return GetPathOrientedTransform(pathCopy);
    }
}