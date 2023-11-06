using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class DiscretizeLevelToGrid : MonoBehaviour
{
    [HideInInspector] public Grid Grid;
    public Vector3Int GridMin;
    public Vector3Int GridMax;
    public LayerMask ObstacleLayerMask;
    [HideInInspector]  public List<PatrolPath> PatrolPaths;
     public PolygonBoundary PolygonBoundary;
    public float Step;
    public float Iterations;
    public int LookAtGrid = 0;
    public int LookAtRange ;
    public List<bool[,]> FutureGrids;
    public bool EnableDebugCamera;
    public Camera MainCamera;
    public Camera DebugCamera;
    //public float Future;
    // Start is called before the first frame update
    void Start()
    {
        
        this.Grid = GetComponent<Grid>();
        if (PolygonBoundary != null) 
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            GridMin = Grid.WorldToCell(levelBounds.min);
            GridMax = Grid.WorldToCell(levelBounds.max);
        }
        PatrolPaths = FindObjectsOfType<PatrolPath>().ToList();
        FutureGrids = new List<bool[,]>();
        for (int i = 0;i<Iterations;i++) 
        {
            var grid = GetFutureGrid(i * Step);
            FutureGrids.Add(grid);
        }
        Debug.Log($"Future Grid Count");

        
    }
    
    public bool[,] GetFutureGrid(float future) 
    {

        int rows = GridMax.y - GridMin.y;
        int cols = GridMax.x - GridMin.x;
        var futureGrid = new bool[rows,cols];
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                Vector3Int cellPosition = new Vector3Int(col+GridMin.x, row+GridMin.y, 0);
                Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);
                if (IsObstacleAtPosition(worldPosition) || IsEnemiesVisionOnPosition(worldPosition,future ))
                {
                    futureGrid[row,col] = true;
                }
            }
        }
        return futureGrid;
    }
    private bool IsEnemiesVisionOnPosition(Vector3 worldPosition,float future) 
    {

        foreach (var patrolPath in PatrolPaths)
        {
            var positionAndDirection = patrolPath.CalculateFuturePosition(future);
            if (patrolPath.FieldOfView.TestCollision(worldPosition,positionAndDirection.Item1,positionAndDirection.Item2)) 
            {
                return true;
            }

        }
        return false;
    }

    private bool IsObstacleAtPosition(Vector3 worldPosition)
    {
        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        Vector2 halfBoxSize = Grid.cellSize * 0.5f;

        // Perform a BoxCast to check for obstacles in the area
        RaycastHit2D hit = Physics2D.BoxCast(
            origin: position2D,
            size: halfBoxSize,
            angle: 0f,
            direction: Vector2.zero,
            distance: 0.01f,
            layerMask: ObstacleLayerMask
        );

        return hit.collider != null;
    }

    // Update is called once per frame
    void Update()
    {

        if (EnableDebugCamera) 
        {
            MainCamera.enabled = false;
            DebugCamera.enabled = true;
            DebugCamera.transform.LookAt(new Vector3(0,0,0),Vector3.back);
        }
        else
        {

            MainCamera.enabled = true;
            DebugCamera.enabled = false;
        }

    }
    private void OnDrawGizmosSelected()
    {
        if (FutureGrids == null) return;
        LookAtGrid = Mathf.Clamp(LookAtGrid, 0, FutureGrids.Count-1);
        
        int rows = GridMax.y - GridMin.y;
        int cols = GridMax.x - GridMin.x;
        Gizmos.color = Color.blue;
        for (int i = LookAtGrid-LookAtRange; i < LookAtGrid+LookAtRange; i++)
        {
            var lookAtCurrent = i;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (FutureGrids[lookAtCurrent][row, col])
                    {
                        Vector3Int cellPosition = new Vector3Int(col + GridMin.x, row + GridMin.y, 0);
                        Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);

                        worldPosition.z = lookAtCurrent * Step;
                        Vector3 cellsize = Grid.cellSize;
                        cellsize.z = Step;
                        Gizmos.DrawCube(worldPosition, Grid.cellSize);
                    }
                }
            }

        }
        //        //Draw future positions
        //        foreach (var enemyPath in PatrolPaths) 
        //        {
        //            Vector2 futurePos = enemyPath.CalculateFuturePosition(Future).Item1;
        //            Gizmos.DrawSphere(futurePos,0.1f);
        //        }
    }
}
