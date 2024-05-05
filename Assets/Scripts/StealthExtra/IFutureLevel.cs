using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFutureLevel : ICloneable, IClusterable
{
    public float Step { get; }
    public float Iterations { get; }
    public Transform GlobalTransform { get; set; }
    public List<IPredictableThreat> DynamicThreats { get; }
    public void Generate(LevelPhenotype phenotype);
    public Bounds GetBounds();
    public float GetMaxSimulationTime();
    public bool IsColliding(Vector3 from, Vector3 to);
    public bool IsColliding(Vector2 from, Vector2 to, float timeFrom, float timeTo);
    //Dynamic collisions are collisions with dynamic threats. E.g enemies
    public bool IsDynamicCollision(Vector3 from, Vector3 to);
    //Static collisions are collision with the level geomtry
    public bool IsStaticCollision(Vector3 from, Vector3 to);
}