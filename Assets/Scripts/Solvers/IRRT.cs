using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IRapidlyEpxploringRandomTree<TState>
{
    public RRTStats Stats { get; }
    public TreeNode<TState> StartNode { get; set; }
    public TState Goal { get; set; }
    public int MaxIterations { get; set; }
    public TreeNode<TState> GoalNodeFound { get; set; }
    public float SteerStep { get; set; }

    public void Run();

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