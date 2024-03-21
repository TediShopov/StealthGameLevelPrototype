using System.Collections;
using System.Collections.Generic;
using EnemyPathingStrategies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyPathPhenotype
{
    // A Test behaves as an ordinary method
    [Test]
    public void StraightLine_PreffrerUnvisitedNodes()
    {
        Vector2 A = new Vector2(0, 0);
        Vector2 B = new Vector2(1, 0);
        Vector2 C = new Vector2(2, 0);
        Graph<Vector2> graph = new Graph<Vector2>();
        graph.AddNode(A);
        graph.AddNode(B);
        graph.AddNode(C);

        graph.AddEdge(A, B);
        graph.AddEdge(B, C);

        EnemyStrategyPathComposer pathComposer = new
            EnemyStrategyPathComposer(
            graph,
            2,
            new List<EnemyPathingStrategies.EnemyPathingStategy>()
            {
                new LocalDirectionStrategy(true)
            });

        List<Vector2> actual = pathComposer.ComposePath(A);
        Assert.IsTrue(actual.Count == 3);
        // Use the Assert class to test conditions
    }
}