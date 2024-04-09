using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

public class RRTBiased : RRT
{
    private TreeNode<Vector3> _lastAddedState;
    private Vector3 _lastBiasedState;

    //If nodes distance to goal is closer than this bias distance, node will performed Biased Step next
    //iteration to steer it to goal
    public float BiasDistance;

    public RRTBiased(IFutureLevel discretizedLevel, float bias, float goalDist, float maxvel)
        : base(discretizedLevel, goalDist, maxvel)
    {
        this.FutureLevel = discretizedLevel;
        _randomMin = discretizedLevel.GetBounds().min;
        _randomMax = discretizedLevel.GetBounds().max;
        this.BiasDistance = bias;
        this.GoalDistance = goalDist;
        this.MaxVelocity = maxvel;
    }

    public override TreeNode<Vector3> DoStep()
    {
        TreeNode<Vector3> stepResult = null;
        bool cannotBiasStateThatWasAlreadyBiased = _lastAddedState != null && _lastBiasedState.Equals(_lastAddedState.Content);
        if (cannotBiasStateThatWasAlreadyBiased)
        {
            //Debug.Log("cannotBiasStateThatWasAlreadyBiased");
        }
        if (_lastAddedState != null
            && !_lastBiasedState.Equals(_lastAddedState.Content)
            && IsInBiasDistance(_lastAddedState.Content, Goal))
        {
            _lastBiasedState = _lastAddedState.Content;
            stepResult = DoBiasedStep();
        }
        else
        {
            stepResult = base.DoStep();
        }
        if (stepResult != null) _lastAddedState = stepResult;
        return stepResult;
    }

    public bool IsInBiasDistance(Vector3 state, Vector3 goal)
    {
        return Vector2.Distance(state, goal) < BiasDistance;
    }

    public override Vector3 GetRandomState()
    {
        //First sample a 2d position in the level
        Vector2 goalSubState = new Vector2(UnityEngine.Random.Range(_randomMin.x, _randomMax.x), UnityEngine.Random.Range(_randomMin.y, _randomMax.y));

        //Limit the time step random tange to have lower bound
        //such as the state can be reached by a stright line from the start state
        float time = UnityEngine.Random.Range(
            MinimumTimeToReach(StartNode.Content, goalSubState),
            _randomMax.z);
        return new Vector3(goalSubState.x, goalSubState.y, time);
    }

    //Gets the minimum time to reach from one state to another
    //solely based on max speed on a straight line
    private float MinimumTimeToReach(Vector2 start, Vector2 end)
    {
        float distance = Vector2.Distance(start, end);
        float minimumTimeToReach = distance / MaxVelocity;
        return minimumTimeToReach;
    }

    public float GetRandomReachableTime(Vector2 point)
    {
        float d = Vector2.Distance(StartNode.Content, point);
        float minimumTimeToReach = d / MaxVelocity;
        return
           UnityEngine.
           Random.Range(minimumTimeToReach, _randomMax.z);
    }

    //Gets the last node added to the RRT and steers it to goal, this should only be called
    //when last added nodes distance to goal is smaller than the bias
    private TreeNode<Vector3> DoBiasedStep()
    {
        Vector3 target = BiasToGoal(_lastAddedState.Content, Goal);
        var nearestNode = KDTree.NearestNeighbor(_kdTree, KDTree.ToFloatArray(target));
        var nearestPoint = (Vector3)nearestNode;

        Vector3 newPoint = Steer((Vector3)nearestPoint, target);

        if (!IsColliding((Vector3)nearestPoint, newPoint))
        {
            return AddToTreeStates(newPoint, nearestPoint);
        }
        return null;
    }

    private Vector3 BiasToGoal(Vector3 from, Vector3 goalState)
    {
        float d = Vector2.Distance(from, goalState);
        float minimumTimeToReach = d / MaxVelocity;
        return new Vector3(goalState.x, goalState.y, from.z + minimumTimeToReach);
    }

    private TreeNode<Vector3> AddToTreeStates(Vector3 newPoint, Vector3 nearestPoint)
    {
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