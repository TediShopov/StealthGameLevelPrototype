using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultipleRRTRunner : MonoBehaviour
{
    //    public DiscretizeLevelToGrid VoxelizedLevel;
    //    public CharacterController2D Controller;
    //    public Transform StartNode;
    //    public Transform EndNode;
    public int Runs = 1;
    //    public int maxIterations = 1000;
    //    public float GoalDistance = 1.0f;
    //    public float BiasDistance = 25.0f;
    public GameObject RRTPrefab;
    public List<List<Vector3>> FoundPaths;

    private void Start()
    {
        for (int i = 0; i < Runs; i++) 
        {
            Instantiate(RRTPrefab, this.transform);
            var rrtVisualizer = RRTPrefab.GetComponent<RapidlyExploringRandomTreeVisualizer>();
        }

    }
    public void Update()
    {
        
    }

//    public void DebugDrawGraph( Func<Vector3,bool> condition,Color nodeColor , Color connectionColor ) 
//    {
//        if(RRTGraph == null) return;
//        foreach (var node in RRTGraph.adjacencyList)
//        {
//            if (!condition(node.Key))
//                continue;
//            Gizmos.color = nodeColor;
//            Gizmos.DrawSphere(node.Key, 0.1f);
//            foreach (var connection in node.Value)
//            {
//                Gizmos.color = connectionColor;
//                Gizmos.DrawLine(node.Key, connection);
//            }
//        }
//    }

}
