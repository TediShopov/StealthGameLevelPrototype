using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Graph<T>
{
    public Dictionary<T, List<T>> adjacencyList = new Dictionary<T, List<T>>();

    private void Start()
    {
    }

    public List<List<T>> Cluster(Func<T, T, bool> pred)
    {
        List<List<T>> clusters = MergeNodesIntoClusters(pred);
        return clusters.Where(x => x.Count > 1).ToList();
    }

    public void AddNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<T>();
        }
    }

    public void RemoveNode(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            List<T> neighbours = GetNeighbors(node).ToList();
            for (int i = 0; i < neighbours.Count; i++)
            {
                RemoveEdge(node, neighbours[i]);
            }
        }
        adjacencyList.Remove(node);
    }

    public List<List<T>> MergeNodesIntoClusters(Func<T, T, bool> clusterPredicate)
    {
        List<List<T>> clusters = new List<List<T>>();
        HashSet<T> visited = new HashSet<T>();

        foreach (var node in adjacencyList.Keys)
        {
            if (!visited.Contains(node))
            {
                List<T> cluster = new List<T>();
                ClusterDFS(node, clusterPredicate, cluster, visited);
                clusters.Add(cluster);
            }
        }

        return clusters;
    }

    public List<T> FindPath(T start, T goal)

    {
        List<T> path = new List<T>();
        HashSet<T> visited = new HashSet<T>();
        DFS(start, goal, visited, path);
        return path;
    }

    private bool DFS(T current, T goal, HashSet<T> visited, List<T> path)
    {
        visited.Add(current);
        path.Add(current);

        if (current.Equals(goal))
        {
            return true; // Path found
        }

        var neighbors = this.adjacencyList[current];
        foreach (T neighbor in neighbors)
        {
            if (!visited.Contains(neighbor))
            {
                if (DFS(neighbor, goal, visited, path))
                {
                    return true;
                }
            }
        }

        // If the goal is not reachable from the current node, backtrack
        path.Remove(current);

        return false;
    }

    public bool IsValidPath(List<T> path)
    {
        //Test path
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] == null) return false;
            if (!this.GetNeighbors(path[i]).Contains(path[i + 1]))
            { return false; }
        }
        return true;
    }

    public List<T> GetUnvisitedNeighbors(T node, HashSet<T> visited)
    {
        List<T> neighbors = new List<T>();

        foreach (var neighbor in this.GetNeighbors(node))
        {
            if (!visited.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    // Fisher-Yates shuffle for randomly ordering the list.
    public void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + (int)(UnityEngine.Random.value * (n - i));
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }

    private void ClusterDFS(T currentNode, Func<T, T, bool> clusterPredicate, List<T> cluster, HashSet<T> visited)
    {
        visited.Add(currentNode);
        cluster.Add(currentNode);

        foreach (var neighbor in this.GetNeighbors(currentNode))
        {
            if (!visited.Contains(neighbor) && clusterPredicate(currentNode, neighbor))
            {
                ClusterDFS(neighbor, clusterPredicate, cluster, visited);
            }
        }
    }

    public void RemoveEdge(T node1, T node2)
    {
        if (!adjacencyList.ContainsKey(node1) || !adjacencyList.ContainsKey(node2))
        {
            // You can handle this case as needed.
            // Either ignore the edge or add the missing nodes.
            return;
        }

        adjacencyList[node1].Remove(node2);
        adjacencyList[node2].Remove(node1); // For an undirected graph
    }

    public bool AddEdge(T node1, T node2)
    {
        if (!adjacencyList.ContainsKey(node1) || !adjacencyList.ContainsKey(node2))
        {
            // You can handle this case as needed.
            // Either ignore the edge or add the missing nodes.
            return false;
        }
        if (!adjacencyList[node1].Contains(node2))
        {
            adjacencyList[node1].Add(node2);
        }
        if (!adjacencyList[node2].Contains(node1))
        {
            adjacencyList[node2].Add(node1); // For an undirected graph
        }
        return true;
    }

    public List<T> GetNeighbors(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            return adjacencyList[node];
        }
        return new List<T>();
    }

    //    public void SimplifyGraph( float mergeDistance)
    //    {
    //        Debug.Log("Simplifying");
    //        var nodes = new List<T>(this.adjacencyList.Keys);
    //
    //        for (int i = 0; i < nodes.Count; i++)
    //        {
    //            for (int j = i + 1; j < nodes.Count; j++)
    //            {
    //                T nodeA = nodes[i];
    //                T nodeB = nodes[j];
    //
    //                if (Vector2.Distance(nodeA, nodeB) <= mergeDistance)
    //                {
    //                    MergeNodes(this, nodeA, nodeB);
    //                    nodes.Remove(nodeB);
    //                    j--;
    //                }
    //            }
    //        }
    //    }
    public void MergeNodes(List<T> nodesToMerge, Func<List<T>, T> newNodeCalculation)
    {
        if (nodesToMerge.Count == 0)
            return;

        // Calculate the centroid of the nodes to be merged.
        T centroid = newNodeCalculation(nodesToMerge);

        // Create a new node at the centroid.
        this.AddNode(centroid);

        // Get the neighbors of the nodes to be merged.
        List<T> neighborsToMerge = new List<T>();
        foreach (var node in nodesToMerge)
        {
            neighborsToMerge.AddRange(this.GetNeighbors(node));
        }

        // Add the new node as a neighbor to all the neighbors of the merged nodes.
        foreach (var neighbor in neighborsToMerge)
        {
            this.AddEdge(centroid, neighbor);
        }

        // Remove the merged nodes from the this.
        foreach (var node in nodesToMerge)
        {
            this.RemoveNode(node);
        }
    }

    private static void MergeNodes(Graph<T> graph, T nodeA, T nodeB)
    {
        Debug.Log("Merged Nodes");
        List<T> neighborsB = graph.GetNeighbors(nodeB);

        foreach (T neighbor in neighborsB)
        {
            if (!graph.GetNeighbors(nodeA).Contains(neighbor))
            {
                graph.AddEdge(nodeA, neighbor);
            }
        }

        graph.RemoveNode(nodeB);
    }

    // Start is called before the first frame updatepublic

    public static void DebugDrawGraph(Graph<Vector2> graph, Color nodeColor, Color connectionColor, float nodeRadius = 0.1f)
    {
        foreach (var node in graph.adjacencyList)
        {
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(node.Key, nodeRadius);
            foreach (var connection in node.Value)
            {
                Gizmos.color = connectionColor;
                Gizmos.DrawLine(node.Key, connection);
            }
        }
    }

    public static void DebugDrawGraph(Graph<Vector3> graph, Color nodeColor, Color connectionColor)
    {
        foreach (var node in graph.adjacencyList)
        {
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(node.Key, 0.1f);
            foreach (var connection in node.Value)
            {
                Gizmos.color = connectionColor;
                Gizmos.DrawLine(node.Key, connection);
            }
        }
    }

    public static void DebigDrawClusters(List<List<Vector2>> clusters)
    {
        foreach (var cluster in clusters)
        {
            if (cluster.Count > 1)
            {
                foreach (var node in cluster)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(node, 0.1f);
                }
            }
        }
    }
}