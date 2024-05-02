using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RRT : IRapidlyEpxploringRandomTree<Vector3>
{
    public TreeNode<Vector3> StartNode { get; set; }
    public TreeNode<Vector3> GoalNodeFound { get; set; }
    public Vector3 Goal { get; set; }

    //If distance between some state and and end state is below
    //this distance the state is treated as a solution
    [SerializeField] public float GoalDistance;

    [SerializeField] private int _maxIterations;

    public int MaxIterations
    {
        get { return _maxIterations; }
        set { _maxIterations = value; }
    }

    [SerializeField] public float _steerStep;

    public float SteerStep
    {
        get { return _steerStep; }
        set { _steerStep = value; }
    }

    [HideInInspector] public int Iterations = 0;

    protected float MaxVelocity;
    protected KDTree _kdTree;
    protected Vector3 _randomMin;
    protected Vector3 _randomMax;

    public IFutureLevel FutureLevel;

    public RRTStats _stats;
    public RRTStats Stats => _stats;

    protected Dictionary<Vector3, TreeNode<Vector3>> _stateToTreeNode;

    public bool Succeeded()
    {
        if (this.GoalNodeFound != null)
            return this.IsGoalState(GoalNodeFound.Content);
        return false;
    }

    public RRT()
    {
    }

    private Transform levelTransform;

    public virtual void Setup(IFutureLevel discretizedLevel, float goalDist, float maxvel
        , Vector3 start, Vector3 end, Transform world)
    {
        this.levelTransform = world;
        this.FutureLevel = discretizedLevel;
        _randomMin = world.TransformPoint(discretizedLevel.GetBounds().min);
        _randomMax = world.TransformPoint(discretizedLevel.GetBounds().max);
        //_randomMax = discretizedLevel.GetBounds().max;
        this.GoalDistance = goalDist;
        this.MaxVelocity = maxvel;
        StartNode = new TreeNode<Vector3>(start);
        Goal = end;
        //Initializing mapping from explored states to tree node that contain information about parent and children
        _stateToTreeNode = new Dictionary<Vector3, TreeNode<Vector3>>();
        _stateToTreeNode.Add(start, StartNode);

        //initialzing KDtree strucutre to improve performance of nearease neighbour search
        _kdTree = new KDTree(KDTree.ToFloatArray(start), 3, 0);
        Iterations = 0;
    }

    public void Run()
    {
        while (Iterations < MaxIterations)
        {
            TreeNode<Vector3> stepResult = null;
            stepResult = DoStep();
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
            Iterations++;
        }
        _stats.TotalIterations = Iterations;
    }

    public virtual Vector3 GetRandomState()
    {
        return new Vector3(
            UnityEngine.Random.Range(_randomMin.x, _randomMax.x),
            UnityEngine.Random.Range(_randomMin.y, _randomMax.y),
            UnityEngine.Random.Range(_randomMin.z, _randomMax.z)
            );
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

        //Perform static collision - against geomtry- in GLOBAL SPACE
        if (FutureLevel.IsStaticCollision(from, to))
        {
            _stats.StaticFails++;
            return true;
        }

        //Perform dynamic collision - against threats- in LOCAL SPACE
        from = levelTransform.InverseTransformPoint(from);
        to = levelTransform.InverseTransformPoint(to);

        if (FutureLevel.IsDynamicCollision(from, to))
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

    //Does a step using the random sampler
    public virtual TreeNode<Vector3> DoStep()
    {
        Vector3 target = GetRandomState();
        return DoStep(target);
    }

    //Tries to reach a predetermined state
    public virtual TreeNode<Vector3> DoStep(Vector3 target)
    {
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
        if (_stateToTreeNode.ContainsKey(newPoint) == true)
            return null;
        if (_stateToTreeNode.ContainsKey(nearestPoint))
        {
            _kdTree.AddKDNode(KDTree.ToFloatArray(newPoint));
            TreeNode<Vector3> newStateNode = new TreeNode<Vector3>(newPoint);
            //TODO check if tree already contains this
            _stateToTreeNode.Add(newPoint, newStateNode);
            _stateToTreeNode[nearestPoint].AddChild(newStateNode);
            return newStateNode;
        }
        return null;
    }

    public virtual Vector3 Steer(Vector3 from, Vector3 to)
    {
        Vector2 direction = (to - from).normalized;
        float distanceToGoal = Vector2.Distance(from, to);

        if (distanceToGoal <= SteerStep + 0.1f)
        {
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

    public bool IsGoalState(Vector3 state)
    {
        return Vector2.Distance(state, Goal) < GoalDistance;
    }
}