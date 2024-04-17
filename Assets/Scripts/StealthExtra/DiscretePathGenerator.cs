using EnemyPathingStrategies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Profiling;

namespace EnemyPathingStrategies
{
    public class EnemyPathingStategy
    {
        public virtual Vector2 Choose(EnemyStrategyPathComposer pathGenerator, IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;
            return input
                .OrderBy(x => pathGenerator.Path.Contains(x))
                .First();
        }
    }

    public class PathLengthStrategy : EnemyPathingStategy
    {
        private bool longest;

        public PathLengthStrategy(bool longest = true)
        {
            this.longest = longest;
        }

        public override Vector2 Choose(EnemyStrategyPathComposer pathGenerator, IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;
            input = input
                .OrderBy(x => pathGenerator.Path.Contains(x));

            if (longest)
                input = input.ThenBy(x => Vector2.Distance(prev, x));
            else
                input = input.ThenByDescending(x => Vector2.Distance(prev, x));
            return input.First();
        }
    }

    public class LocalDirectionStrategy : EnemyPathingStategy
    {
        private bool IsLeft;

        public LocalDirectionStrategy(bool left)
        {
            this.IsLeft = left;
        }

        public override Vector2 Choose(EnemyStrategyPathComposer pathGenerator, IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;

            input = input
                .OrderBy(x => pathGenerator.Path.Contains(x));
            if (IsLeft)
                input = input.ThenBy(x => Vector2.SignedAngle(prev, x));
            else
                input = input.ThenByDescending(x => Vector2.SignedAngle(prev, x));
            return input.First();
        }
    }
}

public class EnemyStrategyPathComposer
{
    public Graph<Vector2> RoadMap;

    public List<Vector2> Path;
    public bool finished = false;
    public int TargetDecisions = 3;
    public int MadeDecisions = 0;
    public int CurrentStrategyIndex = 0;
    public Vector2 Last => Path[Path.Count - 1];

    public EnemyStrategyPathComposer(Graph<Vector2> roadmap, int len, List<EnemyPathingStategy> strats)
    {
        if (strats.Count == 0) throw new ArgumentException("Enemy path picking strategies cannot be null");
        RoadMap = roadmap;
        this.PathingStategies = strats;
    }

    private List<EnemyPathingStategy> PathingStategies;

    public List<Vector2> ComposePath(Vector2 from)
    {
        Path = new List<Vector2>() { from };
        while (finished == false)
        {
            IEnumerable<Vector2> input = RoadMap.GetNeighbors(Last);
            Progress(input);
        }
        return Path;
    }

    public void Progress(IEnumerable<Vector2> input)
    {
        if (input.Count() == 0)
        { finished = true; return; }
        else if (input.Count() == 1)
        {
            if (Path.Contains(input.First()))
            {
                finished = true;
                return;
            }
            else
            {
                Path.Add(input.First());
            }
        }
        else
        {
            Path.Add(PathingStategies[CurrentStrategyIndex].Choose(this, input.OrderBy(x => x.x)));
            CurrentStrategyIndex++;
            if (CurrentStrategyIndex >= PathingStategies.Count)
                CurrentStrategyIndex = 0;
            MadeDecisions++;
        }
        if (MadeDecisions >= TargetDecisions)
        {
            finished = true; return;
        }

        if (Path.Count > 1 && Path[Path.Count - 1] == Path[0])
        {
            //TODO cyclic path
            finished = true; return;
        }
    }
}

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
                (float)Chromosome.GetGene(geneIndex).Value * Stategies.Count - 1);
            EnemyPathingStategy enemyPathingStategy = Stategies[stratIndex];
            localStrats.Add(enemyPathingStategy);
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