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
    public Graph<Vector2> Roadmap { get; set; }
    virtual public List<Vector2> GeneratePath() 
    {
        var keyValueList = this.Roadmap.adjacencyList.Keys.ToArray();

        // Generate a random index to pick an element.
        int randomIndex = UnityEngine.Random.Range(0,this.Roadmap.adjacencyList.Count);

        var path = new List<Vector2>();
        for (int i = 0; i < AttemptsToMatchBiasedDistance; i++)
        {
            RandomPathDFS(keyValueList[randomIndex], new HashSet<Vector2>(), path, BiasPathDistance);
            if (PathLength(path) >= BiasPathDistance) { break; }
        }
        return path;
    }
    virtual public List<List<Vector2>> GeneratePaths(int pathsToGenerate) 
    {
        var keyValueList = this.Roadmap.adjacencyList.Keys.ToArray();

        List<List<Vector2>> pathsToReturn=new List<List<Vector2>>();

        var visited= new HashSet<Vector2>();
        for (int i = 0; i < pathsToGenerate; i++) 
        {
            var path = new List<Vector2>();
            for (int j = 0; j < AttemptsToMatchBiasedDistance; j++)
            {
                // Generate a random index to pick an element.
                int randomIndex = UnityEngine.Random.Range(0,this.Roadmap.adjacencyList.Count);
                RandomPathDFS(keyValueList[randomIndex], visited, path, BiasPathDistance);
                if (PathLength(path) >= BiasPathDistance) { break; }
            }
            pathsToReturn.Add(path);
        }
        return pathsToReturn;
    }
    private float RandomPathDFS(   Vector2 currentNode, HashSet<Vector2> visited, List<Vector2> path, float maxDistance)
    {
        visited.Add(currentNode);
        path.Add(currentNode);

        List<Vector2> unvisitedNeighbors = Roadmap.GetUnvisitedNeighbors(currentNode, visited);

        float coveredDistance= PathLength(path);
        if (unvisitedNeighbors.Count == 0)
            return coveredDistance;
        if (coveredDistance > maxDistance)
        {
            return coveredDistance;
        }

        // Shuffle the neighbors randomly.
        Roadmap.Shuffle(unvisitedNeighbors);

        foreach (var neighbor in unvisitedNeighbors)
        {
            float distToNeighbor= Vector2.Distance(currentNode, neighbor);
            if (RandomPathDFS(neighbor, visited, path, maxDistance)  >maxDistance) 
            {
                return PathLength(path);
            }
            else
            {

                //If path doesnt satisfy criteria remove the nodes from it and try the next neighbour;
                int index = path.FindIndex(x=>x==currentNode);
                path.RemoveRange(index+1, path.Count - index - 1);
            }
        }
        return PathLength(path);
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
}
