using CGALDotNet.Geometry;
using CGALDotNetGeometry.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CGALDotNet;
using CGALDotNet.Triangulations;
using Vector2 = UnityEngine.Vector2;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using UnityEditor;
using System;

public class VoronoiRoadMapGenerator : MonoBehaviour
{
    private List<Point2d> _voronoiPoints;
    private List<Vector2> ObstaclePoints;
    private List<Vector2> BoundaryPoints;
    public DelaunayTriangulation2<EEK> _triangulation;
    public PolygonBoundary PolygonBoundary;
    List<PolygonCollider2D> Colldiers;
    PolygonCollider2D Boundary;
    public float DistanceFromPoint = 0.5f;
    public LayerMask BoundaryObstacleLayerMask;
    // Start is called before the first frame update
    void Start()
    {
    }
    public Graph<Vector2> GetRoadmapGraph()
    {
        this.Colldiers = new List<PolygonCollider2D>();
        ObstaclePoints = CollectAllPolygonObstaclePoints();
        BoundaryPoints = GetBoundingPolygonPoints();
        //Construct voronoi points by merging the collections
        List<Vector2> VoronoiPoints = new List<Vector2>();
        VoronoiPoints.AddRange(ObstaclePoints);
        VoronoiPoints.AddRange(BoundaryPoints);

        this.Colldiers.AddRange(FindObjectsOfType<PolygonCollider2D>());
        this.Boundary = this.PolygonBoundary.GetComponent<PolygonCollider2D>();
        this.Colldiers.Remove(Boundary);

        //Internal representation of  the voronoi points;
        _voronoiPoints = VoronoiPoints.Select(x => x.ToCGALPoint2d()).ToList();

        //Create triangulation
        _triangulation = new DelaunayTriangulation2<EEK>();
        _triangulation.Insert(_voronoiPoints.ToArray(), _voronoiPoints.Count);
//        Debug.Log($" Voronoi Points used: {_voronoiPoints.Count.ToString()}");
//        Debug.Log($" Obstacle points used: {ObstaclePoints.Count.ToString()}");
//        Debug.Log($" Boundary points used: {BoundaryPoints.Count.ToString()}");
//        Debug.Log($" Collider count: {Colldiers.Count.ToString()}");
//
        return GenerateGraphFromLineSegments(GetValidSegments(_triangulation));
    }
    private List<Vector2> GetBoundingPolygonPoints()
    {
        if (PolygonBoundary == null)
        { return new List<UnityEngine.Vector2>(); }
        return GetGlobalPointsFromCollider(PolygonBoundary.GetComponent<PolygonCollider2D>());
    }
    public Graph<Vector2> GenerateGraphFromLineSegments(List<Vector2>segments)
    {
        var graph =new Graph<Vector2>();
        for (int i = 0;i<segments.Count-1;i+=2) 
        {
            graph.AddNode(segments[i]);
            graph.AddNode(segments[i+1]);
            graph.AddEdge(segments[i], segments[i+1]);
        }
        return graph;
    }

    private List<Vector2> CollectAllPolygonObstaclePoints()
    {
        PolygonCollider2D[] colliders = FindObjectsOfType<PolygonCollider2D>();
        List<Vector2> pointsList = new List<Vector2>();
        foreach (var collider in colliders)
        {
            pointsList.AddRange(GetGlobalPointsFromCollider(collider));
        }
        //Remove points that are collected from polygon boundary
        foreach (var boundingPoint in GetBoundingPolygonPoints())
        {
            pointsList.Remove(boundingPoint);
        }
        return pointsList;
    }

    private static List<Vector2> GetGlobalPointsFromCollider(PolygonCollider2D collider)
    {
        Vector2[] colliderPoints = collider.points;

        List<Vector2> pointsList = new List<Vector2>();
        for (int i = 0; i < colliderPoints.Length; i++)
        {
            Vector2 point = collider.transform.TransformPoint(colliderPoints[i]);
            pointsList.Add(point);
        }
        return pointsList;
    }
    private bool IsCollidingObstacle(Vector2 p)
    {
        foreach (var collider in Colldiers)
        {
            if (collider.OverlapPoint(p))
            {
                return true;
            }
        }
        return false;
    }
    private bool IsOutsideBoundary(Vector2 p)
    {
        return !Boundary.OverlapPoint(p);
    }
    private bool Valid(Vector2 p)
    {
        return !IsCollidingObstacle((Vector2)p) && !IsOutsideBoundary(p);
    }
    private bool IsValidSegment(Vector2 start, Vector2 end)
    {
        if(IsOutsideBoundary(start)) { return false; }
        if(IsOutsideBoundary(end)) { return false; }
        var hit = Physics2D.Linecast(start, end, BoundaryObstacleLayerMask);
        if (hit)
        {
            return false;
        }
        return true;

    }
    public List<Vector2> GetValidSegments(DelaunayTriangulation2<EEK> triangulation)
    {
        List<Vector2> segments = new List<Vector2>();
        foreach (var segment in triangulation.GetVoronoiSegments())
        {
            Vector2 start = segment.A.ToUnityVector2();
            Vector2 end = segment.B.ToUnityVector2();
            if (!Valid(start) || !Valid(end) || !IsValidSegment(start, end))
            {
            }
            else
            {
                segments.Add(start);
                segments.Add(end);
            }
        }
        return segments;
    }



    public static void DebugDrawAsSegments(List<Vector2> segments, Dictionary<Func<Vector2, Vector2,bool>, Color> conditionalColorMapping)
    {
        int iter = 0;
        for (int i = 0; i < segments.Count - 1; i+=2)
        {
            iter++;
            Vector2 start = segments[i];
            Vector2 end = segments[i + 1];
            foreach (var pred in conditionalColorMapping)
            {
                if (pred.Key(start,end))
                {
                    Gizmos.color = pred.Value;
                    Gizmos.DrawLine(start, end);
                    break;
                }
            }
        }
    }
    public List<Vector2> SegmentList(CGALDotNetGeometry.Shapes.Segment2d[] segment)
    {
        var toReturn = new List<Vector2>();
        foreach (var item in segment)
        {
            toReturn.Add(item.A.ToUnityVector2());
            toReturn.Add(item.B.ToUnityVector2());
        }
        return toReturn;
    }
    private void OnDrawGizmosSelected()
    {
        //Draw valid and invalid semgents
        DebugDrawAsSegments(
            SegmentList(_triangulation.GetVoronoiSegments()),
            new Dictionary<Func<UnityEngine.Vector2, UnityEngine.Vector2, bool>, Color> 
            {
                { IsValidSegment, Color.green}
            }
        ); 
    }
}
