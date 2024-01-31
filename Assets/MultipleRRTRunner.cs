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

    public void Run()
    {
        for (int i = 0; i < Runs; i++)
        {
            var RRT = Instantiate(RRTPrefab, this.transform);
            var rrtVisualizer = RRT.GetComponent<RapidlyExploringRandomTreeVisualizer>();
            rrtVisualizer.Setup();
            rrtVisualizer.Run();
        }
    }
}