using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IPathGenerator
{
    public Graph<Vector2> Roadmap { get; set; }

    public List<Vector2> GeneratePath();
}

public class PathGeneratorClass : MonoBehaviour, IPathGenerator
{
    public float BiasPathDistance;
    public int AttemptsToMatchBiasedDistance;
    public System.Random LevelRandom;

    public void Awake()
    {
        //Random.InitState(seed: RandomPathSeed);
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        if (level)
            LevelRandom = level.GetComponent<SpawnRandomStealthLevel>().LevelRandom;
        else
            LevelRandom = new System.Random();
    }

    public Graph<Vector2> Roadmap { get; set; }

    public virtual List<List<Vector2>> GeneratePaths(int pathsToGenerate)
    {
        var keyValueList = this.Roadmap.adjacencyList.Keys.ToArray();

        List<List<Vector2>> pathsToReturn = new List<List<Vector2>>();

        var visited = new HashSet<Vector2>();
        for (int i = 0; i < pathsToGenerate; i++)
        {
            var bestFoundPath = new List<Vector2>();
            var path = new List<Vector2>();
            for (int j = 0; j < AttemptsToMatchBiasedDistance; j++)
            {
                // Generate a random index to pick an element.
                int randomIndex = LevelRandom.Next(0, this.Roadmap.adjacencyList.Count);
                if (randomIndex > this.Roadmap.adjacencyList.Count)
                    randomIndex = LevelRandom.Next(0, this.Roadmap.adjacencyList.Count);
                RandomPathDFS(keyValueList[randomIndex], ref visited, ref path, ref bestFoundPath, BiasPathDistance);
                if (PathLength(bestFoundPath) >= BiasPathDistance) { break; }
            }
            pathsToReturn.Add(bestFoundPath);
        }
        return pathsToReturn;
    }

    private void RandomPathDFS(Vector2 currentNode, ref HashSet<Vector2> visited, ref List<Vector2> currentPath, ref List<Vector2> bestPath, float maxDistance)
    {
        visited.Add(currentNode);
        currentPath.Add(currentNode);

        List<Vector2> unvisitedNeighbors = Roadmap.GetUnvisitedNeighbors(currentNode, visited);

        float coveredDistance = PathLength(currentPath);
        if (coveredDistance < maxDistance && unvisitedNeighbors.Count != 0)
        {
            //Update best path
            if (coveredDistance > PathLength(bestPath))
                bestPath = new List<Vector2>(currentPath);
            // Shuffle the neighbors randomly.
            Roadmap.Shuffle(LevelRandom, unvisitedNeighbors);
            foreach (var neighbor in unvisitedNeighbors)
            {
                RandomPathDFS(neighbor, ref visited, ref currentPath, ref bestPath, maxDistance);
            }
        }
        //Rollback changes
        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(currentNode);
    }

    public float PathLength(List<Vector2> path)
    {
        float length = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector2.Distance(path[i], path[i + 1]);
        }
        return length;
    }

    public List<Vector2> GeneratePath()
    {
        throw new System.NotImplementedException();
    }
}