using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Graph : MonoBehaviour
{
    public Dictionary<Vector2, List<Vector2>> adjacencyList = new Dictionary<Vector2, List<Vector2>>();
    public bool Simplify = false;
    public float SimplifyDistance = 1.0f;
    List<List<Vector2>> clusters=new List<List<Vector2>>();

    void Update()
    {
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (Simplify) 
        {
            Debug.Log($"Total connection: {GetTotalConnectionsCount()}");
            Simplify = false;
            //            SimplifyGraph( SimplifyDistance);

            clusters = MergeNodesIntoClusters();
            Debug.Log($"Clusters: {clusters.Count} ");
        }
        
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

        adjacencyList[node1].Add(node2);
        adjacencyList[node2].Add(node1); // For an undirected graph
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
        foreach (var cluster in clusters)
        {
            if (cluster.Count > 1) 
            {

                foreach (var node in cluster)
                {

                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(node, 1.0f);
                }
            }
        }
    }
}
