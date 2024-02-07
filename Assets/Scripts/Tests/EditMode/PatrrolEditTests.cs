using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.TextCore.Text;
using UnityEngine;
using UnityEngine.TestTools;

public struct PatrolSegment 
{
    
}
public class BacktrackPatrolPath 
{
    //Operates as an index, but is continous
    //E.g 1.6f would represent 60% of segments from element 1 to elements 2 
    public float relPathPostion = 0;
    public bool traverseForward = true;
    public List<Vector2> Path;
    public BacktrackPatrolPath(List<Vector2> path, float startPos =0)
    {
        if(path == null) throw new ArgumentNullException("Path cannot be null");
        if(path.Count <=1 ) throw new ArgumentException("Pats need to be defined by at least 2 points");
        Path= path;
        relPathPostion = startPos;
    }

    public int GetNextIndex(int current) 
    {
        int next = traverseForward ? current + 1 : current - 1;
        if(next>=Path.Count || next<0) 
        {
            traverseForward = !traverseForward;
            return GetNextIndex(current);
        }
        return next;
    } 
    private Tuple<int, int> GetSegmentIndices(float rel)
    {
        int from = -1;
        int to = -1;
        if (rel <0 || rel>Path.Count)
        {
            return null;
        }

        var tempTraverse = traverseForward;
        if (rel % 1 == 0) 
        {
            int index = traverseForward ?  Mathf.FloorToInt(rel) : Mathf.CeilToInt(rel);
            from = index;
            to = GetNextIndex(index);
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

    public float GetSegmentLength(float rel) 
    {
       var seg = GetSegment(rel);
        return Vector2.Distance(seg.Item1, seg.Item2);
    }
    public Vector2 GetCurrent()
    {
        Tuple<Vector2,Vector2> segment= GetSegment(relPathPostion);
        float segmentCompletion = relPathPostion % 1f;
        return Vector2.Lerp(segment.Item1, segment.Item2, segmentCompletion);
    }
    public void MoveAlong(float displacement) 
    {
        if (displacement<0.0f)
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
            if (currentSegment.Item2 >= Path.Count-1 || currentSegment.Item2 <= 0)
                traverseForward = !traverseForward;
            //Update current segment and distance to segmente end
            currentSegment = GetSegmentIndices(relPathPostion);
            distanceToSegmentEnd = Vector2.Distance(GetCurrent(), Path[currentSegment.Item2]);
        }
        float f = displacement / Vector2.Distance(Path[currentSegment.Item1], Path[currentSegment.Item2]);
        relPathPostion += f;
    }
    

}
public class PatrrolEditTests
{
    //Indexing test 
    [Test]
    public void PatrolPathGetSemgent_CannotChangeDirectionOfPatrol()
    {
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(2,0),
                new Vector2(3,0),
            };
        
        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        backtrackPatrolPath.traverseForward = false;
        Tuple<Vector2, Vector2> segmentOnForwrd = backtrackPatrolPath.GetSegment(0);
        Assert.AreEqual(segmentOnForwrd.Item1, path[0]);
        Assert.AreEqual(segmentOnForwrd.Item2, path[1]);
        Assert.AreEqual(backtrackPatrolPath.traverseForward, false);

        

    }
    [Test]
    public void PatrolPathGetSemgent_Correct()
    {
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(2,0),
                new Vector2(3,0),
            };
        
        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        Tuple<Vector2, Vector2> segmentOnForwrd = backtrackPatrolPath.GetSegment(0);
        Assert.AreEqual(segmentOnForwrd.Item1, path[0]);
        Assert.AreEqual(segmentOnForwrd.Item2, path[1]);
        

        Tuple<Vector2, Vector2> segmentOnForwrdDifferentIndex = backtrackPatrolPath.GetSegment(1);
        Assert.AreEqual(segmentOnForwrdDifferentIndex.Item1, path[1]);
        Assert.AreEqual(segmentOnForwrdDifferentIndex.Item2, path[2]);



        backtrackPatrolPath.traverseForward = false;
        Tuple<Vector2, Vector2> segmentOnBackwards = backtrackPatrolPath.GetSegment(1);
        Assert.AreEqual(segmentOnBackwards.Item1, path[1]);
        Assert.AreEqual(segmentOnBackwards.Item2, path[0]);
        // Use the Assert class to test conditions
    }
    // A Test behaves as an ordinary method
    [Test]
    public void PatrolPathLessThanNeccesaryPoints_ThrowsExceptions()
    {
        Assert.Throws<ArgumentException>(
            delegate {
                new BacktrackPatrolPath(new List<Vector2>());
            });
        Assert.Throws<ArgumentException>(
            delegate {
                new BacktrackPatrolPath(new List<Vector2>() { new Vector2(0,0)});
            });
    }
    [Test]
    public void PatrolPathRegular_ReturnsCorrectNextPosition()
    {
        // Use the Assert class to test conditions
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(2,0),
                new Vector2(3,0),
            };
        
        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        backtrackPatrolPath.MoveAlong(1f);
        Assert.AreEqual(path[1], backtrackPatrolPath.GetCurrent());
        backtrackPatrolPath.MoveAlong(2f);
        Assert.AreEqual(path[3], backtrackPatrolPath.GetCurrent());
    }
    [Test]
    public void BacktrackingPatrolPath_WrapsAroundAtEnd()
    {
        // Use the Assert class to test conditions
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(2,0),
                new Vector2(3,0),
            };
        
        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        backtrackPatrolPath.MoveAlong(4f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[2]);
        backtrackPatrolPath.MoveAlong(4f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[2]);
        backtrackPatrolPath.MoveAlong(4f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[0]);

        // Segment A -0- B -1- C -2- D 
        //Given path [0,1,2,3]
        //Moving 3 indices then moving 1 should return elements 2

        //Given path [0,1,2] current on 1 reversed true 
        //Move along 2 return 1 Move along 2 return 1 againg


        //Double Wrap
        //Given path [0,1,2,3]
        //Moving 8 indices then moving 1 should return elements 2
    }
}
