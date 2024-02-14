using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BacktrackPatrolPathSequentialPerfrormance : MonoBehaviour
{
    public int RandomSeed = 0;

    // Start is called before the first frame update
    private void Start()
    {
        var testPatrol = GetRandomPatrol();
        RecalculatingPerformance(testPatrol, 500, 1, 1000);
        IncrementalPerformance(testPatrol, 500, 1, 1000);
        RecalculatingPerformance(testPatrol, 2000, 1, 1000);
        IncrementalPerformance(testPatrol, 2000, 1, 1000);
        RecalculatingPerformance(testPatrol, 3000, 1, 1000);
        IncrementalPerformance(testPatrol, 3000, 1, 1000);
    }

    private BacktrackPatrolPath GetRandomPatrol()
    {
        System.Random rnd = new System.Random(RandomSeed);

        //Initialize a list of random vector 2 points
        List<Vector2> randomPath = new List<Vector2>();
        for (int i = 0; i < 10; i++)
        {
            randomPath.Add(new Vector2(Helpers.GetRandomFloat(rnd, 0, 5), Helpers.GetRandomFloat(rnd, 0, 5)));
        }
        return new BacktrackPatrolPath(randomPath);
    }

    private float IncrementalPerformance(BacktrackPatrolPath path, float from, float step, float stepCount)
    {
        return Helpers.LogExecutionTime(() =>
        {
            path.MoveAlong(from);
            for (int i = 0; i < stepCount; i++)
            {
                path.MoveAlong(i * step);
            }
        },
            $"Incremental Patrol Position Time: {from} {step}");
    }

    private float RecalculatingPerformance(BacktrackPatrolPath path, float from, float step, float stepCount)
    {
        return Helpers.LogExecutionTime(() =>
        {
            for (int i = 0; i < stepCount; i++)
            {
                var copyPath = new BacktrackPatrolPath(path);
                copyPath.MoveAlong(from + i * step);
            }
        },
            $"Recalculated Patrol Position Time: {from} {step}");
    }

    // Update is called once per frame
    private void Update()
    {
    }
}