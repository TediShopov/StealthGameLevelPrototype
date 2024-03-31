using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : IPredictableThreat
{
    public DefaultEnemyProperties Properties;
    private BacktrackPatrolPath Route;

    private FutureTransform Transform = new FutureTransform
    {
        Position = Vector2.zero,
        Direction = Vector2.right,
    };

    public Patrol(
        DefaultEnemyProperties props,
        List<Vector2> path)
    {
        this.Properties = props;

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
            this.Properties.ViewDistance,
            this.Properties.FOV
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
            Properties.FOV,
            Properties.ViewDistance,
            LayerMask.GetMask("Obstacle")
            );
    }

    public void TimeMove(float deltaTime)
    {
        if (Route == null) return;
        //Move a constant speed along a route
        Route.MoveAlong(deltaTime * Properties.Speed);

        //Get the position along the route and direction
        //heading to the next path point
        Transform = GetPathOrientedTransform(Route);
    }
}