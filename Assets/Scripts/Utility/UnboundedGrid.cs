using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnboundedGrid
{
    [SerializeField]
    public Vector2 Origin;

    [SerializeField]
    public float cellSize;

    public UnboundedGrid(Vector2 origin, float cellSize)
    {
        Origin = origin;
        this.cellSize = cellSize;
    }

    public UnboundedGrid(UnboundedGrid other)
    {
        this.Origin = other.Origin;
        this.cellSize = other.cellSize;
    }

    public UnboundedGrid(Grid grid)
    {
        Origin = grid.transform.position;
        this.cellSize = grid.cellSize.x;
    }

    // Converts grid coordinates to world coordinates
    public Vector3 GetCellCenterWorld(Vector3Int coord)
    {
        float worldX = Origin.x + coord.x * cellSize;
        float worldY = Origin.y + coord.y * cellSize;
        return new Vector2(worldX, worldY);
    }

    // Converts grid coordinates to world coordinates
    public Vector3 GetCellCenterWorld(int gridX, int gridY)
    {
        float worldX = Origin.x + gridX * cellSize;
        float worldY = Origin.y + gridY * cellSize;
        return new Vector2(worldX, worldY);
    }

    // Converts world coordinates to grid coordinates
    public Vector3Int WorldToCell(Vector2 worldPos)
    {
        int gridX = Mathf.FloorToInt((worldPos.x - Origin.x) / cellSize);
        int gridY = Mathf.FloorToInt((worldPos.y - Origin.y) / cellSize);
        return new Vector3Int(gridX, gridY, 0);
    }

    // Checks if a given world position is within certain bounds
    public bool IsWithinGrid(Vector2 worldPos, Vector2 bounds)
    {
        return (worldPos.x >= Origin.x && worldPos.x <= Origin.x + bounds.x * cellSize) &&
               (worldPos.y >= Origin.y && worldPos.y <= Origin.y + bounds.y * cellSize);
    }

    // Computes the Euclidean distance between two grid points
    public float DistanceBetween(Vector2Int gridPos1, Vector2Int gridPos2)
    {
        return Vector2Int.Distance(gridPos1, gridPos2);
    }
}