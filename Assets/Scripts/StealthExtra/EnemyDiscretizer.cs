using System.Collections.Generic;

using UnityEngine;

[RequireComponent(typeof(PatrolPath))]
public class EnemyDiscretizer : DynamicObstacleDiscretizer
{
    private PatrolPath _path;

    public override List<Vector3Int> GetPossibleAffectedCells(Grid grid, float future)
    {
        _path = GetComponent<PatrolPath>();
        var toReturn = new List<Vector3Int>();

        var position = _path.CalculateFuturePosition(future).Item1;
        var direction = _path.CalculateFuturePosition(future).Item2;
        Bounds bounds = new Bounds();
        bounds.center = position;
        //        bounds.center = position + direction * path.EnemyProperties.ViewDistance/2.0f;
        //        bounds.Expand(path.EnemyProperties.ViewDistance*2.0f);

        Vector2 minLeft = position + Vector2.Perpendicular(direction) * _path.EnemyProperties.ViewDistance;
        Vector2 maxRight = position + Vector2.Perpendicular(-direction) * _path.EnemyProperties.ViewDistance;
        maxRight += direction * _path.EnemyProperties.ViewDistance;
        bounds.Encapsulate(minLeft);
        bounds.Encapsulate(maxRight);

        Vector3Int min = grid.WorldToCell(bounds.min);
        Vector3Int max = grid.WorldToCell(bounds.max);
        for (int row = min.y; row < max.y; row++)
        {
            for (int col = min.x; col < max.x; col++)
            {
                toReturn.Add(new Vector3Int(col, row, 0));
            }
        }
        return toReturn;
    }

    public static List<Vector3Int> GetPossibleAffectedCells(PatrolPath path, Grid grid, float future)
    {
        var toReturn = new List<Vector3Int>();

        Bounds bounds = new Bounds();
        FutureTransform ft = path.GetFutureTransform(future);
        Vector2 minLeft = ft.Position + Vector2.Perpendicular(ft.Direction) * path.EnemyProperties.ViewDistance;
        Vector2 maxRight = ft.Position + Vector2.Perpendicular(-ft.Direction) * path.EnemyProperties.ViewDistance;
        maxRight += ft.Direction * path.EnemyProperties.ViewDistance;
        bounds.Encapsulate(minLeft);
        bounds.Encapsulate(maxRight);
        Vector3Int min = grid.WorldToCell(bounds.min);
        Vector3Int max = grid.WorldToCell(bounds.max);
        for (int row = min.y; row < max.y; row++)
            for (int col = min.x; col < max.x; col++)
                toReturn.Add(new Vector3Int(col, row, 0));
        return toReturn;
    }

    public override bool IsObstacle(Vector3 position, float future)
    {
        var positionDirecion = _path.CalculateFuturePosition(future);
        return _path.FieldOfView.TestCollision(position, positionDirecion.Item1, positionDirecion.Item2);
    }
}