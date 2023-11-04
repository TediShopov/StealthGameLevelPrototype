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
    public Graph Graph;
    public float BiasPathDistance = 15.0f;
    public int AttempsToGetCorrectBiasPathDistance = 3;
    private PatrolPath[] PatrolPaths;

    public float ClusterDistance = 1.0f;

    public bool RangeBasedClusterPredicate(Vector2 node, Vector2 neighbour ) 
    {
        return Vector2.Distance( node, neighbour ) < ClusterDistance;

    }
    // Start is called before the first frame update
    void Start()
    {

        if (RoadMapGenerator == null) return;
        Graph = RoadMapGenerator.GetRoadmapGraph();
        GenerateGraphFromLineSegments(Graph, RoadMapGenerator.GetValidSegments(RoadMapGenerator._triangulation));
        Graph.Cluster(RangeBasedClusterPredicate);
        PatrolPaths = FindObjectsOfType<PatrolPath>();
        foreach (PatrolPath p in PatrolPaths)
        {
            for (int i = 0;i<AttempsToGetCorrectBiasPathDistance;i++) 
            {
                p.Positions = Graph.GetRandomPathInDistance(BiasPathDistance);
                if (Graph.PathLength(p.Positions) >= BiasPathDistance) { break; }
            }
            Debug.Log($"Path Length is: {Graph.PathLength(p.Positions)}");
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
        Graph.DebugDrawGraph(Color.black, Color.green);
    }
}
