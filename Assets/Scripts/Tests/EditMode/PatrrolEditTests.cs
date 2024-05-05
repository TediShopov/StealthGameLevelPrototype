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
    [Test]
    public void BacktrackPatrolLandingExactlyOnEnd()
    {
        var e = new DefaultEnemyProperties();
        e.Speed = 1.5f;
        var path =
            new List<Vector2>()
            {
                new Vector2(4.80f, -3.60f),
                new Vector2(0.00f,-2.40f),
                new Vector2(-4.40f, 4.80f),
                new Vector2(2.80f, 4.40f),
            };

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        float timePassed = 0.0f;

        try
        {
            while (timePassed < 25.0f)
            {
                if (timePassed > 5.8f)
                {
                    int c = 3;
                }
                backtrackPatrolPath.MoveAlong(0.2f * e.Speed);
                timePassed += 0.2f;
            }
        }
        catch (Exception)
        {
            int c = 3;
            throw;
        }
        //backtrackPatrolPath.GetSegment();

        Assert.Pass();
        //Assert.AreEqual(backtrackPatrolPath.TraverseForward, false);
    }

    [Test]
    public void PatrolEmptyPath_OutputsUnchangingFutureTransforms()
    {
        new DefaultEnemyProperties();

        //Test with empty path vector
        Patrol patrol = new Patrol(new DefaultEnemyProperties(), new List<Vector2>());
        FutureTransform initialFT = patrol.GetTransform();

        for (int i = 1; i < 10; i++)
        {
            patrol.TimeMove(1);
            //Should be treated as "static"
            //Should not be chaning position
            Assert.AreEqual(initialFT, patrol.GetTransform());
            Assert.AreEqual(i, patrol.Time);
        }

        //Test with no path provided
        patrol = new Patrol(new DefaultEnemyProperties(), null);
        initialFT = patrol.GetTransform();

        for (int i = 1; i < 10; i++)
        {
            patrol.TimeMove(1);
            //Should be treated as "static"
            //Should not be chaning position
            Assert.AreEqual(initialFT, patrol.GetTransform());
            Assert.AreEqual(i, patrol.Time);
        }

        //Test by copying previous patrol
        patrol = new Patrol(patrol);
        initialFT = patrol.GetTransform();
        Assert.AreEqual(patrol.Time, 9);
        patrol.Time = 0;
        for (int i = 1; i < 10; i++)
        {
            patrol.TimeMove(1);
            //Should be treated as "static"
            //Should not be chaning position
            Assert.AreEqual(initialFT, patrol.GetTransform());
            Assert.AreEqual(i, patrol.Time);
        }
    }

    [Test]
    public void PatrolPathNonNull_CorrectlyCopiesPath()
    {
        Assert.True(true);
    }

    //    [Test]
    //    public void PatrolPathEndingOnSamePoint_HasCyclicPatrolPath()
    //    {
    //        new DefaultEnemyProperties();
    //        //Assigned patrol route should be cyclic
    //        var path =
    //            new List<Vector2>()
    //            {
    //                new Vector2(0,0),
    //                new Vector2(1,0),
    //                new Vector2(2,0),
    //                new Vector2(0,0),
    //            };
    //        Patrol patrol= new Patrol(new DefaultEnemyProperties(), List<Vector2>);
    //    }
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
    public void PatrolPathGetIndices_StressTest()
    {
        var path =
            new List<Vector2>()
            {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(3,0),
                new Vector2(4,0),
            };

        BacktrackPatrolPath backtrackPatrolPath = new BacktrackPatrolPath(path);
        try
        {
            backtrackPatrolPath.GetSegment(0);
            backtrackPatrolPath.GetSegment(1);
            backtrackPatrolPath.GetSegment(2);
            backtrackPatrolPath.GetSegment(3);
            backtrackPatrolPath.GetSegment(3.15f);
            backtrackPatrolPath.GetSegment(4.15f);
            backtrackPatrolPath.GetSegment(-1);
        }
        catch (Exception)
        {
            Assert.Fail();
        }

        backtrackPatrolPath.MoveAlong(100.0f);
        backtrackPatrolPath.GetSegment();

        Assert.Pass();

        // Use the Assert class to test conditions
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

        backtrackPatrolPath.TraverseForward = false;
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
    public void BacktrackingGetCurrentAtEnd()
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
        Assert.IsNotNull(backtrackPatrolPath.GetSegment(3));
        backtrackPatrolPath.TraverseForward = false;
        Assert.IsNotNull(backtrackPatrolPath.GetSegment(0));
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