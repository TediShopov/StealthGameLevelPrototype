using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class DiscretizeLevelToGrid : MonoBehaviour
{
    [HideInInspector] public Grid Grid;
    private Vector3Int GridMin;
    private Vector3Int GridMax;
    public LayerMask ObstacleLayerMask;
    [HideInInspector]  public List<Vector3Int> Obstacles;
    [HideInInspector]  public List<FieldOfView> EnemyFieldOfViews;
     public PolygonBoundary PolygonBoundary;
    
    // Start is called before the first frame update
    void Start()
    {
        this.Grid = GetComponent<Grid>();
        Obstacles = new List<Vector3Int>();
        if (PolygonBoundary != null) 
        {
            Bounds levelBounds = PolygonBoundary.GetComponent<PolygonCollider2D>().bounds;
            GridMin = Grid.WorldToCell(levelBounds.min);
            GridMax = Grid.WorldToCell(levelBounds.max);
        }
        this.EnemyFieldOfViews=FindObjectsOfType<FieldOfView>().ToList();

        
    }
    public void UpdateGrid()
    {
        for (int x = GridMin.x; x < GridMax.x; x++)
        {
            for (int y = GridMin.y; y < GridMax.y; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                Vector3 worldPosition = Grid.GetCellCenterWorld(cellPosition);

                if (IsObstacleAtPosition(worldPosition) || IsEnemiesVisionOnPosition(worldPosition))
                {
                    SetTile(cellPosition);
                }
//                else
//                {
//                    gridTilemap.SetTile(cellPosition, emptyTile);
//                }
            }
        }
    }
    public void SetTile(Vector3Int obstacle) 
    {
        this.Obstacles.Add(obstacle);
    }
    private bool IsEnemiesVisionOnPosition(Vector3 worldPosition) 
    {

        foreach (var fov in EnemyFieldOfViews)
        {
            if (fov.TestCollisition(worldPosition)) 
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
        
        this.Obstacles.Clear();
        UpdateGrid();
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(Grid.GetCellCenterWorld(GridMin), Grid.cellSize);
        Gizmos.DrawCube(Grid.GetCellCenterWorld(GridMax), Grid.cellSize);
        if(Obstacles.Count > 0) 
        {
            foreach (var obstacle in Obstacles) 
            {
                Gizmos.DrawCube(Grid.GetCellCenterWorld(obstacle), Grid.cellSize);
            }
        }
    }
}
