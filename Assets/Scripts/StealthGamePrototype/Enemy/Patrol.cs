using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : IPredictableThreat
{
    public DefaultEnemyProperties AestheticProperties;
    private BacktrackPatrolPath Route;

    private FutureTransform Transform = new FutureTransform
    {
        Position = Vector2.zero,
        Direction = Vector2.right,
    };

    public IPredictableThreat Copy()
    {
        var copy = new Patrol(this);
        return copy;
    }

    public Patrol(Patrol other)
    {
        this.AestheticProperties = other.AestheticProperties;
        //Route creation strategy

        if (other.Route != null)
            this.Route = other.Route.Copy();
        this.Time = other.Time;
        this.Transform = other.Transform;
        this.AestheticProperties = other.AestheticProperties;

        //Path should be valid
        //Otherwise treated as static

        //If paths begging and end point are the same
        //route is cyclic

        //If no of the above the rotue is backtracking
    }

    public Patrol(
        DefaultEnemyProperties props,
        List<Vector2> path)
    {
        this.AestheticProperties = props;

        //Route creation strategy

        //Path should be valid
        if (path != null && path.Count >= 2)
        {
            Route = new BacktrackPatrolPath(path);
        }
        //Otherwise treated as static

        //If paths begging and end point are the same
        //route is cyclic

        //If no of the above the rotue is backtracking
    }

    public Patrol(
        DefaultEnemyProperties props,
        List<Vector2> path,
        FutureTransform initialTransform)
        : this(props, path)
    {
        Transform = initialTransform;
    }

    public float Time { get; set; }

    public Bounds GetBounds()
    {
        return FieldOfView.GetFovBounds(
            GetTransform(),
            this.AestheticProperties.ViewDistance,
            this.AestheticProperties.FOV
            );
    }

    public FutureTransform GetPathOrientedTransform(
        BacktrackPatrolPath path)
    {
        FutureTransform futureTransform = new FutureTransform();

        futureTransform.Position = path.GetCurrent();
        var seg = path.GetSegment();
        futureTransform.Direction = (seg.Item2 - seg.Item1).normalized;
        return futureTransform;
    }

    public FutureTransform GetTransform()
    {
        return Transform;
    }

    public void Reset()
    {
        if (Route != null)
        {
            Route.Reset();
        }
    }

    public bool TestThreat(Vector2 collision)
    {
        return FieldOfView.TestCollision(
            collision,
            Transform,
            AestheticProperties.FOV,
            AestheticProperties.ViewDistance,
            LayerMask.GetMask("Obstacle")
            );
    }

    public void TimeMove(float deltaTime)
    {
        Time += deltaTime;
        if (Route == null) return;
        //Move a constant speed along a route
        Route.MoveAlong(deltaTime * AestheticProperties.Speed);

        //Get the position along the route and direction
        //heading to the next path point
        Transform = GetPathOrientedTransform(Route);
    }
}