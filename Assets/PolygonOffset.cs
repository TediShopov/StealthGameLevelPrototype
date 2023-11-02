using System.Collections;
using System.Collections.Generic;
using CGALDotNet;
using CGALDotNet.Geometry;
using CGALDotNet.Polygons;
using CGALDotNetGeometry.Shapes;
using CGALDotNetGeometry.Numerics;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(PolygonCollider2D))]
[ExecuteInEditMode]
public class PolygonOffset : MonoBehaviour
{
    public float DebugDistance;
    //Unity polyon collider 2d 
    PolygonCollider2D PolygonCollider;
    Polygon2<EIK> Polygon;
    List<Polygon2<EIK>> OffsetPolygon;
    public bool Intrude;
    public double Offset;
    public bool ApplyOffsetButton =false;

    // Start is called before the first frame update
    void Awake()
    {
        this.PolygonCollider = this.GetComponent<PolygonCollider2D>(); 
    }

    // Update is called once per frame
    void Update()
    {

        if (Offset != 0) 
        {
            SetCollideToPolygon();
            SetOffsetPollygon();
        }
        if (ApplyOffsetButton == true) 
        {
            ApplyOffsetMethod();
            Offset = 0;
            ApplyOffsetButton  = false;
        }
    }
    public void ApplyOffsetMethod() 
    {
        UnityEngine.Vector2[] newColliderPoints = new UnityEngine.Vector2[OffsetPolygon[0].Count];
        int iter = 0;
        foreach (var point in OffsetPolygon[0])
        {
            newColliderPoints[iter] = new UnityEngine.Vector2((float)point.x,(float)point.y);
            iter++;
        }
        PolygonCollider.points = newColliderPoints;
    }
    public void SetCollideToPolygon() 
    {
        Point2d[] points= new Point2d[PolygonCollider.points.Length];

        int iter = 0;
        foreach (var point in PolygonCollider.points)
        {
            points[iter] = new Point2d(point.x,point.y);
            iter++;
        }
        Polygon= new Polygon2<EIK>(points);
        if (Polygon.IsSimple) 
        {
            if (!Polygon.IsCounterClockWise)
            {
                Polygon.Reverse();
            }
        }
    }
    public void SetOffsetPollygon() 
    {
        if (Polygon == null) return;
        //Get the offset algorithm instance
        var instance = PolygonOffset2<EIK>.Instance;
        //Create the interior offset.
        OffsetPolygon = new List<Polygon2<EIK>>();
        if (Intrude)
            instance.CreateInteriorOffset(Polygon, Offset, OffsetPolygon);
        else
            instance.CreateExteriorOffset(Polygon, Offset, OffsetPolygon);
        if(OffsetPolygon.Count > 1) 
        {
            Debug.Log($"Interior offset count is {OffsetPolygon[0].Count}");
        }
    }
    public void OnDrawGizmos()
    {

        
        if (PolygonCollider == null) return;
            Gizmos.color = Color.white;
        foreach (var point in PolygonCollider.points)
        {
            Gizmos.DrawSphere(this.transform.TransformPoint(point), DebugDistance); 
        }
        if (OffsetPolygon == null) return;
        if (OffsetPolygon.Count > 0 && OffsetPolygon[0].Count > 0) 
        {
            Gizmos.color = Color.yellow;
            foreach (var polygon in OffsetPolygon) 
            {
                foreach (var point in polygon)
                {
                    UnityEngine.Vector2 vec = new UnityEngine.Vector2((float)point.x, (float)point.y);
                    Gizmos.DrawSphere(this.transform.TransformPoint(vec), DebugDistance);
                }
            }
        }
    }
}
