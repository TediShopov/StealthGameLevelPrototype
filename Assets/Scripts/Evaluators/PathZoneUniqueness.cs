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
                if (zoneIndexList.Any(x => x == currentZoneIndex) == false)
                {
                    zoneIndexList.Add(currentZoneIndex);
                }
            }
        }
        return zoneIndexList;
    }

    public bool ZoneAreEqual(List<int> zoneA, List<int> zoneB)
    {
        return zoneA.SequenceEqual(zoneB);
    }

    public override string Evaluate()
    {
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

        int unqiuePathsCount = 0;

        if (solutionPathsZones.Count > 0)
        {
            unqiuePathsCount++;
        }

        for (int i = 0; i < solutionPathsZones.Count - 1; i++)
        {
            for (int j = i + 1; j < solutionPathsZones.Count; j++)
            {
                if (ZoneAreEqual(solutionPathsZones[i], solutionPathsZones[j]))
                { unqiuePathsCount++; }
            }
        }

        return unqiuePathsCount.ToString();
    }

    public override void Init(GameObject phenotype)
    {
        Name = "PathZoneUniqueness";
        LevelObject = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
    }
}