using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Wrapper class to encapsualte any data in a tree node
public class TreeNode<T>
{
    public TreeNode(T curr)
    {
        this.Content = curr;
    }
    public T Content;
    public TreeNode<T> Parent = null;
    public List<TreeNode<T>> Children = new List<TreeNode<T>>();

    public void AddChild(TreeNode<T> child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}

public class RRT : IRapidlyEpxploringRandomTree<Vector3>
{
    [SerializeField] public RRTStats _stats;
    [SerializeField] private int _maxIterations; //Max iterations before terminating
    [SerializeField] private float _steerStep;
    [SerializeField] private bool _naiveNN = true;
    [SerializeField] public float Time = 0;
    [HideInInspector] private readonly float _delta = 0.1f;

    [HideInInspector] public IFutureLevel _futureLevel;
    [SerializeField] public float _goalDistance; //Distance to goal suffiecient for solution
    [HideInInspector] public int _iterations = 0;

    [HideInInspector] protected float _maxVelocity; //Kinematic constraint of players movement
    [HideInInspector] protected Vector3 _randomMax;
    [HideInInspector] protected Vector3 _randomMin;
    [HideInInspector] protected KDTree _kdTree; //KDTree to improve nearest neighbour search

    //Dictionary to mapp a vector from explored space into KDTree node
    [HideInInspector] protected Dictionary<Vector3, TreeNode<Vector3>> _stateToTreeNode;

    public int MaxIterations
    { get { return _maxIterations; } set { _maxIterations = value; } }

    public float SteerStep
    { get { return _steerStep; } set { _steerStep = value; } }

    public Vector3 Goal { get; set; }
    public TreeNode<Vector3> StartNode { get; set; }
    public TreeNode<Vector3> GoalNodeFound { get; set; }

    public RRTStats Stats => _stats;

    public RRT()
    {
    }

    //Runs the algorithm until either max iterations is reached or solution is found
    public void Run()
    {
        Time = Helpers.TrackExecutionTime(() =>
        {
            while (_iterations < MaxIterations)
            {
                TreeNode<Vector3> stepResult = null;
                stepResult = DoStep();
                //If steps collision check did not pass they will return
                //empty step so it has to be checked again
                if (stepResult != null)
                {
                    //If it is close enough to goal by some user defined critera,
                    //the algorithm has found a path
                    if (IsGoalState(stepResult.Content))
                    {
                        GoalNodeFound = stepResult;
                        break;
                    }
                }
                _iterations++;
            }
            _stats.TotalIterations = _iterations;
        }
            );
    }
    //Has a solution been found
    public bool Succeeded()
    {
        if (this.GoalNodeFound != null)
            return this.IsGoalState(GoalNodeFound.Content);
        return false;
    }
    public bool IsGoalState(Vector3 state)
    {
        return Vector2.Distance(state, Goal) < _goalDistance;
    }

    //Are two states colliding
    public bool IsColliding(Vector3 from, Vector3 to)
    {
        if (from.z < 0 || to.z < 0)
        {
            _stats.TimeFails++;
            return true;
        }
        if (from.z > to.z)
        {
            _stats.TimeFails++;
            return true;
        }

        if (_futureLevel.IsStaticCollision(from, to))
        {
            _stats.StaticFails++;
            return true;
        }

        if (_futureLevel.IsDynamicCollision(from, to))
        {
            _stats.DynamicFails++;
            return true;
        }
        return false;
    }
    //Returns a random Vector3 coordinates in the bounds of the explored space
    public virtual Vector3 GetRandomState()
    {
        return new Vector3(
            UnityEngine.Random.Range(_randomMin.x, _randomMax.x),
            UnityEngine.Random.Range(_randomMin.y, _randomMax.y),
            UnityEngine.Random.Range(_randomMin.z, _randomMax.z)
            );
    }
    //Does a step using the random sampler
    public virtual TreeNode<Vector3> DoStep()
    {
        Vector3 target = GetRandomState();
        return DoStep(target);
    }
    //Tries to reach a predetermined state
    public virtual TreeNode<Vector3> DoStep(Vector3 target)
    {
        //var nearestNode = GetNearestNode(target);

        var nearestPoint = (Vector3)GetNearestPoint(target);

        Vector3 newPoint = Steer((Vector3)nearestPoint, target);

        if (!IsColliding((Vector3)nearestPoint, newPoint))
        {
            return AddToTreeStates(newPoint, nearestPoint);
        }
        return null;
    }
    public Vector3 GetNearestPoint(Vector3 target)
    {
        if (_naiveNN)
            return NaiveNN(target);
        else
            return KNN(target);
    }
    public Vector3 NaiveNN(Vector3 target)
    {
        return (Vector3)KDTree.NearestNeighbor(_kdTree, KDTree.ToFloatArray(target));
    }
    public Vector3 KNN(Vector3 target)
    {
        float[] targetArray = KDTree.ToFloatArray(target);
        float[][] knn = _kdTree.KNearestNeighbors(targetArray, 4);

        float[] maxFeasible = knn[0];
        foreach (var n in knn)
        {
            if (n[2] < targetArray[2])
            {
                if (KDTree.FloatDistance(targetArray, n) <
                    KDTree.FloatDistance(targetArray, maxFeasible))
                {
                    maxFeasible = n;
                }
            }
        }
        return new Vector3(maxFeasible[0], maxFeasible[1], maxFeasible[2]);
    }

    //Traverse the tree from the branch all the way up to the root and
    //stores traversed states in a list
    public List<Vector3> ReconstructPathToSolution()
    {
        List<Vector3> path = new List<Vector3>();
        //Return empty path if no soluton was found
        if (GoalNodeFound == null) return path;

        TreeNode<Vector3> currentlyTraversedNode = GoalNodeFound;
        while (currentlyTraversedNode != null)
        {
            path.Add(currentlyTraversedNode.Content);
            currentlyTraversedNode = currentlyTraversedNode.Parent;
        }
        path.Reverse();
        return path;
    }

    public virtual void Setup(IFutureLevel discretizedLevel, float goalDist, float maxvel
        , Vector3 start, Vector3 end)
    {
        _stats = new RRTStats();
        _futureLevel = discretizedLevel;
        _randomMin = discretizedLevel.GetBounds().min;
        _randomMax = discretizedLevel.GetBounds().max;
        _goalDistance = goalDist;
        _maxVelocity = maxvel;

        StartNode = new TreeNode<Vector3>(start);
        Goal = end;
        _stateToTreeNode = new Dictionary<Vector3, TreeNode<Vector3>>();
        _stateToTreeNode.Add(start, StartNode);

        //initialzing KDtree strucutre to improve performance of nearease neighbour search
        _kdTree = new KDTree(KDTree.ToFloatArray(start), 3, 0);
        _iterations = 0;
    }
    public virtual Vector3 Steer(Vector3 from, Vector3 to)
    {
        Vector2 direction = (to - from).normalized;
        float distanceToGoal = Vector2.Distance(from, to);

        if (distanceToGoal <= SteerStep + _delta)
        {
            float timePassed = to.z - from.z;
            if (distanceToGoal / timePassed <= _maxVelocity)
            {
                return new Vector3(to.x, to.y, to.z);
            }
            else
            {
                Vector2 reachedPosition =
                    new Vector2(from.x, from.y) + direction * (_maxVelocity * timePassed);

                return new Vector3(reachedPosition.x, reachedPosition.y, from.z + timePassed);
            }
        }
        else
        {
            try
            {
                return Steer(from, from + (to - from).normalized * SteerStep);
            }
            catch (System.StackOverflowException)
            {
                throw;
            }
        }
    }
    protected TreeNode<Vector3> AddToTreeStates(Vector3 newPoint, Vector3 nearestPoint)
    {
        if (_stateToTreeNode.ContainsKey(newPoint) == true)
            return null;
        if (_stateToTreeNode.ContainsKey(nearestPoint))
        {
            _kdTree.AddKDNode(KDTree.ToFloatArray(newPoint));
            TreeNode<Vector3> newStateNode = new TreeNode<Vector3>(newPoint);
            _stateToTreeNode.Add(newPoint, newStateNode);
            _stateToTreeNode[nearestPoint].AddChild(newStateNode);
            return newStateNode;
        }
        return null;
    }
}