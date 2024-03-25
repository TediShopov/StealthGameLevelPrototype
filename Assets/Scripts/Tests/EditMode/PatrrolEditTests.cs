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
        backtrackPatrolPath.TraverseForward = false;
        Tuple<Vector2, Vector2> segmentOnForwrd = backtrackPatrolPath.GetSegment(0);
        Assert.AreEqual(segmentOnForwrd.Item1, path[0]);
        Assert.AreEqual(segmentOnForwrd.Item2, path[1]);
        Assert.AreEqual(backtrackPatrolPath.TraverseForward, false);
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

        backtrackPatrolPath.IsMovingForward = false;
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
            delegate
            {
                new BacktrackPatrolPath(new List<Vector2>());
            });
        Assert.Throws<ArgumentException>(
            delegate
            {
                new BacktrackPatrolPath(new List<Vector2>() { new Vector2(0, 0) });
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
    }

    [Test]
    public void BacktrackingUnevenPatrolPath_WrapsAroundAtEnd()
    {
        // Use the Assert class to test conditions
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(12f,0),
                new Vector2(15.5213f,0),
                new Vector2(17.0f,0),
            };

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        backtrackPatrolPath.MoveAlong(17f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[3]);
        backtrackPatrolPath.MoveAlong(17f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[0]);
        backtrackPatrolPath.MoveAlong(17f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[3]);
        backtrackPatrolPath.MoveAlong(17f);
        backtrackPatrolPath.MoveAlong(17f);
        Assert.AreEqual(backtrackPatrolPath.GetCurrent(), path[3]);
        backtrackPatrolPath.MoveAlong(0.1f);
        Assert.IsFalse(backtrackPatrolPath.TraverseForward);
    }

    [Test]
    public void BacktrackingManySmallAdditions_IsCorrect()
    {
        // Use the Assert class to test conditions
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(12f,0),
                new Vector2(15.5213f,0),
                new Vector2(17.0f,0),
            };

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        int iter = 0;
        try
        {
            for (int i = 0; i < 200; i++)
            {
                iter = i;
                backtrackPatrolPath.MoveAlong(0.1f);
            }
        }
        catch (Exception)
        {
            Debug.Log($"Arguement out of range at {iter}");
            throw;
        }
    }

    [Test]
    public void IndexingTes()
    {
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
            };

        BacktrackPatrolPath backtrackPatrolPathInteger = new BacktrackPatrolPath(path, 1);
        Assert.AreEqual(new Vector2(1.0f, 0), backtrackPatrolPathInteger.GetCurrent());
        backtrackPatrolPathInteger.TraverseForward = false;
        Assert.AreEqual(new Vector2(1.0f, 0), backtrackPatrolPathInteger.GetCurrent());

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path, 0.8f);
        Assert.AreEqual(new Vector2(0.8f, 0), backtrackPatrolPath.GetCurrent());
        backtrackPatrolPath.TraverseForward = false;
        Assert.AreEqual(new Vector2(0.8f, 0), backtrackPatrolPath.GetCurrent());
    }

    [Test]
    public void BacktrackingReverseFraction()
    {
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
            };

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        backtrackPatrolPath.MoveAlong(1.0f);
        Assert.AreEqual(new Vector2(1.0f, 0), backtrackPatrolPath.GetCurrent());
        Assert.IsFalse(backtrackPatrolPath.TraverseForward);
        backtrackPatrolPath.MoveAlong(0.2f);
        Assert.AreEqual(new Vector2(0.8f, 0), backtrackPatrolPath.GetCurrent());
    }
}