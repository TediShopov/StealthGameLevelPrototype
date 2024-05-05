using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EnemyPathingStrategies
{
    public class EnemyPathingStategy
    {
        public virtual Vector2 Choose(
            EnemyStrategyPathComposer pathGenerator,
            IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;
            return input
                .OrderBy(x => pathGenerator.Path.Contains(x))
                .First();
        }
    }

    public class LocalDirectionStrategy : EnemyPathingStategy
    {
        private bool _isLeft;

        public LocalDirectionStrategy(bool left)
        {
            this._isLeft = left;
        }

        public override Vector2 Choose(
            EnemyStrategyPathComposer pathGenerator,
            IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;

            input = input
                .OrderBy(x => pathGenerator.Path.Contains(x));
            if (_isLeft)
                input = input.ThenBy(x => Vector2.SignedAngle(prev, x));
            else
                input = input.ThenByDescending(x => Vector2.SignedAngle(prev, x));
            return input.First();
        }
    }

    public class PathLengthStrategy : EnemyPathingStategy
    {
        private bool _longest;

        public PathLengthStrategy(bool longest = true)
        {
            this._longest = longest;
        }

        public override Vector2 Choose(
            EnemyStrategyPathComposer pathGenerator,
            IOrderedEnumerable<Vector2> input)
        {
            Vector2 prev = pathGenerator.Last;
            input = input
                .OrderBy(x => pathGenerator.Path.Contains(x));

            if (_longest)
                input = input.ThenBy(x => Vector2.Distance(prev, x));
            else
                input = input.ThenByDescending(x => Vector2.Distance(prev, x));
            return input.First();
        }
    }
}

public class EnemyStrategyPathComposer
{
    public List<Vector2> Path;

    private int _currentStrategyIndex = 0;
    private bool _finished = false;
    private int _madeDecisions = 0;
    private List<EnemyPathingStrategies.EnemyPathingStategy> _pathingStategies;
    private Graph<Vector2> _roadMap;
    private int _targetDecisions = 3;

    public Vector2 Last => Path[Path.Count - 1];
    public EnemyStrategyPathComposer(
        Graph<Vector2> roadmap,
        int len,
        List<EnemyPathingStrategies.EnemyPathingStategy> strats)
    {
        if (strats.Count == 0)
            throw new ArgumentException("Enemy path picking strategies cannot be null");

        _roadMap = roadmap;
        this._pathingStategies = strats;
    }

    public List<Vector2> ComposePath(Vector2 from)
    {
        Path = new List<Vector2>() { from };
        while (_finished == false)
        {
            IEnumerable<Vector2> input = _roadMap.GetNeighbors(Last);
            Progress(input);
        }
        return Path;
    }

    private void Progress(IEnumerable<Vector2> input)
    {
        if (input.Count() == 0)
        { _finished = true; return; }
        else if (input.Count() == 1)
        {
            if (Path.Contains(input.First()))
            {
                _finished = true;
                return;
            }
            else
            {
                Path.Add(input.First());
            }
        }
        else
        {
            Path.Add(_pathingStategies[_currentStrategyIndex].Choose(this, input.OrderBy(x => x.x)));
            _currentStrategyIndex++;
            if (_currentStrategyIndex >= _pathingStategies.Count)
                _currentStrategyIndex = 0;
            _madeDecisions++;
        }
        if (_madeDecisions >= _targetDecisions)
        {
            _finished = true; return;
        }

        if (Path.Count > 1 && Path[Path.Count - 1] == Path[0])
        {
            //TODO cyclic path
            _finished = true; return;
        }
    }
}