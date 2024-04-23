using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BacktrackPatrolPath
{
    //Operates as an index, but is continous
    //E.g 1.6f would represent 60% of segments from element 1 to elements 2
    private float relPathPostion = 0;

    public bool TraverseForward = true;
    private List<Vector2> Path;

    public List<Vector2> GetPath()
    { return new List<Vector2>(Path); }

    public BacktrackPatrolPath(List<Vector2> path, float startPos = 0)
    {
        if (path == null) throw new ArgumentNullException("Path cannot be null");
        if (path.Count <= 1) throw new ArgumentException("Pats need to be defined by at least 2 points");
        Path = path;
        relPathPostion = startPos;
    }

    public void Reset()
    {
        relPathPostion = 0;
        TraverseForward = true;
    }

    public BacktrackPatrolPath(BacktrackPatrolPath other)
    {
        this.Copy(other);
    }

    public void Copy(BacktrackPatrolPath other)
    {
        this.relPathPostion = other.relPathPostion;
        this.TraverseForward = other.TraverseForward;
        this.Path = new List<Vector2>(other.Path);
    }

    public BacktrackPatrolPath Copy()
    {
        return new BacktrackPatrolPath(this);
    }

    public int GetNextIndex(int current)
    {
        int next = TraverseForward ? current + 1 : current - 1;
        if (next >= Path.Count || next < 0)
        {
            next = TraverseForward ? current - 1 : current + 1;
        }
        return next;
    }

    private int GetIndex(float rel)
    {
        return TraverseForward ? Mathf.FloorToInt(rel) : Mathf.CeilToInt(rel);
    }

    private Tuple<int, int> GetSegmentIndices(float rel)
    {
        int from = -1;
        int to = -1;
        if (rel < 0 || rel >= Path.Count)
        {
            return null;
        }

        var tempTraverse = TraverseForward;
        if (rel % 1 == 0)
        {
            from = GetIndex(rel);
            to = GetNextIndex(from);
        }
        else
        {
            from = GetIndex(TraverseForward ? Mathf.FloorToInt(rel) : Mathf.CeilToInt(rel));
            //to = GetNextIndex(TraverseForward ? Mathf.CeilToInt(rel) : Mathf.FloorToInt(rel));
            to = GetNextIndex(from);
        }
        TraverseForward = tempTraverse;
        return new Tuple<int, int>(from, to);
    }

    public Tuple<Vector2, Vector2> GetSegment(float rel)
    {
        Tuple<int, int> indices = GetSegmentIndices(rel);
        try
        {
            if (indices != null)
                return new Tuple<Vector2, Vector2>(Path[indices.Item1], Path[indices.Item2]);
            else
                return null;
        }
        catch (Exception)
        {
            throw;
        }
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
        if (segment == null)
        {
            int b = 3;
        }
        float segmentCompletion = Math.Abs(relPathPostion - GetIndex(relPathPostion));
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
                TraverseForward = !TraverseForward;
            //Update current segment and distance to segmente end
            currentSegment = GetSegmentIndices(relPathPostion);
            distanceToSegmentEnd = Vector2.Distance(GetCurrent(), Path[currentSegment.Item2]);
        }
        float f = displacement / Vector2.Distance(Path[currentSegment.Item1], Path[currentSegment.Item2]);
        relPathPostion += TraverseForward ? f : -f;
    }

    public float GetTotalLength()
    {
        float total = 0;
        for (int i = 0; i < Path.Count - 1; i++)
            total += Vector2.Distance(Path[i], Path[i + 1]);
        return total;
    }
}