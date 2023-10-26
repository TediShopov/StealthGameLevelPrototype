using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CGALDotNet;
using CGALDotNet.Geometry;
using CGALDotNet.Polygons;
using CGALDotNetGeometry.Shapes;
using CGALDotNetGeometry.Numerics;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

[RequireComponent(typeof(PolygonCollider2D))]
public class PolygonObstacle : MonoBehaviour
{
    public bool IsExpandingOutWards = true;
    public float DebugDistance = 0.1f;
    public PolygonCollider2D PolygonCollider;
    protected Polygon2<EIK> Polygon;
    protected List<Polygon2<EIK>> OffsetPolygon;
    private void Start()
    {
        PolygonCollider = GetComponent<PolygonCollider2D>();
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log(collision.gameObject);
    }
    public void OffsetPoints(double Offset) 
    {

        OffsetPolygon = new List<Polygon2<EIK>>();
        SetTo(gameObject.transform, PolygonCollider, ref Polygon);
        //Get the offset algorithm instance
        var instance = PolygonOffset2<EIK>.Instance;
        //Create the interior offset.
        if (IsExpandingOutWards == true)
            instance.CreateInteriorOffset(Polygon, -Offset, OffsetPolygon);
        else
            instance.CreateInteriorOffset(Polygon, Offset, OffsetPolygon);

        //
        SetTo(gameObject.transform,OffsetPolygon[0],ref PolygonCollider);
        CopyPoints(OffsetPolygon[0], ref Polygon);
    }
    public static void CopyPoints(Polygon2<EIK> from, ref Polygon2<EIK> to) 
    {
        Point2d[] offsetPoionts = new Point2d[from.Count]; 
        from.GetPoints(offsetPoionts, from.Count);
        to.SetPoints(offsetPoionts, offsetPoionts.Length);
    }
    public static void SetTo(Transform transform,Polygon2<EIK> polygon,ref PolygonCollider2D polygonCollider) 
    {
        UnityEngine.Vector2[] newColliderPoints = new UnityEngine.Vector2[polygon.Count];
        int iter = 0;
        foreach (var point in polygon)
        {
            newColliderPoints[iter] = new UnityEngine.Vector2((float)point.x,(float)point.y);
            newColliderPoints[iter] = transform.InverseTransformPoint(newColliderPoints[iter]);
            iter++;
        }
        polygonCollider.points = newColliderPoints;
    }
    public static void SetTo(Transform transform,PolygonCollider2D polygonCollider,ref Polygon2<EIK> polygon) 
    {
        Point2d[] points= new Point2d[polygonCollider.points.Length];
        int iter = 0;
        foreach (var point in polygonCollider.points)
        {

            //Transform the points to make them GLOBAL
            var globalPoint = transform.TransformPoint(point);
            points[iter] = new Point2d(globalPoint.x,globalPoint.y);
            iter++;
        }
        polygon= new Polygon2<EIK>(points);
        if (polygon.IsSimple) 
            if (!polygon.IsCounterClockWise)
                polygon.Reverse();
    }
    public  void OnDrawGizmos()
    {
        if (PolygonCollider == null) return;
        if(IsExpandingOutWards)
            Gizmos.color = Color.red;
        else Gizmos.color = Color.blue;
        for (int i = 0;i < PolygonCollider.points.Length-1;i++) 
        {
            Debug.DrawLine(this.transform.TransformPoint(PolygonCollider.points[i]), 
                this.transform.TransformPoint(PolygonCollider.points[i + 1]), Color.white);
        }
        foreach (var point in PolygonCollider.points)
        {
            Gizmos.DrawSphere(this.transform.TransformPoint(point), DebugDistance); 
        }
    }
    public bool CheckIntersection(PolygonObstacle other,ref UnityEngine.Vector2 pointOfIntersection) 
    {
        if (this.Polygon == null || other.Polygon == null) return false;
       List<PolygonWithHoles2<EIK>> results = new List<PolygonWithHoles2<EIK>>();
       bool result= this.Polygon.Intersection(other.Polygon, results);
        if(result) 
        {
            //Get the first hole and put and 
            foreach (var hole in results[0])
            {
                pointOfIntersection.x +=(float) hole.x;
                pointOfIntersection.y+=(float) hole.y;
            }
            pointOfIntersection.x /= results[0].Count;
            pointOfIntersection.y /= results[0].Count;
        }
        return result;
        
    }
    public bool PointOfIntersection(PolygonObstacle other) 
    {
       return this.Polygon.Intersection(other.Polygon, new List<PolygonWithHoles2<EIK>>());
    }
}
