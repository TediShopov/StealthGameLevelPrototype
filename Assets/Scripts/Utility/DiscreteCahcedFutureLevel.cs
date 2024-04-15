using PlasticGui.Configuration;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticLevel
{
    private float From;
    private float To;
    private float TimeStep;
    public IEnumerable<IPredictableThreat> Threats;

    public int StepCount =>
      Mathf.FloorToInt((To - From) / (float)TimeStep);

    public float CurrentTime = 0;
    public bool IsFinished => CurrentTime > To;

    public List<List<IPredictableThreat>> LevelThreats;

    public StaticLevel(
        IEnumerable<IPredictableThreat> threats,
        float from,
        float to,
        float timeStep)
    {
        var simulation = new DynamicLevelSimulation(threats, from, to, timeStep);
        LevelThreats = new List<List<IPredictableThreat>>();
        while (!simulation.IsFinished)
        {
            var currentTimeFrameThreats = new List<IPredictableThreat>();
            //Get 2d position
            //float passedTim e = simulation.CurrentTime - from.z;
            foreach (var threat in simulation.Threats)
                currentTimeFrameThreats.Add(threat.Copy());
            LevelThreats.Add(currentTimeFrameThreats);
            simulation.Progress();
        }
    }
}

public class DiscreteCahcedFutureLevel : DiscreteRecalculatingFutureLevel
{
    private StaticLevel StaticLevel;

    public override void Init()
    {
        base.Init();
        StaticLevel = new StaticLevel(
            this.DynamicThreats, 0, GetMaxSimulationTime(), this.Step);
    }

    public override bool IsDynamicCollision(Vector3 from, Vector3 to)
    {
        int index = 0;
        int fromTimeFrameIndex = Mathf.FloorToInt(from.z / Step);
        int toTimeFrameIndex = Mathf.CeilToInt(to.z / Step);

        //Clamp index if neccesary
        if (toTimeFrameIndex >= StaticLevel.LevelThreats.Count)
            toTimeFrameIndex = StaticLevel.LevelThreats.Count - 1;

        for (int i = fromTimeFrameIndex; i <= toTimeFrameIndex; i++)
        {
            float rel = Mathf.InverseLerp(from.z, to.z, index * Step);
            Vector2 positionInTime = Vector2.Lerp(from, to, rel);
            foreach (var threat in this.StaticLevel.LevelThreats[i])
            {
                if (threat.TestThreat(positionInTime))
                    return true;
            }
            index++;
        }
        return false;
    }

    public override IFutureLevel PrototypeComponent(GameObject to)
    {
        var other = to.AddComponent<DiscreteCahcedFutureLevel>();
        other.BoundaryLayerMask = this.BoundaryLayerMask;
        other.ObstacleLayerMask = this.ObstacleLayerMask;
        other._iter = this._iter;
        other._step = this._step;
        return other;
    }

    public override void Update()
    {
        base.Update();
    }

    public void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
    }
}