using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
public class RapidlyExploringRandomTreeVisualizer : MonoBehaviour
{
    public DiscretizeLevelToGrid VoxelizedLevel;
    public CharacterController2D Controller;
    public Transform StartNode;
    public Transform EndNode;
    public int Runs = 1;
    public int maxIterations = 1000;
    public float GoalDistance = 1.0f;
    public float BiasDistance = 25.0f;
    public List<List<Vector3>> FoundPaths;
    public List<IRapidlyEpxploringRandomTree<Vector3>> RCSolvers;

    private void Start()
    {
        if (VoxelizedLevel == null) return;
        RCSolvers=new List<IRapidlyEpxploringRandomTree<Vector3>>();
        FoundPaths = new List<List<Vector3>>();
        for (int i = 0; i < Runs; i++) 
        {
            RCSolvers.Add(new DiscreteDistanceBasedRRTSolver(VoxelizedLevel, BiasDistance, GoalDistance, Controller.MaxSpeed));
        }
        foreach (var rrtSolver in RCSolvers)
        {
            rrtSolver.Run(StartNode.position, EndNode.position, maxIterations);
            FoundPaths.Add(rrtSolver.ReconstructPathToSolution());
        }

    }
    public void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {
        //KDTreeVisualizer.DrawTree(kdTree);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(EndNode.position, BiasDistance);
        
        foreach (var path in FoundPaths)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawSphere(path[i], 0.1f);
                Gizmos.DrawLine(path[i], path[i+1]);
            }

        }
    }
//    public void DebugDrawGraph( Func<Vector3,bool> condition,Color nodeColor , Color connectionColor ) 
//    {
//        if(RRTGraph == null) return;
//        foreach (var node in RRTGraph.adjacencyList)
//        {
//            if (!condition(node.Key))
//                continue;
//            Gizmos.color = nodeColor;
//            Gizmos.DrawSphere(node.Key, 0.1f);
//            foreach (var connection in node.Value)
//            {
//                Gizmos.color = connectionColor;
//                Gizmos.DrawLine(node.Key, connection);
//            }
//        }
//    }

}
