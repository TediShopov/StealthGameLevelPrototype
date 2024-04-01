using StealthLevelEvaluation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathZoneUniqueness : MeasureMono
{
    private GameObject LevelObject;

    public RapidlyExploringRandomTreeVisualizer[] GetRRTMonos()
    {
        if (LevelObject == null)
            throw new System.ArgumentException("No level object is assigned to measure mono.");

        //Todo make sure RRTs algorithms are already ran
        var RRTMonos =
            LevelObject.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();
        if (RRTMonos == null || RRTMonos.Length == 0)
            throw new System.ArgumentException("No RRT monos have been ran on the level.");

        return RRTMonos;
    }

    public static List<int> GetPathVisitedZones(
        FloodfilledRoadmapGenerator floodfilled,
        List<Vector3> path)
    {
        if (floodfilled == null)
            throw new System.ArgumentNullException(
                "Needs a floodfill algorithm to define level zeons");

        var zoneIndexList = new List<int>();

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int segmentA = (Vector2Int)floodfilled.Grid.WorldToCell(path[i]);
            Vector2Int segmentB = (Vector2Int)floodfilled.Grid.WorldToCell(path[i + 1]);
            Vector2Int[] cellsAlongLine =
                VoxelizedLevelBase.GetCellsInLine(segmentA, segmentB);
            foreach (var cellInSegment in cellsAlongLine)
            {
                int currentZoneIndex = floodfilled.GetCellZoneIndex(cellInSegment);
                //Add only if index of zone is not found previously in the array
                //                if (zoneIndexList.Count == 0)
                //                    zoneIndexList.Add(currentZoneIndex);
                //                else
                //                {
                //                    if (zoneIndexList[zoneIndexList.Count - 1] != currentZoneIndex)
                //                        zoneIndexList.Add(currentZoneIndex);
                //                }

                if (zoneIndexList.Any(x => x == currentZoneIndex) == false)
                {
                    zoneIndexList.Add(currentZoneIndex);
                }
            }
        }

        return zoneIndexList;
    }

    public List<List<int>> SeenPaths;

    public bool ZoneAreEqual(List<int> zoneA, List<int> zoneB)
    {
        return zoneA.SequenceEqual(zoneB);
    }

    public override string Evaluate()
    {
        SeenPaths = new List<List<int>>();
        var monos = GetRRTMonos();

        var flood = LevelObject.GetComponentInChildren<FloodfilledRoadmapGenerator>();

        List<List<Vector3>> solutionPaths =
            monos
            .Where(x => x.RRT.Succeeded())
            .Select(x => x.RRT.ReconstructPathToSolution())
            .ToList();

        List<List<int>> solutionPathsZones =
            solutionPaths
            .Select(x => GetPathVisitedZones(flood, x))
            .ToList();

        SeenPaths = new List<List<int>>();

        foreach (var path in solutionPathsZones)
        {
            if (SeenPaths.Any(x => ZoneAreEqual(x, path)) == false)
                SeenPaths.Add(path);
        }
        return SeenPaths.Count.ToString();
    }

    public override void Init(GameObject phenotype)
    {
        Name = "PathZoneUniqueness";
        LevelObject = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
    }
}