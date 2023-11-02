using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class KDTree
{
    public Vector2 Point { get; set; }
    public KDTree Left { get; private set; }
    public KDTree Right { get; private set; }
    public int Depth { get; set; }

    public KDTree(Vector3 point, int depth =0)
    {
        Point = point;
        Left = null;
        Right = null;
        Depth = depth;
    }
    public void AddLeft(Vector2 point) 
    {
        this.Left = new KDTree(point, Depth + 1);
    }
    public void AddRight(Vector2 point) 
    {
        this.Right = new KDTree(point, Depth + 1);
    }

    public void AddKDNode(Vector2 target)
    {
        if (IsLeft(target))
        {
            if (Left == null)
            {
                this.Left = new KDTree(target, Depth + 1);
            }
            else
            {
                this.Left.AddKDNode(target);
            }
        }
        else 
        {
            if (Right == null)
            {
                this.Right = new KDTree(target, Depth + 1);
            }
            else
            {
                this.Right.AddKDNode(target);
            }
        }
        

    }

    public bool IsLeft(Vector2 target) => (target[this.Depth % 2] < this.Point[this.Depth % 2]);

    public KDTree[] GetCorrectNode( Vector2 target) 
    {
        if (target[this.Depth % 2] < this.Point[this.Depth % 2]) 
            return new KDTree[] { this.Left, this.Right};
        else
            return new KDTree[] { this.Right, this.Left};
    }
    public static KDTree NearestNeighbor(KDTree root, Vector2 target)
    {
        if (root == null)
        {
            return null;
        }
        var branched = root.GetCorrectNode( target);

        KDTree temp = NearestNeighbor(branched[0], target);
        KDTree best = Closest(target, temp, root); 
        float distanceToBest = Vector2.Distance(target,best.Point);
        float distPerpenicular =Mathf.Abs(target[root.Depth%2] - root.Point[root.Depth % 2]);

        if (distPerpenicular < distanceToBest) 
        {
            temp = NearestNeighbor(branched[1],target);
            best = Closest(target, temp, best);
        }
        return best;

    }
    public static KDTree Closest(Vector2 point, KDTree nodeOne, KDTree nodeTwo) 
    {
        if (nodeOne == null) return nodeTwo;
        if (nodeTwo == null) return nodeOne;
        float distanceFromOne = Vector2.Distance(point, nodeOne.Point);
        float distanceFromTwo = Vector2.Distance(point, nodeTwo.Point);
        if (distanceFromOne < distanceFromTwo) return nodeOne;
        else return nodeTwo;
    }

}

public class KDTreeVisualizer : MonoBehaviour
{
     private KDTree rootKDNode;
    private List<Transform> kdNodeTransforms = new List<Transform>();
    private Vector2 NearestFoundInTree = new Vector2();
    private Vector2 LastClicked;
    public bool RedoLast = false;

    private void Start()
    {
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            LastClicked = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (rootKDNode == null) 
            {
                rootKDNode = new KDTree(LastClicked);
            }
            else
            {
            NearestFoundInTree = KDTree.NearestNeighbor(rootKDNode, LastClicked).Point;
                rootKDNode.AddKDNode(LastClicked);
            }
            //AddKDNode( clickPosition);
        }
        if (RedoLast) 
        {
            NearestFoundInTree = KDTree.NearestNeighbor(rootKDNode,LastClicked).Point;
            RedoLast = false;
        }
    }

     private void OnDrawGizmos()
    {
        DrawTree(rootKDNode);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(NearestFoundInTree, 0.2f); 
    }

    public static void DrawTree(KDTree node)
       
    {
        if (node == null)
            return;

        // Draw the node as a red sphere using Gizmos
//        Gizmos.color = Color.red;
//        Gizmos.DrawSphere(node.Point, 0.1f);

        // Draw lines to left and right children, if they exist
        if (node.Left != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(node.Point, node.Left.Point);
            DrawTree(node.Left);
        }
        if (node.Right != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(node.Point, node.Right.Point);
            DrawTree(node.Right);
        }
    }
}
