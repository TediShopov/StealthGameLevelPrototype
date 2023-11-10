using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class RapidlyExploringRandomTree : MonoBehaviour
{
    public DiscretizeLevelToGrid VoxelizedLevel;
    public CharacterController2D Controller;
    public Transform StartNode;
    public Transform EndNode;
    public bool BuildAtBegining = true;
    public bool DoRRTStep = false;
    public int Runs = 1;
    public int maxIterations = 1000;
    private Vector3 lastAddedNode = Vector3.zero;
    public List<List<Vector3>> FoundPaths;
    // private List<Transform> nodes = new List<Transform>();

    private Vector3 RandomMin;
    private Vector3 RandomMax;
    Graph<Vector3> RRTGraph;
    private KDTree kdTree;
    public float CurrentFuture;
    public int EndBias;
    private void Start()
    {
        if (VoxelizedLevel == null) return;
        //Set boudnaries of sampler to be inside the goemtry based on the boudns of the volxelized spaece
        RandomMin = VoxelizedLevel.Grid.GetCellCenterWorld(VoxelizedLevel.GridMin);
        RandomMin.z = 0;
        RandomMax = VoxelizedLevel.Grid.GetCellCenterWorld(VoxelizedLevel.GridMax);
        RandomMax.z = VoxelizedLevel.Iterations * VoxelizedLevel.Step ;
        if (BuildAtBegining)
        {
           FoundPaths=  BuildManyRRTPaths();
        }
        
    }
    public List<Vector2> FlattenedPath() 
    {
       return this.RRTGraph.FindPath(StartNode.position,lastAddedNode).Select(s=> new Vector2(s.x,s.y)).ToList();
    }
    public void Update()
    {
        if(DoRRTStep) 
        {
            RRTStep(EndBias);
            DoRRTStep = false;
        }
        
    }
    public List<List<Vector3>> BuildManyRRTPaths() 
    {
        var paths = new List<List<Vector3>>();
        for (int i = 0;i < Runs;i++) 
        {
            var path = BuildRRTPath();
            if (path.Count > 0) 
            {
                paths.Add(BuildRRTPath());
            }
        }
        return paths;
    }

    private List<Vector3> BuildRRTPath()
    {
        this.RRTGraph = new Graph<Vector3>();
        kdTree = new KDTree(KDTree.ToFloatArray(StartNode.position),3,0);
        RRTGraph.AddNode(StartNode.position);
        int iter = 0;
        bool reachedGoal = false;
        while (iter < maxIterations) 
        {
            if (RRTStep(iter)) 
            {
                reachedGoal = true;
                return this.RRTGraph.FindPath(StartNode.position,lastAddedNode);
            }
            iter++;
        }
        return new List<Vector3>();
    }
    Vector3 target;
    Vector3 newPoint;
    KDTree nearestNode;
    public bool RRTStep(int iterCount)
    {

        if (iterCount % EndBias == 0)
        {
            target = GetRandomGoalState();
        }
        else
        {
            target = RandomPoint();
        }

        nearestNode = KDTree.NearestNeighbor(kdTree, KDTree.ToFloatArray(target));
        newPoint = Steer((Vector3)nearestNode, target);

        if (IsValidSegment((Vector3)nearestNode, newPoint))
        {
            if (nearestNode.Point[2] > newPoint.z) 
            {
                Debug.Log("Cannot run time back");
                return false;
            }
            kdTree.AddKDNode(KDTree.ToFloatArray(newPoint));
            RRTGraph.AddNode(newPoint);
            RRTGraph.AddEdge((Vector3)nearestNode, newPoint);
            lastAddedNode = newPoint;
            //nodes.Add(newNodeTransform);

            if (ReachedGoal(newPoint, EndNode.position, 1.0f))
            {
                return true;
            }
        }
        return false;
    }
    private Vector3 GetRandomGoalState() 
    {
        float d = Vector2.Distance(StartNode.position, EndNode.position);
        float minimumTimeToReach = d / (float)Controller.MaxSpeed;
        float z = UnityEngine.Random.Range(minimumTimeToReach, RandomMax.z);
        return new Vector3(EndNode.position.x, EndNode.position.y, z);
    }
    private bool ReachedGoal(Vector3 from, Vector3 goal, float bias) 
    {
        return Vector2.Distance(from, goal) < bias;
    }

    private Vector3 RandomPoint()
    {
        Vector2 goalSubState = new Vector2(UnityEngine.Random.Range(RandomMin.x, RandomMax.x), UnityEngine.Random.Range(RandomMin.y, RandomMax.y));
        float d = Vector2.Distance(StartNode.position, goalSubState);
        float minimumTimeToReach = d / Controller.MaxSpeed;
        float z = UnityEngine.Random.Range(minimumTimeToReach, RandomMax.z);
        return new Vector3(goalSubState.x, goalSubState.y, z);
//        float x = UnityEngine.Random.Range(RandomMin.x, RandomMax.x);
//        float y = UnityEngine.Random.Range(RandomMin.y, RandomMax.y);
//        float z = UnityEngine.Random.Range(RandomMin.z, RandomMax.z);
//        return new Vector3(x, y,z);
    }


    private Vector3 Steer(Vector3 fromPoint, Vector3 toPoint)
    {
        Vector2 direction = (toPoint - fromPoint).normalized;
        float distanceToGoal = Vector2.Distance(fromPoint, toPoint); 
        float timePassed= toPoint.z - fromPoint.z;
        if (distanceToGoal / timePassed <= Controller.MaxSpeed) 
        {
            return new Vector3(toPoint.x, toPoint.y, toPoint.z);
        }
        else
        {

            Vector2 reachedPosition = new Vector2(fromPoint.x, fromPoint.y) + direction * (Controller.MaxSpeed * timePassed); 
            return new Vector3(reachedPosition.x,reachedPosition.y, fromPoint.z + timePassed);

        }
    }

    private bool IsValidSegment(Vector3 start, Vector3 end) 
    {

        if(end.z < start.z) return false;
        Vector2Int startCell = (Vector2Int)this.VoxelizedLevel.Grid.WorldToCell(start);
        Vector2Int endCell = (Vector2Int)this.VoxelizedLevel.Grid.WorldToCell(end);
        var listOfRCells = DiscretizeLevelToGrid.GetCellsInLine(startCell, endCell);
        return !VoxelizedLevel.CheckCellsColliding(listOfRCells.ToList(), start.z, end.z);
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
    public void DebugDrawGraph( Func<Vector3,bool> condition,Color nodeColor , Color connectionColor ) 
    {
        if(RRTGraph == null) return;
        foreach (var node in RRTGraph.adjacencyList)
        {
            if (!condition(node.Key))
                continue;
            Gizmos.color = nodeColor;
            Gizmos.DrawSphere(node.Key, 0.1f);
            foreach (var connection in node.Value)
            {
                Gizmos.color = connectionColor;
                Gizmos.DrawLine(node.Key, connection);
            }
        }
    }

}
