using Mono.Cecil;
using StealthLevelEvaluation;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Provides negative fitness depening on how much spawn or destination areas
// are observed by guard FOVS
[ExecuteInEditMode]
public class StartEndDestinationObserveTime : MeasureMono
{
    private IFutureLevel FutureLevel;
    private NativeGrid<float> Heatmap;
    private GameObject Start;
    private GameObject End;
    public int MaxTimeFrameObservedStart = 3;
    public int MaxTimeFramesObservedEnd = 3;
    public int FrameObserveStart = 0;
    public int FrameObserveEnd = 0;

    public override MeasurementType GetCategory()
    {
        return MeasurementType.VALIDATION;
    }

    public override string GetName()
    {
        return "StartEndDestinationObserveTime";
    }

    protected override string Evaluate()
    {
        if (Passes())
            return "True";
        else
        {
            IsTerminating = true;
            return "False";
        }
    }

    private bool Passes()
    {
        if (Manifestation != null)
            Init(Manifestation);
        else
        {
            var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
            if (level != null)
            {
                Manifestation = level;
                Init(level);
            }
            else return false;
        }

        Vector2Int startNativeCoord = Heatmap.GetNativeCoord(Start.transform.position);
        Vector2Int endNativeCoord = Heatmap.GetNativeCoord(End.transform.position);

        FrameObserveStart = Mathf.FloorToInt(Heatmap.Get(startNativeCoord.x, startNativeCoord.y) * FutureLevel.Iterations);
        FrameObserveEnd = Mathf.FloorToInt(Heatmap.Get(endNativeCoord.x, endNativeCoord.y) * FutureLevel.Iterations);

        if (FrameObserveStart > MaxTimeFrameObservedStart)
            return false;
        if (FrameObserveEnd > MaxTimeFramesObservedEnd)
            return false;

        return true;
    }

    public override void Init(GameObject manifestation)
    {
        IsValidator = true;
        Manifestation = manifestation;
        var phenotype = manifestation.GetComponentInChildren<LevelChromosomeMono>()
            .Chromosome.Phenotype;

        FutureLevel = phenotype.FutureLevel;
        Heatmap = new NativeGrid<float>(FutureLevel.GetHeatmap());
        Heatmap.Grid.Origin = this.Manifestation.transform.position;
        Start = Manifestation.GetComponentInChildren<CharacterController2D>().gameObject;
        End = Manifestation.GetComponentInChildren<WinTrigger>().gameObject;
    }
}