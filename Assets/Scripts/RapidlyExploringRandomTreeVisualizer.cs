using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class RapidlyExploringRandomTreeVisualizer : MonoBehaviour
{
    public IFutureLevel VoxelizedLevel;
    public CharacterController2D Controller;
    public GameObject StartNode;
    public GameObject EndNode;
    public int maxIterations = 1000;
    public float GoalDistance = 1.0f;
    public float BiasDistance = 25.0f;
    public List<Vector3> Path = new List<Vector3>();
    public bool OutputDiscretized = false;
    public IRapidlyEpxploringRandomTree<Vector3> RRT;
    private GameObject level;

    public void Setup()
    {
        level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        if (level == null) return;
        EndNode = Helpers.SafeGetComponentInChildren<WinTrigger>(level).gameObject;
        //VoxelizedLevel = Helpers.SafeGetComponentInChildren<VoxelizedLevelBase>(level);
        VoxelizedLevel = level.GetComponentInChildren<IFutureLevel>(false);
        StartNode = Helpers.SafeGetComponentInChildren<CharacterController2D>(level).gameObject;
        Controller = Helpers.SafeGetComponentInChildren<CharacterController2D>(level);
    }

    public void Run()
    {
        Profiler.BeginSample("RRT Run");
        if (VoxelizedLevel == null) return;
        RRT = new DiscreteDistanceBasedRRTSolver(VoxelizedLevel, BiasDistance, GoalDistance, Controller.MaxSpeed);

        RRT.Run(StartNode.transform.position, EndNode.transform.position, maxIterations);
        Path = RRT.ReconstructPathToSolution();
        Profiler.EndSample();
    }

    public void Update()
    {
        //EndNode = level.GetComponentInChildren<WinTrigger>().gameObject;
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
        Gizmos.DrawWireSphere(this.RRT.Goal, BiasDistance);
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Gizmos.DrawSphere(Path[i], 0.1f);
            Gizmos.DrawLine(Path[i], Path[i + 1]);
            Handles.Label(Path[i], $"{Path[i].z.ToString("0.00")}");
            //Handles.Label(Path[i] + Vector3.down * 0.2f, $"{(Path[i].z)}");
            if (OutputDiscretized)
            {
                bool collided = VoxelizedLevel.IsColliding(Path[i], Path[i + 1], Path[i].z, Path[i + 1].z);
                if (collided)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;

                if (VoxelizedLevel is VoxelizedLevel)
                {
                    VoxelizedLevel v = (VoxelizedLevel)VoxelizedLevel;
                    Handles.Label(Path[i] + Vector3.down * 0.2f, $"{v.GetFutureLevelIndex(Path[i].z)}");
                    Vector2Int startCell = (Vector2Int)v.Grid.WorldToCell(Path[i]);
                    Vector2Int endCell = (Vector2Int)v.Grid.WorldToCell(Path[i + 1]);
                    var listOfRCells = VoxelizedLevelBase.GetCellsInLine(startCell, endCell);
                    foreach (var cell in listOfRCells)
                    {
                        Gizmos.DrawSphere(v.Grid.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0)), 0.1f);
                    }
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