using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPredictableThreat
{
    //Time frame in the future that this threat resides
    public float Time { get; }

    public void TimeMove(float deltaTime);

    public void Reset();

    //Get the 2d posiiton and direction of the threat
    public FutureTransform GetTransform();

    public bool TestThreat(Vector2 collision);

    public Bounds GetBounds();
}