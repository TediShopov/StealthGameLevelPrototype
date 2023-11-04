using CGALDotNet.Triangulations;
using CGALDotNet;
using CGALDotNet.Triangulations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InitializeStealthLevel : MonoBehaviour
{
    public VoronoiRoadMapGenerator RoadMapGenerator;
    public Graph<Vector2> Graph;
    public float BiasPathDistance = 15.0f;
    public int AttempsToGetCorrectBiasPathDistance = 3;
    public List<List<Vector2>> Clusters;
    private PatrolPath[] PatrolPaths;

    public float ClusterDistance = 1.0f;
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
    public bool RangeBasedClusterPredicate(Vector2 node, Vector2 neighbour ) 
    {
        return Vector2.Distance( node, neighbour ) < ClusterDistance;

    }
    // Start is called before the first frame update
    void Start()
    {

        if (RoadMapGenerator == null) return;
        //Use voronoi roadmap generator to produce culled roadmap graph
        Graph = RoadMapGenerator.GetRoadmapGraph();

        //Cluster points based on geometric distance from each other
        Clusters = Graph.Cluster(RangeBasedClusterPredicate);

        //Merge nodes belonging to the same cluster to a new node located 
        foreach (var cluster in Clusters)
        {
            Graph.MergeNodes(cluster, CalculateCentroid);
        }

        //Generate Patrol Paths
        PatrolPaths = FindObjectsOfType<PatrolPath>();
        foreach (PatrolPath p in PatrolPaths)
        {
            for (int i = 0;i<AttempsToGetCorrectBiasPathDistance;i++) 
            {
                p.Positions = p.GetRandomPathInDistance(Graph,BiasPathDistance);
                if (p.PathLength(p.Positions) >= BiasPathDistance) { break; }
            }
            Debug.Log($"Path Length is: {p.PathLength(p.Positions)}");
            if (!Graph.IsValidPath(p.Positions))
            {
                Debug.Log("Invalid Path");
            }
            p.SetInitialPositionToPath();
        }

    }


    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnDrawGizmosSelected()
    {
        if (Graph == null) return;
        Graph<Vector2>.DebugDrawGraph(Graph,Color.black, Color.green);
        Graph<Vector2>.DebigDrawClusters(Clusters);
    }
}
