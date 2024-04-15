using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RRTSolverDifficultyEvaluation : MeasureMono
{
    //Solver prefab of object
    public GameObject RRTPrefab;

    public int DeisredSuccesses;
    public int MaxRRTAttempts;

    public int Successes;
    public int Attempts;
    public float Chance => (float)Successes / (float)Attempts;

    public override MeasurementType GetCategory()
    {
        return MeasurementType.DIFFICULTY;
    }

    public override string GetName()
    {
        return "" + RRTPrefab.name;
    }

    protected override string Evaluate()
    {
        Successes = 0;
        Attempts = 0;
        return CalculateDifficultyFindingSolution().ToString();
    }

    public override void Init(GameObject phenotype)
    {
    }

    private float CalculateDifficultyFindingSolution()
    {
        Attempts = 0;
        Successes = 0;
        while (Attempts < MaxRRTAttempts)
        {
            if (RunRRT())
                Successes++;
            Attempts++;
            if (Successes >= DeisredSuccesses)
                break;
        }
        return Chance;
    }

    private bool RunRRT()
    {
        var RRT = Instantiate(RRTPrefab, this.transform);
        var rrtVisualizer = RRT.GetComponent<RapidlyExploringRandomTreeVisualizer>();
        rrtVisualizer.Setup();
        rrtVisualizer.Run();
        return rrtVisualizer.RRT.Succeeded();
    }
}