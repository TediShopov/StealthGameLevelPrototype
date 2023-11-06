using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RapidlyExploringRandomTree : MonoBehaviour
{
    public DiscretizeLevelToGrid VoxelizedLevel;
    public Transform StartNode;
    public Transform EndNode;
    public bool BuildAtBegining = true;
    public bool DoRRTStep = false;
    public float maxStepSize = 1.0f;
    public int maxIterations = 1000;
    // private List<Transform> nodes = new List<Transform>();

    private Vector3 RandomMin;
    private Vector3 RandomMax;
    private Graph<Vector3> RRTGraph;
    private KDTree kdTree;
    public float CurrentFuture;
    public int EndBias;
    private void Start()
    {
        if (VoxelizedLevel == null) return;
        this.RRTGraph = new Graph<Vector3>();
        kdTree = new KDTree(KDTree.ToFloatArray((Vector3)StartNode.position),0);
        //Set boudnaries of sampler to be inside the goemtry based on the boudns of the volxelized spaece
        RandomMin = VoxelizedLevel.Grid.GetCellCenterWorld(VoxelizedLevel.GridMin);
        RandomMin.z = 0;
        RandomMax = VoxelizedLevel.Grid.GetCellCenterWorld(VoxelizedLevel.GridMax);
        RandomMax.z = VoxelizedLevel.Iterations * maxStepSize * VoxelizedLevel.Grid.cellSize.z;
        if (BuildAtBegining)
        {
            BuildRRT();
        }
    }
    public void Update()
    {
        if(DoRRTStep) 
        {
            RRTStep(EndBias);
            DoRRTStep = false;
        }
        
    }

    private void BuildRRT()
    {
        kdTree = new KDTree(KDTree.ToFloatArray(StartNode.position),3,0);
        // nodes.Add(startNode);
        int iter = 0;
        while (iter < maxIterations) 
        {
            RRTStep(iter);
            iter++;
        }


        Debug.Log("RRT did not reach the goal.");
    }
    Vector3 target;
    Vector3 newPoint;
    KDTree nearestNode;
    public bool RRTStep(int iterCount)
    {

        if (iterCount % EndBias == 0)
        {
            target = EndNode.position;
            target.z = RandomMax.z;
        }
        else
        {
            target = RandomPoint();
        }


        Debug.Log($"Radom point: {target}");
        nearestNode = KDTree.NearestNeighbor(kdTree, KDTree.ToFloatArray(target));

        newPoint = Steer((Vector3)nearestNode, target);
        Debug.Log($"New point: {newPoint}");

        if (!ObstacleInPath((Vector3)nearestNode, newPoint))
        {
            if (nearestNode.Point[2] > newPoint.z) 
            {
                Debug.Log("Cannot run time back");
                return false;
            }
            kdTree.AddKDNode(KDTree.ToFloatArray(newPoint));
            RRTGraph.AddNode(newPoint);
            RRTGraph.AddEdge((Vector3)nearestNode, newPoint);
            //nodes.Add(newNodeTransform);

            if (Vector3.Distance(newPoint, EndNode.position) < maxStepSize)
            {
                Debug.Log("Goal reached!");
            }
            return true;
        }
        return false;
    }

    private Vector3 RandomPoint()
    {
        float x = Random.Range(RandomMin.x, RandomMax.x);
        float y = Random.Range(RandomMin.y, RandomMax.y);
        float z = Random.Range(RandomMin.z, RandomMax.z);
        return new Vector3(x, y,z);
    }


    private Vector3 Steer(Vector3 fromPoint, Vector3 toPoint)
    {
        Vector3 direction = (toPoint - fromPoint).normalized;
        return fromPoint + direction * maxStepSize;
    }

    private bool IsValidSegment(Vector3 start, Vector3 end) 
    {
        return true;
//        var hit= Physics2D.Linecast(start, end, BoundaryObstacleLayerMask);
//        if(hit) 
//        {
//            return false;
//        }
//        return true;Vector2
//
    }
    private bool ObstacleInPath(Vector2 fromPoint, Vector2 toPoint)
    {
        return !IsValidSegment(fromPoint, toPoint);
    }
    private void OnDrawGizmosSelected()
    {
        //KDTreeVisualizer.DrawTree(kdTree);
        if (RRTGraph == null) return;
        Graph<Vector3>.DebugDrawGraph(RRTGraph, Color.black, Color.green);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(target, 0.1f);
        if(nearestNode != null) 
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere((Vector2)nearestNode, 0.1f);
        }
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPoint, 0.1f);
    }

}
