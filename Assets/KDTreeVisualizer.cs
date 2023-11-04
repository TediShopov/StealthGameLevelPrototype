using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
public class KDTree 
{
    public float[] Point { get; set; }
    public int MaxDimensions { get; set; }
    public KDTree Left { get; private set; }
    public KDTree Right { get; private set; }
    public int Depth { get; set; }

    public KDTree(float[] point, int maxDimension,int depth =0)
    {
        Point = point;
        Left = null;
        Right = null;
        Depth = depth;
    }
    public void AddLeft(float[] point) 
    {
        this.Left = new KDTree(point, Depth + 1);
    }
    public void AddRight(float[] point) 
    {
        this.Right = new KDTree(point, Depth + 1);
    }

    public void AddKDNode(float[] target)
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

    public bool IsLeft(float[] target) => 
        (target[this.Depth % 2] < this.Point[this.Depth % 2]);


    public KDTree[] GetCorrectNode( float[] target) 
    {
        if (target[this.Depth % 2] < this.Point[this.Depth % 2]) 
            return new KDTree[] { this.Left, this.Right};
        else
            return new KDTree[] { this.Right, this.Left};
    }
    public static float FloatDistance(float[] target, float[] point) 
    {

        float distance = 0f;
        float[] distancesInDimension = new float[target.Length];
        for (int i = 0; i < target.Length; i++) 
        {
            distancesInDimension[i] = target[i] - point[i];
            distance += distancesInDimension[i] * distancesInDimension[i];
        }
        return (float)Math.Sqrt(distance);
    }
    public static KDTree NearestNeighbor(KDTree root, float[] target)
    {
        if (root == null)
        {
            return null;
        }
        var branched = root.GetCorrectNode( target);

        KDTree temp = NearestNeighbor(branched[0], target);
        KDTree best = Closest(target, temp, root); 
        float distanceToBest = FloatDistance(target,best.Point);
        float distPerpenicular =Mathf.Abs(target[root.Depth%2] - root.Point[root.Depth % 2]);

        if (distPerpenicular < distanceToBest) 
        {
            temp = NearestNeighbor(branched[1],target);
            best = Closest(target, temp, best);
        }
        return best;

    }
    public static KDTree Closest(float[] point, KDTree nodeOne, KDTree nodeTwo) 
    {
        if (nodeOne == null) return nodeTwo;
        if (nodeTwo == null) return nodeOne;
        float distanceFromOne = FloatDistance(point, nodeOne.Point);
        float distanceFromTwo = FloatDistance(point, nodeTwo.Point);
        if (distanceFromOne < distanceFromTwo) return nodeOne;
        else return nodeTwo;
    }
    public static float[] ToFloatArray(Vector2 pos) 
    {
        return new float[] { pos.x, pos.y };
    }
    public static float[] ToFloatArray(Vector3 pos) 
    {
        return new float[] { pos.x, pos.y,pos.z};
    }
     public static implicit operator Vector2(KDTree node) => new Vector2(node.Point[0], node.Point[1]);

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
                rootKDNode = new KDTree(KDTree.ToFloatArray(LastClicked),2 );
            }
            else
            {
                KDTree.NearestNeighbor(rootKDNode, KDTree.ToFloatArray(LastClicked));
            var floatPoint = KDTree.NearestNeighbor(rootKDNode, KDTree.ToFloatArray(LastClicked)).Point;
                NearestFoundInTree = new Vector2(floatPoint[0], floatPoint[1]);
                rootKDNode.AddKDNode(KDTree.ToFloatArray(LastClicked));
            }
            //AddKDNode( clickPosition);
        }
        if (RedoLast) 
        {
            NearestFoundInTree = (Vector2)KDTree.NearestNeighbor(rootKDNode,KDTree.ToFloatArray(LastClicked));
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
            Gizmos.DrawLine((Vector2)node, (Vector2)node.Left);
            DrawTree(node.Left);
        }
        if (node.Right != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine((Vector2)node, (Vector2)node.Right);
            DrawTree(node.Right);
        }
    }
}
