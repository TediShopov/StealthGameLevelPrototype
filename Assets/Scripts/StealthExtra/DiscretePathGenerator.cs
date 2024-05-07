using EnemyPathingStrategies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Profiling;

//Static class containing list of enemy pathing strategies

public class DiscretePathGenerator : MonoBehaviour, IPathGenerator
{
    public Graph<Vector2> Roadmap { get; set; }
    public System.Random LevelRandom { get; set; }
    public LevelChromosomeBase Chromosome;
    [HideInInspector] public int geneIndex = 0;

    public List<EnemyPathingStategy> Stategies = new List<EnemyPathingStategy>()
    {
        new PathLengthStrategy(true),
        new PathLengthStrategy(false),
        new LocalDirectionStrategy(true),
        new LocalDirectionStrategy(false)
    };

    public List<Vector2> GeneratePath()
    {
        if (geneIndex >= Chromosome.Length - 4)
        {
            int a = 3;
        }
        int decisionLength = Mathf.CeilToInt(
        Mathf.Lerp(1, 4, (float)Chromosome.GetGene(geneIndex).Value));

        EnemyStrategyPathComposer pathComposer = null;
        var localStrats = new List<EnemyPathingStategy>();
        for (int i = 0; i < 4; i++)
        {
            int stratIndex = (int)(
                (float)Chromosome.GetGene(geneIndex).Value * (Stategies.Count - 1));
            try
            {
                EnemyPathingStategy enemyPathingStategy = Stategies[stratIndex];
                localStrats.Add(enemyPathingStategy);
            }
            catch (Exception)
            {
                Debug.LogWarning($"_DEB_ {(float)Chromosome.GetGene(geneIndex).Value} {stratIndex}");

                throw;
            }
            geneIndex++;
        }
        int randomNodeIndex = LevelRandom.Next(0, Roadmap.adjacencyList.Count - 1);
        var keyList = this.Roadmap.adjacencyList.Keys.ToArray();
        Vector2 startingNode = keyList[randomNodeIndex];

        pathComposer = new EnemyStrategyPathComposer(Roadmap, decisionLength, localStrats);
        return pathComposer.ComposePath(startingNode);
    }

    public List<List<Vector2>> GeneratePaths(int pathsToGenerate)
    {
        if (Roadmap == null || Roadmap.adjacencyList.Count <= 0) return
                new List<List<Vector2>>()
                {
                    new List<Vector2>(),
                    new List<Vector2>(),
                    new List<Vector2>()
                };

        Profiler.BeginSample("New Enemy path generation");
        var keyValueList = this.Roadmap.adjacencyList.Keys.ToArray();

        List<List<Vector2>> pathsToReturn = new List<List<Vector2>>();

        var visited = new HashSet<Vector2>();
        for (int i = 0; i < pathsToGenerate; i++)
        {
            pathsToReturn.Add(GeneratePath());
        }
        Profiler.EndSample();
        return pathsToReturn;
    }

    public void Init(GameObject To)
    {
        var level = Helpers.SearchForTagUpHierarchy(To.gameObject, "Level");
        Chromosome = level.GetComponentInChildren<LevelChromosomeMono>().Chromosome;
    }
}