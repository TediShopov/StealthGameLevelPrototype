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
            return PassStr;
        else
        {
            IsTerminating = true;
            return FailStr;
        }
    }
    private List<Vector2Int> GetNeighbours(Vector2Int nc)
    {
        return new List<Vector2Int> {
            new Vector2Int(nc.x, nc.y+1),
            new Vector2Int(nc.x, nc.y-1),
            new Vector2Int(nc.x+1, nc.y+1),
            new Vector2Int(nc.x+1, nc.y-1),
            new Vector2Int(nc.x-1, nc.y+1),
            new Vector2Int(nc.x-1, nc.y-1),
            new Vector2Int(nc.x+1, nc.y),
            new Vector2Int(nc.x-1,nc.y),
        };
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

        {
            Vector2Int startNativeCoord = Heatmap.GetNativeCoord(Start.transform.position);
            List<Vector2Int> startCoords = new List<Vector2Int> { startNativeCoord };
            startCoords.AddRange(GetNeighbours(startNativeCoord));
            foreach (var coord in startCoords)
            {
                FrameObserveStart +=
                    Mathf.FloorToInt(Heatmap.Get(coord.x, coord.y) * FutureLevel.Iterations);
            }
        }

        {
            Vector2Int endNativeCoord = Heatmap.GetNativeCoord(End.transform.position);
            List<Vector2Int> endCoords = new List<Vector2Int> { endNativeCoord };
            endCoords.AddRange(GetNeighbours(endNativeCoord));
            foreach (var coord in endCoords)
            {
                FrameObserveEnd +=
                    Mathf.FloorToInt(Heatmap.Get(coord.x, coord.y) * FutureLevel.Iterations);
            }
        }

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