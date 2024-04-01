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

public struct RRTResults
{
}

public interface IRapidlyEpxploringRandomTree<TState>
{
    public RRTStats Stats { get; }
    public TreeNode<TState> StartNode { get; set; }
    public TState Goal { get; set; }
    public int MaxIterations { get; set; }
    public TreeNode<TState> GoalNodeFound { get; set; }

    public void Run(TState start, TState end, int maxIteration = 100);

    public bool Succeeded();

    //Return boolean indicating if some state is close enough to be considered goal state
    public bool IsGoalState(TState state);

    //Gets a random state to be sample. Usually takes into considaration the bound of the level and currently explored tree braches
    public TState GetRandomState();

    //Give intermediate state between from and to states based on some constraints defined by the algorithm
    public TState Steer(TState from, TState to);

    //Return a booleaned indication if a some abstract state could be reached from another state
    public bool IsColliding(TState from, TState to);

    //Traverse the tree from the branch all the way up to the root and stores traversed states in a list
    List<TState> ReconstructPathToSolution();
}

//public class RRTNode : ITreeNode<RRTNode>
//{
//    public Vector3 Position;
//}
[System.Serializable]
public struct RRTStats
{
    public int TotalIterations;
    public int SuccesfullConnections;
    public int FailedConnections => StaticFails + DynamicFails + TimeFails;
    public int StaticFails;
    public int DynamicFails;
    public int TimeFails;
}

public class DiscreteDistanceBasedRRTSolver : IRapidlyEpxploringRandomTree<Vector3>
{
    public int MaxIterations { get; set; }
    public TreeNode<Vector3> StartNode { get; set; }
    public TreeNode<Vector3> GoalNodeFound { get; set; }
    public Vector3 Goal { get; set; }

    public float MaxVelocity;
    private KDTree _kdTree;
    private Vector3 _randomMin;
    private Vector3 _randomMax;

    //..private VoxelizedLevelBase FutureLevel;
    private IFutureLevel FutureLevel;

    public RRTStats _stats;
    public RRTStats Stats => _stats;

    private Dictionary<Vector3, TreeNode<Vector3>> _stateToTreeNode;
    private TreeNode<Vector3> _lastAddedState;

    public bool Succeeded()
    {
        if (this.GoalNodeFound != null)
            return this.IsGoalState(GoalNodeFound.Content);
        return false;
    }

    //If nodes distance to goal is closer than this bias distance, node will performed Biased Step next
    //iteration to steer it to goal
    public float BiasDistance;

    //If distance between some state and and end state is below this distance the state is treated as a solution
    public float GoalDistance;

    public DiscreteDistanceBasedRRTSolver(IFutureLevel discretizedLevel, float bias, float goalDist, float maxvel)
    {
        this.FutureLevel = discretizedLevel;
        _randomMin = discretizedLevel.GetBounds().min;
        _randomMax = discretizedLevel.GetBounds().max;
        //        _randomMin = discretizedLevel.FutureGrids[0].WorldMin;
        //        _randomMin.z = 0;
        //        //_randomMax = discretizedLevel.GetMaximumBound();
        //        _randomMax = discretizedLevel.FutureGrids[0].WorldMax;
        //        _randomMax.z = discretizedLevel.Iterations * discretizedLevel.Step;
        this.BiasDistance = bias;
        this.GoalDistance = goalDist;
        this.MaxVelocity = maxvel;
    }

    public void Run(Vector3 start, Vector3 end, int maxIteration = 100)
    {
        StartNode = new TreeNode<Vector3>(start);
        Goal = end;
        //Initializing mapping from explored states to tree node that contain information about parent and children
        _stateToTreeNode = new Dictionary<Vector3, TreeNode<Vector3>>();
        _stateToTreeNode.Add(start, StartNode);

        //initialzing KDtree strucutre to improve performance of nearease neighbour search
        _kdTree = new KDTree(KDTree.ToFloatArray(start), 3, 0);

        int iter = 0;
        Vector3 _lastBiasedState = Vector3.zero;
        while (iter < maxIteration)
        {
            TreeNode<Vector3> stepResult = null;

            bool cannotBiasStateThatWasAlreadyBiased = _lastAddedState != null && _lastBiasedState.Equals(_lastAddedState.Content);
            if (cannotBiasStateThatWasAlreadyBiased)
            {
                //Debug.Log("cannotBiasStateThatWasAlreadyBiased");
            }
            if (_lastAddedState != null && !_lastBiasedState.Equals(_lastAddedState.Content) && IsInBiasDistance(_lastAddedState.Content, end))
            {
                _lastBiasedState = _lastAddedState.Content;
                stepResult = DoBiasedStep();
            }
            else
            {
                stepResult = DoStep();
            }

            //If steps collision check did not pass they will return empty step so it has to be checked again
            if (stepResult != null)
            {
                //If it is close enough to goal by some user defined critera, the algorithm has found a path
                if (IsGoalState(stepResult.Content))
                {
                    GoalNodeFound = stepResult;
                    break;
                }
                //If the newly added state is close enough to some distance from the goal an attemp will be made next step
                //to steer the state to goal
            }
            if (stepResult != null) _lastAddedState = stepResult;
            iter++;
        }
        _stats.TotalIterations = iter;
    }

    public bool IsInBiasDistance(Vector3 state, Vector3 goal)
    {
        return Vector2.Distance(state, goal) < BiasDistance;
    }

    public virtual Vector3 GetRandomState()
    {
        Vector2 goalSubState = new Vector2(UnityEngine.Random.Range(_randomMin.x, _randomMax.x), UnityEngine.Random.Range(_randomMin.y, _randomMax.y));
        float d = Vector2.Distance(StartNode.Content, goalSubState);
        float minimumTimeToReach = d / MaxVelocity;
        float z = UnityEngine.Random.Range(minimumTimeToReach, _randomMax.z);
        return new Vector3(goalSubState.x, goalSubState.y, z);
    }

    public float GetRandomReachableTime(Vector2 point)
    {
        float d = Vector2.Distance(StartNode.Content, point);
        float minimumTimeToReach = d / MaxVelocity;
        return
           UnityEngine.
           Random.Range(minimumTimeToReach, _randomMax.z);
    }

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

        if (FutureLevel.IsStaticCollision(from, to))
        {
            _stats.StaticFails++;
            return true;
        }
        else if (FutureLevel.IsDynamicCollision(from, to))
        {
            _stats.DynamicFails++;
            return true;
        }
        return false;
    }

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

    private TreeNode<Vector3> DoStep()
    {
        Vector3 target = GetRandomState();
        var nearestNode = KDTree.NearestNeighbor(_kdTree, KDTree.ToFloatArray(target));
        var nearestPoint = (Vector3)nearestNode;

        Vector3 newPoint = Steer((Vector3)nearestPoint, target);

        if (!IsColliding((Vector3)nearestPoint, newPoint))
        {
            return AddToTreeStates(newPoint, nearestPoint);
        }
        return null;
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

    public Vector3 Steer(Vector3 from, Vector3 to)
    {
        Vector2 direction = (to - from).normalized;
        float distanceToGoal = Vector2.Distance(from, to);
        float timePassed = to.z - from.z;
        if (distanceToGoal / timePassed <= MaxVelocity)
        {
            return new Vector3(to.x, to.y, to.z);
        }
        else
        {
            Vector2 reachedPosition = new Vector2(from.x, from.y) + direction * (MaxVelocity * timePassed);
            return new Vector3(reachedPosition.x, reachedPosition.y, from.z + timePassed);
        }
    }

    public bool IsGoalState(Vector3 state)
    {
        return Vector2.Distance(state, Goal) < GoalDistance;
    }
}