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
    public PatrolPath[] PatrolPaths;
    // Start is called before the first frame update
    void Start()
    {

        if (RoadMapGenerator == null || Graph == null) return;
        RoadMapGenerator.InitializeRoadmap();
        GenerateGraphFromLineSegments(Graph, RoadMapGenerator.GetValidSegments(RoadMapGenerator._triangulation));
        Graph.Simplify();
        PatrolPaths = FindObjectsOfType<PatrolPath>();
        foreach (PatrolPath p in PatrolPaths) 
        {
            p.Positions = Graph.GetRandomPathInDistance(10.0f);
            p.SetInitialPositionToPath();
        }
        
    }
    

    // Update is called once per frame
    void Update()
    {
        
    }
    void GenerateGraphFromLineSegments(Graph graph, List<Vector2>segments)
    {
        for (int i = 0;i<segments.Count-1;i+=2) 
        {
            graph.AddNode(segments[i]);
            graph.AddNode(segments[i+1]);
            graph.AddEdge(segments[i], segments[i+1]);
        }
    }
}
