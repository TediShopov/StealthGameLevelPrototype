using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FutureDebugVisualization : MonoBehaviour
{
    public DiscretizeLevelToGrid VoxelizedLevel;
    public RapidlyExploringRandomTree RRT;
    public float Future;
    public float FutureBias;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDrawGizmos()
    {

        if (Future < 0) { Future = 0; }
        if (FutureBias < 0) { FutureBias = 0; }

        if (VoxelizedLevel == null || VoxelizedLevel.FutureGrids.Count == 0) return;
        int lookAtGridIndex = Mathf.CeilToInt(Future / VoxelizedLevel.Step);
        bool[,] LookAtGrid = VoxelizedLevel.FutureGrids[lookAtGridIndex];
        VoxelizedLevel.DebugDrawGridByIndex(lookAtGridIndex);
        RRT.DebugDrawGraph(RRTNodeCloseToFuture, Color.green, Color.black);

    }
    public bool RRTNodeCloseToFuture(Vector3 nodePoint) 
    {
        return (Future - FutureBias < nodePoint.z) && (Future + FutureBias > nodePoint.z);
    }
}
