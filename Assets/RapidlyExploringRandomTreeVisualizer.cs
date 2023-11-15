using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEditor;
using UnityEngine;
public class RapidlyExploringRandomTreeVisualizer : MonoBehaviour
{
       public VoxelizedLevelBase VoxelizedLevel;
       public CharacterController2D Controller;
       public Transform StartNode;
       public Transform EndNode;
       public int maxIterations = 1000;
       public float GoalDistance = 1.0f;
       public float BiasDistance = 25.0f;
    public List<Vector3> Path=new List<Vector3>();
    public bool OutputDiscretized = false;
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

        //Do not draw anything as algorithm has not been stared
        if (RRT == null) return;
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
            Handles.Label(Path[i], $"{Path[i].z.ToString("0.00")}");
            Handles.Label(Path[i] + Vector3.down*0.2f, $"{VoxelizedLevel.GetFutureLevelIndex(Path[i].z)}");
            if (OutputDiscretized)
            {

                Vector2Int startCell = (Vector2Int)this.VoxelizedLevel.Grid.WorldToCell(Path[i+1]);
                Vector2Int endCell = (Vector2Int)this.VoxelizedLevel.Grid.WorldToCell(Path[i]);
                var listOfRCells = DiscretizeLevelToGrid.GetCellsInLine(startCell, endCell);
                bool collided = VoxelizedLevel.CheckCellsColliding(listOfRCells.ToList(), Path[i+1].z, Path[i].z);
                if (collided)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
                foreach (var cell in listOfRCells)
                {
                    Gizmos.DrawSphere(VoxelizedLevel.Grid.GetCellCenterWorld(new Vector3Int(cell.x,cell.y,0)), 0.1f);
                }
            }
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
