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
       public int maxIterations = 1000;
       public float GoalDistance = 1.0f;
       public float BiasDistance = 25.0f;
    public List<Vector3> Path;
    IRapidlyEpxploringRandomTree<Vector3> RRT;

    private void Start()
    {
        Run();
    }
    public void Run() 
    {
        if (VoxelizedLevel == null) return;
        RRT = new DiscreteDistanceBasedRRTSolver(VoxelizedLevel, BiasDistance, GoalDistance, Controller.MaxSpeed);
        RRT.Run(StartNode.position, EndNode.position, maxIterations);
        Path = RRT.ReconstructPathToSolution();
    }
    public void Update()
    {
        
    }

    private void OnDrawGizmosSelected()
    {

        //Draw whole tree 
        Gizmos.color = Color.black;
        DFSDraw(this.RRT.StartNode);
        //Draw correct path on top so it is visible
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(EndNode.position, BiasDistance);
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Gizmos.DrawSphere(Path[i], 0.1f);
            Gizmos.DrawLine(Path[i], Path[i + 1]);
        }

    }
    public void DFSDraw(TreeNode<Vector3> node) 
    {
        if (node == null) return;
        Gizmos.DrawSphere(node.Content, 0.1f);
        foreach (var child in node.Children)
        {
            Gizmos.DrawLine(node.Content, child.Content);
            DFSDraw(child);
        }
    }
}
