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

    public bool TerminateAfterSuccessesReached = true;
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
        for (int i = 0; i < MaxRRTAttempts; i++)
        {
            float time = 0;
            var measurmentChild = new MeasureResult();
            measurmentChild.Name = "RRT";
            measurmentChild.Value = "-";
            this.Result.AddChildMeasure(measurmentChild);
        }
    }

    private float CalculateDifficultyFindingSolution()
    {
        Attempts = 0;
        Successes = 0;
        while (Attempts < MaxRRTAttempts)
        {
            if (RunRRT(Attempts))
                Successes++;
            Attempts++;
            if (TerminateAfterSuccessesReached)
            {
                if (Successes >= DeisredSuccesses)
                    break;
            }
        }
        return Chance;
    }

    private bool RunRRT(int i)
    {
        var RRT = Instantiate(RRTPrefab, this.transform);
        var rrtVisualizer = RRT.GetComponent<RapidlyExploringRandomTreeVisualizer>();
        rrtVisualizer.Setup();

        var measurmentChild = this.Result.ChildMeasures[i];
        float time =
            Helpers.TrackExecutionTime(rrtVisualizer.Run);
        measurmentChild.Name = "RRT";
        if (rrtVisualizer.RRT.Succeeded())
            measurmentChild.Value = "True";
        else
            measurmentChild.Value = "False";
        measurmentChild.Time = time;

        return rrtVisualizer.RRT.Succeeded();
    }
}