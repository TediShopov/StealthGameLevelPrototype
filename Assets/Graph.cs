using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public Dictionary<Vector2, List<Vector2>> adjacencyList = new Dictionary<Vector2, List<Vector2>>();
    public float SimplifyDistance = 1.0f;
    List<List<Vector2>> clusters=new List<List<Vector2>>();
    public bool DebugClusters=true;
    public bool DebugGraph=true;
    private void Start()
    {
        
    }
    public void Simplify() 
    {
        clusters = MergeNodesIntoClusters();
        foreach (var cluster in clusters)
        {
            if (cluster.Count > 1)
            {
                MergeNodes(cluster);
            }

        }
    }

    void Update()
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //        if (Simplify) 
        //        {
        //            Debug.Log($"Total connection: {GetTotalConnectionsCount()}");
        //            Simplify = false;
        //            //            SimplifyGraph( SimplifyDistance);
        //
        //            clusters = MergeNodesIntoClusters();
        //            foreach (var cluster in clusters)
        //            {
        //                if (cluster.Count > 1) 
        //                {
        //                    MergeNodes(cluster);
        //                }
        //
        //            }
        //            Debug.Log($"Clusters: {clusters.Count} ");
        //        }

    }
    public List<Vector2> GetRandomPathInDistance(float distanceToCover) 
    {
        var keyValueList = this.adjacencyList.Keys.ToArray();

        // Generate a random index to pick an element.
        int randomIndex = Random.Range(0,this.adjacencyList.Count);
        // Get the randomly selected key-value pair.
        return RandomPathDFS(keyValueList[randomIndex], distanceToCover);

    }
    public int GetTotalConnectionsCount() 
    {
        int totalConnections = 0;
        foreach (var node in adjacencyList)
        {
            totalConnections += node.Value.Count;
        }
        return totalConnections;
    }

    public void AddNode(Vector2 node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<Vector2>();
        }
    }
    public void RemoveNode(Vector2 node) 
    {

        if (adjacencyList.ContainsKey(node))
        {
            List<Vector2> neighbours = GetNeighbors(node).ToList();
            for (int i = 0; i < neighbours.Count; i++) 
            {
                RemoveEdge(node,neighbours[i]);
            }

        }
        adjacencyList.Remove(node);
    }
     public List<List<Vector2>> MergeNodesIntoClusters()
    {
        List<List<Vector2>> clusters = new List<List<Vector2>>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        foreach (var node in adjacencyList.Keys)
        {
            if (!visited.Contains(node))
            {
                List<Vector2> cluster = new List<Vector2>();
                DFS(node, cluster, visited);
                clusters.Add(cluster);
            }
        }

        return clusters;
    }
    public List<Vector2> RandomPathDFS(Vector2 startNode, float maxDistance)
    {
        List<Vector2> path = new List<Vector2>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        RandomPathDFSHelper(startNode, visited, path,0,maxDistance );

        return path;
    }

    private bool RandomPathDFSHelper(   Vector2 currentNode, HashSet<Vector2> visited, List<Vector2> path, float coveredDistance,float maxDistance)
    {
        visited.Add(currentNode);
        path.Add(currentNode);

        List<Vector2> unvisitedNeighbors = GetUnvisitedNeighbors(currentNode, visited);

        if (unvisitedNeighbors.Count == 0)
            return false;
        if (coveredDistance > maxDistance)
        {
            return true;
        }

        // Shuffle the neighbors randomly.
        Shuffle(unvisitedNeighbors);

        foreach (var neighbor in unvisitedNeighbors)
        {
            float distToNeighbor= Vector2.Distance(currentNode, neighbor);
            if (RandomPathDFSHelper(neighbor, visited, path, coveredDistance+distToNeighbor,maxDistance)) 
            {
                return true;
            }
        }
        return false;
    }

    private List<Vector2> GetUnvisitedNeighbors(Vector2 node, HashSet<Vector2> visited)
    {
        List<Vector2> neighbors = new List<Vector2>();

        foreach (var neighbor in this.GetNeighbors(node))
        {
            if (!visited.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    // Fisher-Yates shuffle for randomly ordering the list.
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + (int)(Random.value * (n - i));
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }
    }

    private void DFS(Vector2 currentNode, List<Vector2> cluster, HashSet<Vector2> visited)
    {
        visited.Add(currentNode);
        cluster.Add(currentNode);

        foreach (var neighbor in this.GetNeighbors(currentNode))
        {
            float distance = Vector2.Distance(currentNode, neighbor);
            if (!visited.Contains(neighbor) && distance<= SimplifyDistance)
            {
                DFS(neighbor, cluster, visited);
            }
        }
    }
    public void RemoveEdge(Vector2 node1, Vector2 node2) 
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


    public void AddEdge(Vector2 node1, Vector2 node2)
    {
        if (!adjacencyList.ContainsKey(node1) || !adjacencyList.ContainsKey(node2))
        {
            // You can handle this case as needed.
            // Either ignore the edge or add the missing nodes.
            return;
        }
        if (!adjacencyList[node1].Contains(node2)) 
        {
            adjacencyList[node1].Add(node2);
        }
        if (!adjacencyList[node2].Contains(node1)) 
        {
            adjacencyList[node2].Add(node1); // For an undirected graph
        }

    }

    public List<Vector2> GetNeighbors(Vector2 node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            return adjacencyList[node];
        }
        return new List<Vector2>();
    }

    public Dictionary<Vector2, List<Vector2>> GetGraph()
    {
        return adjacencyList;
    }
    public void SimplifyGraph( float mergeDistance)
    {
        Debug.Log("Simplifying");
        var nodes = new List<Vector2>(this.adjacencyList.Keys);

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                Vector2 nodeA = nodes[i];
                Vector2 nodeB = nodes[j];

                if (Vector2.Distance(nodeA, nodeB) <= mergeDistance)
                {
                    MergeNodes(this, nodeA, nodeB);
                    nodes.Remove(nodeB);
                    j--;
                }
            }
        }
    }
     public void MergeNodes(List<Vector2> nodesToMerge)
    {
        if (nodesToMerge.Count == 0)
            return;

        // Calculate the centroid of the nodes to be merged.
        Vector2 centroid = CalculateCentroid(nodesToMerge);

        // Create a new node at the centroid.
        this.AddNode(centroid);

        // Get the neighbors of the nodes to be merged.
        List<Vector2> neighborsToMerge = new List<Vector2>();
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

    private Vector2 CalculateCentroid(List<Vector2> nodes)
    {
        if (nodes.Count == 0)
            return Vector2.zero;

        float totalX = 0;
        float totalY = 0;

        foreach (var node in nodes)
        {
            totalX += node.x;
            totalY += node.y;
        }

        float centroidX = totalX / nodes.Count;
        float centroidY = totalY / nodes.Count;

        return new Vector2(centroidX, centroidY);
    }

    private static void MergeNodes(Graph graph, Vector2 nodeA, Vector2 nodeB)
    {
        Debug.Log("Merged Nodes");
        List<Vector2> neighborsB = graph.GetNeighbors(nodeB);

        foreach (Vector2 neighbor in neighborsB)
        {
            if (!graph.GetNeighbors(nodeA).Contains(neighbor))
            {
                graph.AddEdge(nodeA, neighbor);
            }
        }

        graph.RemoveNode(nodeB);
    }
    // Start is called before the first frame update
    private void OnDrawGizmosSelected()
    {
        if (DebugGraph)
        {
            foreach (var node in adjacencyList)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(node.Key, 0.1f);
                foreach (var connection in node.Value)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(node.Key, connection);
                }
            }
        }
        if (DebugClusters) 
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
                    var centroid = CalculateCentroid(cluster);
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(centroid, 0.2f);
                }
            }
        }
//        if (DebugClusters) 
//        {
//            if (RandomPath!=null && RandomPath.Count > 0) 
//            {
//
//                    foreach (var pathPoint in RandomPath)
//                    {
//
//                        Gizmos.color = Color.blue;
//                        Gizmos.DrawSphere(pathPoint, 0.1f);
//                    }
//            }
//        }
    }
}
