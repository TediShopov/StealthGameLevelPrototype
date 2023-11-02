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

public class VoronoiRoadMapGenerator : MonoBehaviour
{
    private List<Point2d> _voronoiPoints;
    public List<Vector2> ObstaclePoints;
    public List<Vector2> BoundaryPoints;
    public DelaunayTriangulation2<EEK> _triangulation; 
    public PolygonBoundary PolygonBoundary;
    List<PolygonCollider2D> Colldiers;
    PolygonCollider2D Boundary;
    List<Vector2> SimplifiedPoints;
    public float DistanceFromPoint=0.5f;
    public LayerMask BoundaryObstacleLayerMask;
    // Start is called before the first frame update
    void Start()
    {
    }
    public void InitializeRoadmap() 
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
        _voronoiPoints = ConvertoToPoint2D(VoronoiPoints);

       
        //Create triangulation
        _triangulation = new DelaunayTriangulation2<EEK>();
        _triangulation.Insert(_voronoiPoints.ToArray(), _voronoiPoints.Count);
        Debug.Log($" Voronoi Points used: { _voronoiPoints.Count.ToString()}");
        Debug.Log($" Obstacle points used: { ObstaclePoints.Count.ToString()}");
        Debug.Log($" Boundary points used: { BoundaryPoints.Count.ToString()}");
        Debug.Log($" Collider count: { Colldiers.Count.ToString()}");
        SimplifiedPoints = new List<Vector2>();
        foreach (var segment in _triangulation.GetVoronoiSegments())
        {
            Vector2 start = segment.A.ToUnityVector2();
            Vector2 end = segment.B.ToUnityVector2();
            if (!Valid(start) || !Valid(end))
            {
            }
            else
            {
                AddIfFarEnough(start);
                AddIfFarEnough(end);
            }
        }
//        if (Graph) 
//        {
//            GenerateGraphFromLineSegments(Graph);
//        }
//        Debug.Log($" Simplified count: { SimplifiedPoints.Count.ToString()}");
    }
    private void AddIfFarEnough(Vector2 p) 
    {
        var index=SimplifiedPoints.FindIndex(x => Vector2.Distance(x, p) < DistanceFromPoint);
        if (index == -1)
        {
            SimplifiedPoints.Add(p);
        }
        else 
        {
            Debug.Log("Adjusted");
            Vector2 newPos = (SimplifiedPoints[index] +p)/ 2.0f;
            SimplifiedPoints[index] = newPos;
        }
    }
    private bool CompateFloats(float a, float b, float eps) 
    {
        return Mathf.Abs(a - b) < eps;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private List<Point2d> ConvertoToPoint2D(List<Vector2> points) 
    {
        return points.Select(x => x.ToCGALPoint2d()).ToList();
    }
    private List<Vector2> GetBoundingPolygonPoints() 
    {
        if(PolygonBoundary == null) 
        { return new List<UnityEngine.Vector2>(); }
        return GetGlobalPointsFromCollider(PolygonBoundary.GetComponent<PolygonCollider2D>());


    }
    
    private List<Vector2> CollectAllPolygonObstaclePoints()
    {
        PolygonCollider2D[] colliders = FindObjectsOfType<PolygonCollider2D>();
        List<Vector2> pointsList = new List<Vector2>();
        foreach (var collider in colliders)
        {
            pointsList.AddRange(GetGlobalPointsFromCollider( collider));
        }
        //Remove points that are collected from polygon boundary
        foreach (var boundingPoint in GetBoundingPolygonPoints())
        {
            pointsList.Remove(boundingPoint);
        }
        return pointsList;
    }

    private static List<Vector2> GetCenterPointsFromCollider(PolygonCollider2D collider2D) 
    {
        return new List<UnityEngine.Vector2>() { collider2D.bounds.center };

    }
    private static List<Vector2> GetGlobalPointsFromCollider( PolygonCollider2D collider)
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
        var hit= Physics2D.Linecast(start, end, BoundaryObstacleLayerMask);
        if(hit) 
        {
            return false;
        }
        return true;

    }
    public List<Vector2> GetValidSegments( DelaunayTriangulation2<EEK> triangulation )
    {
        List<Vector2 > segments = new List<Vector2>();
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

    
    private void OnDrawGizmosSelected()
    {
        //        Gizmos.color = Color.green;
        //        foreach (var obstaclePoint in ObstaclePoints)
        //        {
        //            Gizmos.DrawSphere(obstaclePoint, 0.2f);
        //        }
        //
        //        Gizmos.color = Color.black;
        //        foreach (var boundaryPoint in BoundaryPoints)
        //        {
        //            Gizmos.DrawSphere(boundaryPoint, 0.2f);
        //        }
                int iter = 0;
        //
                foreach (var segment in _triangulation.GetVoronoiSegments()) 
                {
                    iter++;
                    
                    Vector2 start = segment.A.ToUnityVector2();
                    Vector2 end = segment.B.ToUnityVector2();
                    if (!Valid(start) || !Valid(end) || !IsValidSegment(start,end))
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(start, end);
                    }
                    else
                    {
                
        
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(start, end);
                    }
                }
    }
}
