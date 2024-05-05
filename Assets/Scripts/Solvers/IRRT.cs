using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RRTStats
{
    public int DynamicFails;
    public int StaticFails;
    public int SuccesfullConnections;
    public int TimeFails;
    public int TotalIterations;
    public int FailedConnections => StaticFails + DynamicFails + TimeFails;
}

public interface IRapidlyEpxploringRandomTree<TState>
{
    public int MaxIterations { get; set; }
    public float SteerStep { get; set; }
    public TState Goal { get; set; }
    public TreeNode<TState> GoalNodeFound { get; set; }
    public TreeNode<TState> StartNode { get; set; }
    public RRTStats Stats { get; }

    public void Run();
    public bool Succeeded();
    //Return a booleaned indication if a some abstract state
    //could be reached from another state
    public bool IsColliding(TState from, TState to);
    //Return boolean indicating if some state is close
    //"enough" to be considered goal state
    public bool IsGoalState(TState state);
    //Give intermediate state between from and to states based
    //on some constraints defined by the algorithm
    public TState Steer(TState from, TState to);
    //Gets a random state of the space explored as a to be sample.
    public TState GetRandomState();
    //Traverse the tree from the branch all the way up to the root and
    //stores traversed states in a list
    List<TState> ReconstructPathToSolution();
}