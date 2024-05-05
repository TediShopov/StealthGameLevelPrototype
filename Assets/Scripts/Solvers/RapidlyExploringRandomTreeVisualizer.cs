using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class RapidlyExploringRandomTreeVisualizer : MonoBehaviour
{
    public IFutureLevel FutureLevel;

    [HideInInspector] public CharacterController2D Controller;
    [HideInInspector] public GameObject StartNode;
    [HideInInspector] public GameObject EndNode;
    public List<Vector3> Path = new List<Vector3>();

    [SerializeReference, SubclassPicker] public RRT RRT;

    [SerializeReference, HideInInspector] protected GameObject level;

    public virtual void Setup()
    {
        level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        if (level == null) return;
        EndNode = Helpers.SafeGetComponentInChildren<WinTrigger>(level).gameObject;

        var phenotype = level.GetComponentInChildren<LevelChromosomeMono>()
            .Chromosome.Phenotype;

        FutureLevel = phenotype.FutureLevel;
        StartNode = Helpers.SafeGetComponentInChildren<CharacterController2D>(level).gameObject;
        Controller = Helpers.SafeGetComponentInChildren<CharacterController2D>(level);
    }

    public virtual void Run()
    {
        Profiler.BeginSample("RRT Run");
        if (FutureLevel == null) return;

        if (RRT == null) return;

        var start = level.transform.InverseTransformPoint(StartNode.transform.position);
        var goal = level.transform.InverseTransformPoint(EndNode.transform.position);

        RRT.Setup(FutureLevel, RRT._goalDistance, Controller.MaxSpeed, start, goal);
        RRT.Run();

        //Ouputs RRT stats
        string rrtStatsLog = $"RRT Iterations {RRT.Stats.TotalIterations}," +
            $"  Failed: {RRT.Stats.FailedConnections} " +
            $"(Time: {RRT.Stats.TimeFails}), " +
            $"(Static: {RRT.Stats.StaticFails}), " +
            $"(Dynamic: {RRT.Stats.DynamicFails})";
        Debug.Log(rrtStatsLog);

        Path = RRT.ReconstructPathToSolution();
        Profiler.EndSample();
    }

    public bool SelectionDrawingRequirements => Selection.transforms.Any(
        x => x == this.transform
        || x == this.transform.parent.transform
        || x == this.transform.parent.parent.transform);

    public void OnDrawGizmosSelected()
    {
        if (Selection.transforms.Any(x => x.Equals(this.gameObject.transform)))
        {
            int b = 3;
        }
        //Avodi drawing if not clicking on this, parent, or level
        if (SelectionDrawingRequirements == false) return;

        //Do not draw anything as algorithm has not been stared
        if (RRT == null) return;

        //Draw whole tree
        Gizmos.color = Color.black;
        DFSDraw(this.RRT.StartNode);

        //Draw correct path on top so it is visible
        Gizmos.color = Color.green;
        for (int i = 0; i < Path.Count - 1; i++)
        {
            var segmentA = level.transform.TransformPoint(Path[i]);
            var segmentB = level.transform.TransformPoint(Path[i + 1]);

            Gizmos.DrawSphere(segmentA, 0.1f);
            Gizmos.DrawLine(segmentA, segmentB);
            Handles.Label(segmentA, $"{segmentA.z.ToString("0.00")}");
        }
    }

    public void DFSDraw(TreeNode<Vector3> node)
    {
        if (node == null) return;

        var nodeGlobalPosition = level.transform.TransformPoint(node.Content);
        Gizmos.DrawSphere(nodeGlobalPosition, 0.1f);
        foreach (var child in node.Children)
        {
            var childGlobalPosition = level.transform.TransformPoint(child.Content);
            Gizmos.DrawLine(nodeGlobalPosition, childGlobalPosition);
            DFSDraw(child);
        }
    }
}