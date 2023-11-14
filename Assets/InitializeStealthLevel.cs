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
    public PathGeneratorClass PathGenerator;
    

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

        if(PathGenerator !=  null) 
        {

            PathGenerator.Roadmap = Graph;
            PatrolPaths = FindObjectsOfType<PatrolPath>().Where(x=>x.Randomized == true).ToArray();
            var paths = PathGenerator.GeneratePaths(PatrolPaths.Length);
            for (int i = 0;i<PatrolPaths.Length;i++) 
            {
                PatrolPaths[i].Positions = paths[i];
                PatrolPaths[i].SetInitialPositionToPath();
            }
        }
        //Generate Patrol Paths

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
