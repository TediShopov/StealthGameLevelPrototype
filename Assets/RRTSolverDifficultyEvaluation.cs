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

    public override string Evaluate()
    {
        return CalculateDifficultyFindingSolution().ToString();
    }

    public override void Init(GameObject phenotype)
    {
    }

    private float CalculateDifficultyFindingSolution()
    {
        int attempts = 0;
        int successful = 0;
        while (attempts < MaxRRTAttempts)
        {
            if (RunRRT())
                successful++;
            attempts++;
            if (successful >= DeisredSuccesses)
                break;
        }
        return (float)successful / (float)attempts;
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