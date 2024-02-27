using System.Collections.Generic;
using UnityEngine;

public abstract class DynamicObstacleDiscretizer : MonoBehaviour
{
    abstract public List<Vector3Int> GetPossibleAffectedCells(Grid grid, float future);
    abstract public bool IsObstacle(Vector3 position, float future); 
    
}
