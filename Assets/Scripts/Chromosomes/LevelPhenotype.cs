using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelPhenotype
{
    [SerializeField] public NativeGrid<int> Zones;
    [SerializeField] public Graph<Vector2> Roadmap;
    [SerializeReference] public IFutureLevel FutureLevel;
    [SerializeReference] public List<IPredictableThreat> Threats;
    public LevelPhenotype()
    {
    }

    public LevelPhenotype(LevelPhenotype other)
    {
        this.Roadmap = new Graph<Vector2>(other.Roadmap);
        this.Zones = new NativeGrid<int>(other.Zones);
        this.Threats = new List<IPredictableThreat>(other.Threats);
        this.FutureLevel = other.FutureLevel;
    }
}