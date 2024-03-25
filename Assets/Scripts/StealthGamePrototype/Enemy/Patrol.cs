using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : IPredictableThreat
{
    private DefaultEnemyProperties Properties;
    private BacktrackPatrolPath Route;
    private FutureTransform Transform;

    public Patrol(
        DefaultEnemyProperties props,
        List<Vector2> path)
    {
        //Route creation strategy

        //If path is contains only a single point
        //Static enemy route

        //If paths begging and end point are the same
        //route is cyclic

        //If no of the above the rotue is backtracking
    }

    public float Time { get; set; }

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

    public bool TestThreat(Vector2 collision)
    {
        return FieldOfView.TestCollision(
            collision,
            Transform,
            Properties.FOV,
            Properties.ViewDistance,
            LayerMask.GetMask("Obstacles")
            );
    }

    public void TimeMove(float deltaTime)
    {
        //Move a constant speed along a route
        Route.MoveAlong(deltaTime * Properties.Speed);

        //Get the position along the route and direction
        //heading to the next path point
        Transform = GetPathOrientedTransform(Route);
    }
}