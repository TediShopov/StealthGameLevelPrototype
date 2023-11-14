using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using UnityEngine;

public class TreeNode<T> 
{
    public TreeNode(T curr)
    {
        this.Content = curr;
    }
    public T Content;
    public TreeNode<T> Parent= null;
    public List<TreeNode<T>> Children = new List<TreeNode<T>>();
    public void AddChild(TreeNode<T> child) 
    {
        child.Parent = this;
        this.Children.Add(child);
    }
}
public struct RRTResults 
{

}
public interface IRapidlyEpxploringRandomTree<TState> 
{
    public TreeNode<TState> StartNode { get; set; }
    public int MaxIterations { get; set; }
    public void Run(TState start, TState end, int maxIteration =100);

    //Return boolean indicating if some state is close enough to be considered goal state
    public bool IsGoalState(TState state, TState end);

    //Gets a random state to be sample. Usually takes into considaration the bound of the level and currently explored tree braches
    public TState GetRandomState();
   //Give intermediate state between from and to states based on some constraints defined by the algorithm
   public TState Steer(TState from, TState to);
    //Return a booleaned indication if a some abstract state could be reached from another state
   public bool IsColliding(TState from, TState to);
    //Traverse the tree from the branch all the way up to the root and stores traversed states in a list
    List<TState> ReconstructPathToRoot(TState end);
}
//public class RRTNode : ITreeNode<RRTNode>
//{
//    public Vector3 Position;
//}

public class DiscreteRRTSolver : IRapidlyEpxploringRandomTree<Vector3>
{
    public int MaxIterations { get ; set ; }
    public TreeNode<Vector3> StartNode { get ; set ; }
    public Vector3 Goal;

    public float MaxVelocity;
    private KDTree _kdTree;
    private Vector3 _randomMin;
    private Vector3 _randomMax;
    private DiscretizeLevelToGrid DiscretizedLevel;
    private Dictionary<Vector3, TreeNode<Vector3>> _stateToTreeNode;
    public DiscreteRRTSolver(DiscretizeLevelToGrid discretizedLevel)
    {
        this.DiscretizedLevel = discretizedLevel;
        
    }
    public void Run(Vector3 start, Vector3 end, int maxIteration = 100)
    {
        _stateToTreeNode = new Dictionary<Vector3, TreeNode<Vector3>>()
        {
            {start, new TreeNode<Vector3>(start) }
        };

        _kdTree = new KDTree(
            point: KDTree.ToFloatArray(start),  
            maxDimension: 3, 
            depth: 0 );
    }
    public Vector3 GetRandomState()
    {
        Vector2 goalSubState = new Vector2(UnityEngine.Random.Range(_randomMin.x, _randomMax.x), UnityEngine.Random.Range(_randomMin.y, _randomMax.y));
        float d = Vector2.Distance(StartNode.Content, goalSubState);
        float minimumTimeToReach = d / MaxVelocity;
        float z = UnityEngine.Random.Range(minimumTimeToReach, _randomMax.z);
        return new Vector3(goalSubState.x, goalSubState.y, z);
    }

    public bool IsColliding(Vector3 from, Vector3 to)
    {
        throw new System.NotImplementedException();
    }

    public List<Vector3> ReconstructPathToRoot(Vector3 end)
    {
        throw new System.NotImplementedException();
    }

    private Vector3 _lastAdded = Vector3.zero;
    private bool DoStep() 
    {

        Vector3 nearestPoint = Vector3.zero;
        Vector3 newPoint = Vector3.zero;
        Vector3 target = Vector3.zero;
        //        if (BiasNewlyAddedToGoal)
        //        {
        //            target = BiasToGoal(lastAddedNode,EndNode.position);
        //            nearestPoint = lastAddedNode;
        //            BiasNewlyAddedToGoal= false;
        //        }
        //        else
        //        {
        //            target = RandomPoint();
        //            var nearestNode = KDTree.NearestNeighbor(kdTree, KDTree.ToFloatArray(target));
        //            nearestPoint = (Vector3)nearestNode;
        //        }
        target = GetRandomState();
        var nearestNode = KDTree.NearestNeighbor(_kdTree, KDTree.ToFloatArray(target));
        nearestPoint = (Vector3)nearestNode;

        newPoint = Steer((Vector3)nearestPoint, target);

        if (!IsColliding((Vector3)nearestPoint, newPoint))
        {
            if (nearestPoint.z > newPoint.z) 
            {
                return false;
            }
            _kdTree.AddKDNode(KDTree.ToFloatArray(newPoint));
            var newStateTreeBranch = new TreeNode<Vector3>(newPoint);
            _stateToTreeNode.Add(newPoint,newStateTreeBranch);
            if (_stateToTreeNode.ContainsKey(nearestPoint)) 
            {
                _stateToTreeNode[nearestPoint].AddChild(newStateTreeBranch);
            }
            _lastAdded = newPoint;
            if (Vector2.Distance(_lastAdded, Goal) < BiasDistance) 
            {
                BiasNewlyAddedToGoal = true;
            }
            //nodes.Add(newNodeTransform);

            if (IsGoalState(newPoint, EndNode.position))
            {
                return true;
            }
        }
        return false;
    }

    public Vector3 Steer(Vector3 from, Vector3 to)
    {
        throw new System.NotImplementedException();
    }

    public bool IsGoalState(Vector3 state, Vector3 end)
    {
        throw new System.NotImplementedException();
    }
}
