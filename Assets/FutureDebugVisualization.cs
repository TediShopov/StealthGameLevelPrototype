using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FutureDebugVisualization : MonoBehaviour
{
    public DiscretizeLevelToGrid VoxelizedLevel;
    public RapidlyExploringRandomTree RRT;
    public float Future;
    public float FutureBias;
//    public Vector2Int StartRLine = Vector2Int.zero;
//    public Vector2Int EndRLine = Vector2Int.zero;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void DrawFlattenedFoundPath() 
    {

        Gizmos.color = Color.yellow;
        foreach (var path in this.RRT.FoundPaths) 
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (RRTNodeCloseToFuture(path[i])) 
                {
                    Vector2 t = path[i];
                    Gizmos.DrawSphere(t, 0.2f);
                    //Draw path 
                    Vector2 t1 = path[i + 1];
                    Gizmos.DrawLine(t1, t);
                }
            }
        }
    }
    private void OnDrawGizmos()
    {

        //Debug.DrawRay(RRT.StartNode.position, Vector3.right * RRT.Controller.MaxSpeed, Color.cyan);
        if (Future < 0) { Future = 0; }
        if (FutureBias < 0) { FutureBias = 0; }

        if (VoxelizedLevel == null || VoxelizedLevel.FutureGrids.Count == 0) return;
        int lookAtGridIndex = Mathf.CeilToInt(Future / VoxelizedLevel.Step);
        bool[,] LookAtGrid = VoxelizedLevel.FutureGrids[lookAtGridIndex];
        VoxelizedLevel.DebugDrawGridByIndex(lookAtGridIndex);
        RRT.DebugDrawGraph(RRTNodeCloseToFuture, Color.green, Color.black);
        DrawFlattenedFoundPath();





//        var listOfRCells = DiscretizeLevelToGrid.GetCellsInLine(StartRLine, EndRLine);
//        if (VoxelizedLevel.CheckCellsColliding(listOfRCells.ToList(), Future, Future+VoxelizedLevel.Step))
//        {
//            Gizmos.color = Color.red;
//        }
//        else 
//        {
//            Gizmos.color = Color.blue;
//        }
//        foreach (var cell in listOfRCells) 
//        {
//            Gizmos.DrawSphere(VoxelizedLevel.Grid.GetCellCenterWorld(new Vector3Int(cell.x,cell.y,0)), 0.5f);
//        }

    }
    public bool RRTNodeCloseToFuture(Vector3 nodePoint) 
    {
        return (Future - FutureBias < nodePoint.z) && (Future + FutureBias > nodePoint.z);
    }
}
