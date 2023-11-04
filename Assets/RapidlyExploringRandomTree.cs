using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RapidlyExploringRandomTree : MonoBehaviour
{
    public Transform StartNode;
    public Transform EndNode;
    public float maxStepSize = 1.0f;
    public int maxIterations = 1000;
    public bool BuildAtBegining = true;
    public bool DoRRTStep = false;
    public PolygonBoundary Boundary;
    public LayerMask BoundaryObstacleLayerMask;
    private Bounds RandomBounds;
    private Graph<Vector2> RRTGraph;
    private KDTree kdTree;
   // private List<Transform> nodes = new List<Transform>();

    private void Start()
    {
        this.RRTGraph = new Graph<Vector2>();
        kdTree = new KDTree(StartNode.position,0);
        if (Boundary != null)
        {
            RandomBounds = Boundary.GetComponent<PolygonCollider2D>().bounds;
        }
        if(BuildAtBegining) 
        {

            BuildRRT();
        }
    }
    public void Update()
    {
        if(DoRRTStep) 
        {
            RRTStep();
            DoRRTStep = false;
        }
        
    }

    private void BuildRRT()
    {
        kdTree = new KDTree(StartNode.position,0);
       // nodes.Add(startNode);

        for (int i = 0; i < maxIterations; i++)
        {
            RRTStep();
        }

        Debug.Log("RRT did not reach the goal.");
    }
    Vector2 randomPoint;
    KDTree nearestNode;
    Vector2 newPoint;
    public bool RRTStep()
    {

        randomPoint = RandomPoint();
        Debug.Log($"Radom point: {randomPoint}");
        nearestNode = KDTree.NearestNeighbor(kdTree, randomPoint);

        newPoint = Steer(nearestNode.Point, randomPoint);
        Debug.Log($"New point: {newPoint}");

        if (!ObstacleInPath(nearestNode.Point, newPoint))
        { 
            kdTree.AddKDNode(newPoint);
            RRTGraph.AddNode(newPoint);
            RRTGraph.AddEdge(nearestNode.Point, newPoint);
            //nodes.Add(newNodeTransform);

            if (Vector2.Distance(newPoint, EndNode.position) < maxStepSize)
            {
                Debug.Log("Goal reached!");
                return true;
            }
        }
        return false;
    }

    private Vector2 RandomPoint()
    {
        float x = Random.Range(RandomBounds.min.x, RandomBounds.max.x);
        float y = Random.Range(RandomBounds.min.y, RandomBounds.max.y);
        return new Vector2(x, y);
    }


    private Vector2 Steer(Vector2 fromPoint, Vector2 toPoint)
    {
        Vector2 direction = (toPoint - fromPoint).normalized;
        return fromPoint + direction * maxStepSize;
    }

    private bool IsValidSegment(Vector2 start, Vector2 end) 
    {
        var hit= Physics2D.Linecast(start, end, BoundaryObstacleLayerMask);
        if(hit) 
        {
            return false;
        }
        return true;

    }
    private bool ObstacleInPath(Vector2 fromPoint, Vector2 toPoint)
    {
        return !IsValidSegment(fromPoint, toPoint);
    }
    private void OnDrawGizmosSelected()
    {
        //KDTreeVisualizer.DrawTree(kdTree);
        if (RRTGraph == null) return;
        Graph<Vector2>.DebugDrawGraph(RRTGraph, Color.black, Color.green);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(randomPoint, 0.1f);
        if(nearestNode != null) 
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(nearestNode.Point, 0.1f);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPoint, 0.1f);
    }

}
