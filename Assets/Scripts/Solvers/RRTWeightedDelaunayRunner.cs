using CGALDotNet;
using CGALDotNet.Polygons;
using CGALDotNet.Triangulations;
using CGALDotNetGeometry.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CGALDotNetGeometry.Shapes;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine.Profiling;
using Codice.CM.SEIDInfo;

public class DelunayTriangulatedLevelData : MonoBehaviour
{
    public PolygonWithHoles2<EEK> LevelFree;
    public Triangle2d[] DelaynayTriangles;
}

//public class RRTWeightedDelaunayRunner : RapidlyExploringRandomTreeVisualizer
//{
//    private DelunayTriangulatedLevelData TringulationData;
//
//    public override void Run()
//    {
//        Profiler.BeginSample("RRT Weighted Run");
//        if (VoxelizedLevel == null) return;
//        //RRT = new RRT_B(VoxelizedLevel, BiasDistance, GoalDistance, Controller.MaxSpeed);
//        var rRTWDelaunay = new RRTWDelaunay(
//            VoxelizedLevel,
//            BiasDistance,
//            GoalDistance,
//            Controller.MaxSpeed);
//
//        rRTWDelaunay.SetTrianglesInFreeSpace(TringulationData.DelaynayTriangles);
//        RRT = rRTWDelaunay;
//        RRT.SteerStep = SteerStep;
//        RRT.Run(
//            StartNode.transform.position,
//            EndNode.transform.position,
//            maxIterations);
//        Stats = RRT.Stats;
//        string rrtStatsLog = $"RRT Iterations {RRT.Stats.TotalIterations}," +
//            $"  Failed: {RRT.Stats.FailedConnections} " +
//            $"(Time: {RRT.Stats.TimeFails}), " +
//            $"(Static: {RRT.Stats.StaticFails}), " +
//            $"(Dynamic: {RRT.Stats.DynamicFails})";
//        Debug.Log(rrtStatsLog);
//        Path = RRT.ReconstructPathToSolution();
//
//        Profiler.EndSample();
//    }
//
//    public List<Vector2> GetSquareColliderVertices(BoxCollider2D collider)
//    {
//        // Check if the collider is a square collider
//        if (collider.GetType() != typeof(BoxCollider2D))
//        {
//            throw new ArgumentException("This function only works with BoxCollider2D");
//        }
//
//        // Get the center and size of the collider
//        Vector2 center = collider.offset;
//        Vector2 size = collider.size;
//
//        // Calculate half sizes along x and y directions
//        float halfWidth = size.x / 2.0f;
//        float halfHeight = size.y / 2.0f;
//
//        // Define the four corners of the square in counter-clockwise order
//        Vector2[] vertices = new Vector2[4]
//        {
//    new Vector2(center.x + halfWidth, center.y + halfHeight),  // Top right
//    new Vector2(center.x - halfWidth, center.y + halfHeight),  // Top left
//    new Vector2(center.x - halfWidth, center.y - halfHeight),  // Bottom left
//    new Vector2(center.x + halfWidth, center.y - halfHeight)   // Bottom right
//        };
//
//        return new List<Vector2>(vertices)
//            .Select(x => (Vector2)collider.gameObject.transform.TransformPoint(x))
//            .ToList();
//    }
//
//    public LayerMask ObstacleLayer;
//
//    public PolygonWithHoles2<EEK> GetCGALPolygonLevel(GameObject phenotype)
//    {
//        //Get the bounding box collider
//        var boundaryBox = (BoxCollider2D)Helpers.GetLevelBoundaryCollider(phenotype)
//            .GetComponent<BoxCollider2D>();
//        if (boundaryBox == null) return null;
//        //Convert the vertices of collider square to CGAL point 2d
//        var boundaryVertices =
//                GetSquareColliderVertices(boundaryBox).
//                Select(x => new Point2d(x.x, x.y))
//
//                .ToArray();
//
//        var polygonBoundary = new Polygon2<EEK>(boundaryVertices);
//
//        //Ensure the boundary polygon is in CCW orientation
//        if (!polygonBoundary.IsCounterClockWise)
//            polygonBoundary.Reverse();
//
//        PolygonWithHoles2<EEK> levelPolygon =
//            new PolygonWithHoles2<EEK>(boundaryVertices);
//
//        //Get all other obstacle colliders and add them as holes
//        Collider2D[] obstacleColliders =
//        phenotype.GetComponentsInChildren<Collider2D>()
//            .Where(x => (ObstacleLayer & (1 << x.gameObject.layer)) != 0)
//            .Where(x => x.composite == null)
//            .Where(x => x is not CompositeCollider2D)
//            .ToArray();
//
//        foreach (var obstacle in obstacleColliders)
//        {
//            Point2d[] points = null;
//            if (obstacle.GetType() == typeof(PolygonCollider2D))
//            {
//                var poly = (PolygonCollider2D)obstacle;
//                points = poly.points
//                    .Select(x => obstacle.transform.TransformPoint(x))
//                    .Select(x => new Point2d(x.x, x.y))
//                    .ToArray();
//            }
//            else if (obstacle.GetType() == typeof(BoxCollider2D))
//            {
//                var box = (BoxCollider2D)obstacle;
//                points = GetSquareColliderVertices(box)
//                    .Select(x => new Point2d(x.x, x.y))
//                    .ToArray();
//            }
//            if (points != null)
//            {
//                //Ensure the boundary polygon is in CW orientation
//                var polygonHole = new Polygon2<EEK>(points);
//
//                //If hole is fully contained
//                if (IsFullyContained(polygonHole, levelPolygon.GetBoundary()))
//                {
//                    if (!polygonHole.IsClockWise)
//                        polygonHole.Reverse();
//                    levelPolygon.AddHole(polygonHole);
//                }
//                else
//                {
//                    //Get the instance object.
//                    var instance = PolygonBoolean2<EEK>.Instance;
//
//                    //If you know the input is good then checking
//                    //can be disabled which can increase perform.
//                    //instance.CheckInput = false;
//
//                    //Create  list to hold the results.
//                    //The result is always a list of PolygonWithHoles2.
//                    var results = new List<PolygonWithHoles2<EEK>>();
//
//                    //Perform what op you wish.
//                    //Could be JOIN, INTERSECT, DIFFERENCE, SYMMETRIC_DIFFERENCE.
//                    if (polygonHole.IsClockWise)
//                        polygonHole.Reverse();
//
//                    if (polygonHole.IsSimple == false)
//                    {
//                        int b = 3;
//                    }
//
//                    if (instance.Op(POLYGON_BOOLEAN.INTERSECT, levelPolygon.GetBoundary(),
//                        polygonHole, results))
//                    {
//                        //If the op was successful the results
//                        //list will  contain the polygons.
//                        foreach (var poly in results)
//                        {
//                            if (IsFullyContained(
//                                poly.GetBoundary(), levelPolygon.GetBoundary()))
//                            {
//                                poly.Print();
//                                if (!poly.IsClockWise)
//                                    poly.Reverse();
//                                levelPolygon.AddHole(poly.GetBoundary());
//                            }
//                        }
//                    }
//
//                    int a = 3;
//                }
//            }
//        }
//        return levelPolygon;
//    }
//
//    public bool IsFullyContained(Polygon2<EEK> p, Polygon2<EEK> b)
//    {
//        foreach (var point in p)
//        {
//            if (b.ContainsPoint(point) == false)
//                return false;
//        }
//        //Contained if all of the points lie inside the boundary polygon
//        return true;
//    }
//
//    public void DrawTriangles(Triangle2d[] triangles, Color color)
//    {
//        var pointVectors = new Vector2[3];
//        Gizmos.color = color;
//        foreach (var tri in triangles)
//        {
//            if (tri.IsCCW == false)
//            {
//                continue;
//            }
//            pointVectors[0] = new Vector2((float)tri.A.x, (float)tri.A.y);
//            pointVectors[1] = new Vector2((float)tri.B.x, (float)tri.B.y);
//            pointVectors[2] = new Vector2((float)tri.C.x, (float)tri.C.y);
//
//            foreach (var p in pointVectors)
//                Gizmos.DrawSphere(p, 0.1f);
//
//            Gizmos.DrawLine(pointVectors[0], pointVectors[1]);
//            Gizmos.DrawLine(pointVectors[1], pointVectors[2]);
//            Gizmos.DrawLine(pointVectors[2], pointVectors[0]);
//        }
//    }
//
//    public Triangle2d[] ConstrainedDelaunay(PolygonWithHoles2<EEK> level)
//    {
//        //        var m = new CGALDotNet.Meshing.ConformingTriangulation2<EEK>();
//        //        m.InsertConstraint(level);
//        //        m.MakeDelaunay();
//        var CDT = new ConstrainedDelaunayTriangulation2<EEK>();
//        CDT.InsertConstraint(level);
//        //        for (int i = 0; i < level.HoleCount; i++)
//        //        {
//        //            CDT.InsertConstraint(level.GetHole(i));
//        //        }
//        //        CDT.InsertConstraint(level.GetBoundary());
//        //Get the triangles a shapes.
//        var triangles = new Triangle2d[CDT.TriangleCount];
//        CDT.GetTriangles(triangles, triangles.Length);
//
//        triangles = triangles.Where(tri =>
//        {
//            for (int i = 0; i < level.HoleCount; i++)
//            {
//                var hole = level.GetHole(i);
//                if (hole.ContainsPoint(tri.Center))
//                    return false;
//            }
//            return true;
//        }).ToArray();
//
//        return triangles;
//    }
//
//    public void DrawPolygonSegments(Polygon2<EEK> poly)
//    {
//        Segment2d[] segments = new Segment2d[poly.Count];
//        poly.GetSegments(segments, poly.Count);
//        foreach (var seg in segments)
//        {
//            Vector3 pointA = new Vector3((float)seg.A.x, (float)seg.A.y, 0);
//            Vector3 pointB = new Vector3((float)seg.B.x, (float)seg.B.y, 0);
//            Gizmos.DrawSphere(pointA, 0.1f);
//            Gizmos.DrawLine(pointA, pointB);
//            Gizmos.DrawSphere(pointB, 0.1f);
//        }
//    }
//
//    public Triangle2d[] Delaynau(PolygonWithHoles2<EEK> level)
//    {
//        level.Triangulate(new List<int>(0));
//        var CDT = new DelaunayTriangulation2<EEK>();
//        CDT.Insert(level.GetBoundary());
//        for (int i = 0; i < level.HoleCount; i++)
//        {
//            CDT.Insert(level.GetHole(i));
//        }
//        //Get the triangles a shapes.
//        var triangles = new Triangle2d[CDT.TriangleCount];
//        CDT.GetTriangles(triangles, triangles.Length);
//        return triangles;
//    }
//
//    public void OnDrawGizmosSelected()
//    {
//        if (TringulationData.DelaynayTriangles != null)
//            DrawTriangles(TringulationData.DelaynayTriangles, Color.magenta);
//        base.OnDrawGizmosSelected();
//    }
//
//    private void DrawLevelPolygon()
//    {
//        if (TringulationData.LevelFree == null) return;
//        var boundaryPoints = TringulationData.LevelFree.GetBoundary()
//            .Select(x => new Vector2((float)x.x, (float)x.y))
//            .ToArray();
//
//        Gizmos.color = Color.red;
//        for (int j = 0; j < boundaryPoints.Count() - 1; j++)
//        {
//            Gizmos.DrawSphere(boundaryPoints[j], 0.1f);
//            Gizmos.DrawLine(boundaryPoints[j], boundaryPoints[j + 1]);
//            Gizmos.DrawSphere(boundaryPoints[j + 1], 0.1f);
//        }
//        Gizmos.DrawLine(boundaryPoints[boundaryPoints.Count() - 1], boundaryPoints[0]);
//
//        for (int i = 0; i < TringulationData.LevelFree.HoleCount; i++)
//        {
//            var hole = TringulationData.LevelFree.GetHole(i)
//            .Select(x => new Vector2((float)x.x, (float)x.y))
//            .ToArray();
//
//            Gizmos.color = Color.blue;
//            for (int j = 0; j < hole.Count() - 1; j++)
//            {
//                Gizmos.DrawSphere(hole[j], 0.1f);
//                Gizmos.DrawLine(hole[j], hole[j + 1]);
//                Gizmos.DrawSphere(hole[j + 1], 0.1f);
//            }
//            Gizmos.DrawLine(hole[hole.Count() - 1], hole[0]);
//        }
//    }
//
//    public override void Setup()
//    {
//        base.Setup();
//
//        //Attempt To Retrieve Level Data if prvious runs are run
//
//        TringulationData = level.GetComponent<DelunayTriangulatedLevelData>();
//        if (TringulationData == null)
//        {
//            //Create new data
//            TringulationData = level.AddComponent<DelunayTriangulatedLevelData>();
//            TringulationData.LevelFree = GetCGALPolygonLevel(level);
//            //LevelFree.Triangulate(new List<int>() { 0, 1, 2, 3 });
//            TringulationData.DelaynayTriangles = ConstrainedDelaunay(TringulationData.LevelFree);
//        }
//        else
//        {
//            Debug.Log("Retrieved traingulation data");
//        }
//    }
//
//    // Start is called before the first frame update
//    private void Start()
//    {
//        Setup();
//        Run();
//    }
//
//    // Update is called once per frame
//    private void Update()
//    {
//    }
//}