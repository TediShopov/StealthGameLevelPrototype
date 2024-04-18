using CGALDotNet.Triangulations;
using CGALDotNet;
using CGALDotNetGeometry.Numerics;
using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CGALDotNet.Geometry;
using CGALDotNetGeometry.Shapes;
using PlasticGui;
using UnityEngine.U2D;
using UnityEditor;

public class VDOPESLevelChromosome : LevelChromosomeBase
{
    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetInt(0, 2));
    }

    public override string ToString()
    {
        return string.Join(string.Empty, GetGenes().Select(g => g.Value.ToString()).ToArray());
    }

    public VDOPESLevelChromosome(int length,
        VoronoiDirectObstacleEnemyPathingStrategy generatorBase = null, System.Random random = null) :
        base(length, generatorBase)
    {
        if (random == null)
            ChromosomeRandom = new System.Random();
        else
            ChromosomeRandom = random;

        var newGeneArrayt = new Gene[length];
        for (int i = 0; i < length; i++)
        {
            newGeneArrayt[i] = GenerateGene(i);
        }
        this.ReplaceGenes(0, newGeneArrayt);
    }

    public override IChromosome CreateNew()
    {
        return new VDOPESLevelChromosome(
            this.Length,
            (VoronoiDirectObstacleEnemyPathingStrategy)this.PhenotypeGenerator,
            ChromosomeRandom);
    }

    public override int GetHashCode()
    {
        int hash = 0;
        Gene[] genes = GetGenes();
        foreach (Gene gene in genes)
        {
            float number = (float)gene.Value;
            int scaledNumber = Mathf.RoundToInt(number / 0.0001f);
            hash ^= (hash << 5) ^ (hash >> 3) ^ scaledNumber;
        }
        return hash;
    }
}

//Visualizes the outtput of CGAL delanay traingulation
public class DelanayTrinagulationUnityMono : MonoBehaviour
{
    public DelaunayTriangulation2<EEK> Triangulation2;
    public UnityEngine.Vector2 VoronoiFacePosition;

    public void DrawVoronoiFaceIndex(int i)
    {
        if (i <= 0 || i > Triangulation2.TriangleCount) return;
        // each voronoi face corrsponds to DT vertex
        //Get dt vertex at i

        TriVertex2 triVertex2 = new TriVertex2();
        Triangulation2.GetVertex(i, out triVertex2);

        var triangles = new Triangle2d[Triangulation2.TriangleCount];
        Triangulation2.GetTriangles(triangles, Triangulation2.TriangleCount);

        var relatedTriangles = triangles.
            Where(x => x.A == triVertex2.Point
            || x.B == triVertex2.Point
            || x.C == triVertex2.Point)
            .ToList();

        int iter = 1;
        foreach (var triangle in relatedTriangles)
        {
            Segment2d a = new Segment2d(triangle.A, triangle.B);
            Segment2d b = new Segment2d(triangle.C, triangle.B);
            Segment2d c = new Segment2d(triangle.A, triangle.C);
            DrawLabelelLine(a, iter.ToString(), Color.red);
            DrawLabelelLine(b, iter.ToString(), Color.red);
            DrawLabelelLine(c, iter.ToString(), Color.red);

            iter += 1;
            Gizmos.color = Color.magenta;
            UnityEngine.Vector2 ceneter = ToGlobalUnity(triangle.CircumCircle.Center);
            Gizmos.DrawSphere(new Vector3(ceneter.x, ceneter.y, -1), 0.1f);

            //Gizmos.DrawWireSphere(ceneter, (float)triangle.CircumRadius);
        }
    }

    public void OnDrawGizmos()
    {
        if (Triangulation2 == null) return;
        int iter = 0;

        foreach (Segment2d seg in Triangulation2.GetVoronoiSegments())
        {
            DrawLabelelLine(seg, iter.ToString(), Color.white);
            iter += 1;
        }

        iter = 0;
        Triangle2d[] triangles = new Triangle2d[Triangulation2.TriangleCount];
        Triangulation2.GetTriangles(triangles, Triangulation2.TriangleCount);
        foreach (var triangle in triangles)
        {
            Segment2d a = new Segment2d(triangle.A, triangle.B);
            Segment2d b = new Segment2d(triangle.C, triangle.B);
            Segment2d c = new Segment2d(triangle.A, triangle.C);
            DrawLabelelLine(a, iter.ToString(), Color.green);
            DrawLabelelLine(b, iter.ToString(), Color.green);
            DrawLabelelLine(c, iter.ToString(), Color.green);

            iter += 1;
            Gizmos.color = Color.green;
            UnityEngine.Vector2 ceneter = ToGlobalUnity(triangle.CircumCircle.Center);
            Gizmos.DrawSphere(new Vector3(ceneter.x, ceneter.y, -1), 0.1f);

            //Gizmos.DrawWireSphere(ceneter, (float)triangle.CircumRadius);
        }

        Point2d pointingAt = new Point2d(VoronoiFacePosition.x, VoronoiFacePosition.y);
        TriVertex2 vertex;
        if (Triangulation2.LocateVertex(pointingAt, 2.0f, out vertex))
            DrawVoronoiFaceIndex(vertex.Index);
    }

    public UnityEngine.Vector2 ToUnity(Point2d d)
    {
        return new UnityEngine.Vector2((float)d.x, (float)d.y);
    }

    public UnityEngine.Vector2 ToGlobalUnity(Point2d d)
    {
        return this.transform.TransformPoint(ToUnity(d));
    }

    public void DrawLabelelLine(Segment2d seg, string label, Color color)
    {
        UnityEngine.Vector2 start = ToUnity(seg.A);
        UnityEngine.Vector2 end = ToUnity(seg.B);

        //Transform to local
        start = this.transform.TransformPoint(start);
        end = this.transform.TransformPoint(end);

        Gizmos.color = color;
        Handles.Label(UnityEngine.Vector2.MoveTowards(start, end, 0.5f), label);
        Gizmos.DrawSphere(start, 0.2f);
        Gizmos.DrawSphere(end, 0.2f);
        Gizmos.DrawLine(start, end);
    }
}

[RequireComponent(typeof(FloodfilledRoadmapGenerator))]
[RequireComponent(typeof(DiscretePathGenerator))]
[RequireComponent(typeof(IFutureLevel))]
[ExecuteInEditMode]
public class VoronoiDirectObstacleEnemyPathingStrategy : LevelPhenotypeGenerator
{
    public int LoydRealaxationRuns = 0;
    //public float AllowedProximityToLevelPositions;

    [Header("Voronoi grid setup")]
    public Vector2Int SampleGridPoints = Vector2Int.one;

    public double Radius = 1;
    public int Samples = 50;
    public int EnemyCount = 3;
    [HideInInspector] public DiscretePathGenerator PathGenerator;
    [HideInInspector] public FloodfilledRoadmapGenerator RoadmapGenerator;
    public DiscreteRecalculatingFutureLevel FutureLevel;
    private Graph<UnityEngine.Vector2> CurrentRoadMap;

    public void Awake()
    {
        this.RoadmapGenerator = GetComponent<FloodfilledRoadmapGenerator>();
        PathGenerator = GetComponent<DiscretePathGenerator>();
    }

    public override LevelChromosomeBase GetAdamChromosome(int s)
    {
        return new VDOPESLevelChromosome(SampleGridPoints.x * SampleGridPoints.y, this, new System.Random(s));
    }

    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        if (chromosome is not VDOPESLevelChromosome)
            throw new System.ArgumentException("VDOEPS Level generator requries VDOEPS level chromosome");

        CreateLevelStructure(to);

        //Setup chromosome
        AttachChromosome(chromosome);

        //TODO- this is here as roadmap geenrator requires a graphs
        //Copy grid components of the level prototype.
        var otherGrid = Data.AddComponent<Grid>();
        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;

        //        var roadmap = RoadmapGenerator.PrototypeComponent(Data);
        //        roadmap.Init(to);
        //        //        chromosome.Measurements.Add(roadmap.Result);
        GenerateVoronoiObstacles();
        //        roadmap.RoadMap = CurrentRoadMap;

        //Attach debug visualizer.
        var vis = Data.AddComponent<DelanayTrinagulationUnityMono>();
        vis.Triangulation2 = triangulation;

        int geneIndex = GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();

        Physics2D.SyncTransforms();

        //Use the generated roadmap to assign guard paths
        //AssignPaths(geneIndex, chromosome.EnemyRoadmap);

        //Initialize the future level
        //CopyComponent(FutureLevel, To).Init(To);
        var futurePrototype = FutureLevel.PrototypeComponent(Data);
        futurePrototype.Init();

        Debug.Log("Generation of phenotype finished");
    }

    public List<UnityEngine.Vector2> Points = new List<UnityEngine.Vector2>();
    private DelaunayTriangulation2<EEK> triangulation;

    public void GenerateVoronoiObstacles()
    {
        CurrentRoadMap = new Graph<UnityEngine.Vector2>();
        int width = SampleGridPoints.x;
        int height = SampleGridPoints.y;

        var points = CreateBoundaryPoints(width, height, Mathf.CeilToInt((float)Radius));
        FillPoints(points, width, height, Radius, Samples);
        ExpandPoints(points, width, height, Mathf.CeilToInt((float)Radius));

        for (int i = 0; i < points.Count; i++)
        {
            points[i] = new Point2d(points[i].x - width / 2.0f, points[i].y - height / 2.0f);
        }
        triangulation = new DelaunayTriangulation2<EEK>();
        var array = points.ToArray();
        triangulation.Insert(array, array.Length);
        if (triangulation == null) return;

        foreach (Segment2d seg in triangulation.GetVoronoiSegments())
        {
            UnityEngine.Vector2 start = new UnityEngine.Vector2((float)seg.A.x, (float)seg.A.y);
            UnityEngine.Vector2 end = new UnityEngine.Vector2((float)seg.B.x, (float)seg.B.y);

            //Transform to local
            start = To.transform.TransformPoint(start);
            end = To.transform.TransformPoint(end);

            //Must be inside boundaryu

            if (LevelBounds.OverlapPoint(start) && LevelBounds.OverlapPoint(end))
            {
                CurrentRoadMap.AddNode(start);
                CurrentRoadMap.AddNode(end);
                CurrentRoadMap.AddEdge(start, end);

                if (!Points.Contains(start))
                    Points.Add(start);
                if (!Points.Contains(end))
                    Points.Add(end);
            }
        }
    }

    public void AssignPaths(int geneIndex, Graph<UnityEngine.Vector2> roadmap)
    {
        LevelRandom = new System.Random();
        PathGenerator.geneIndex = geneIndex;
        PathGenerator.Init(To);
        PathGenerator.Roadmap = roadmap;
        PathGenerator.LevelRandom = LevelRandom;
        PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
        List<List<UnityEngine.Vector2>> paths = PathGenerator.GeneratePaths(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            enemyPaths[i].InitPatrol(paths[i]);
        }
        geneIndex = PathGenerator.geneIndex;
    }

    private void FillPoints(List<Point2d> points, int width, int height, double radius, int samples)
    {
        for (int i = 0; i < samples; i++)
        {
            var point = new Point2d();
            point.x = UnityEngine.Random.Range(0, width);
            point.y = UnityEngine.Random.Range(0, height);

            if (!WithInRadius(point, points, radius))
            {
                points.Add(point);
            }
        }
    }

    private bool WithInRadius(Point2d point, List<Point2d> points, double radius)
    {
        double radius2 = radius * radius;
        foreach (var p in points)
        {
            if (Point2d.SqrDistance(point, p) < radius2)
                return true;
        }

        return false;
    }

    private List<Point2d> ExpandPoints(List<Point2d> points, int width, int height, int radius)
    {
        points.Add(new Point2d(-radius, -radius));
        points.Add(new Point2d(width + radius, -radius));
        points.Add(new Point2d(-radius, height + radius));
        points.Add(new Point2d(width + radius, height + radius));

        for (int i = 0; i <= width; i += radius)
        {
            points.Add(new Point2d(i, -radius));
            points.Add(new Point2d(i, width + radius));

            points.Add(new Point2d(-radius, i));
            points.Add(new Point2d(width + radius, i));
        }

        return points;
    }

    private List<Point2d> CreateBoundaryPoints(int width, int height, int radius)
    {
        var points = new List<Point2d>();

        points.Add(new Point2d(0, 0));
        points.Add(new Point2d(width, 0));
        points.Add(new Point2d(0, height));
        points.Add(new Point2d(width, height));

        for (int i = radius; i <= width - radius; i += radius)
        {
            points.Add(new Point2d(i, 0));
            points.Add(new Point2d(i, width));

            points.Add(new Point2d(0, i));
            points.Add(new Point2d(width, i));
        }

        return points;
    }

    public void CreatePolygonObstacle(List<UnityEngine.Vector2> points)
    {
        var poly = new GameObject("Poly");

        //Acquire center of points
        UnityEngine.Vector2 Center = UnityEngine.Vector2.zero;
        foreach (var p in points)
            Center += p;
        Center /= points.Count;

        poly.transform.position = Center;
        var sprite = poly.AddComponent<SpriteShapeRenderer>();
        //var spriteController = poly.AddComponent<shapeControjk>();
        var polygonCollider = poly.AddComponent<PolygonCollider2D>();

        for (int i = 0; i < points.Count; i++)
        {
            points[i] -= Center;
        }

        polygonCollider.points = points.ToArray();
        poly.transform.SetParent(Obstacles.transform, true);
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;

        Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(Contents.transform);

        List<UnityEngine.Vector2> polygonPoints = new List<UnityEngine.Vector2>();

        //        TriFace2[] faces = new TriFace2[5];
        //        triangulation.GetFaces(faces, 5);

        //Get the triangulation index count

        for (int i = 0; i < triangulation.TriangleCount; i++)
        {
            //Get gene
            bool isObstacle = i % 10 == 0;
            if (isObstacle)
            {
                //IEnumerable<TriVertex2> triVertices = triangulation.GetFace(i).EnumerateVertices(triangulation);
                for (int j = 0; j < 3; j++)
                {
                    //Local level coordinates
                    UnityEngine.Vector2 unityPoint =
                        new UnityEngine.Vector2(
                            (float)triangulation.GetTriangle(i)[j].x,
                        (float)triangulation.GetTriangle(i)[j].y);

                    //Global coordinates
                    unityPoint = To.transform.TransformPoint(unityPoint);

                    //Add to coordinates

                    polygonPoints.Add(unityPoint);
                }
                //                foreach (var triVertex in triangulation.GetTriangle(i))
                //                {
                //                    //Local level coordinates
                //                    UnityEngine.Vector2 unityPoint =
                //                        new UnityEngine.Vector2((float)triVertex.Point.x, (float)triVertex.Point.y);
                //
                //                    //Global coordinates
                //                    unityPoint = To.transform.TransformPoint(unityPoint);
                //
                //                    //Add to coordinates
                //                    polygonPoints.Add(unityPoint);
                //                }
                CreatePolygonObstacle(polygonPoints);
            }

            polygonPoints.Clear();
        }

        //For each triangle face (structure that known neighborus)
        //add to polygon points

        //        for (int i = 0; i < chromosome.Length; i++)
        //        {
        //            //bool isObstacle = (bool)chromosome.GetGene(i).Value;
        //            TriFace2 face = triangulation.GetFace(i);
        //            IEnumerable<TriVertex2> triVertices = face.EnumerateVertices(triangulation);
        //            foreach (var triVertex in triVertices)
        //            {
        //                polygonPoints.Add(new UnityEngine.Vector2((float)triVertex.Point.x, (float)triVertex.Point.y));
        //            }
        //            polygonPoints.Clear();
        //        }

        //Test for off by oen errors
        //        for (int i = 0; i < ObstacleCount; i++)
        //        {
        //            SpawnObstacle(ref geneIndex, LevelBounds, Obstacles);
        //        }

        //Read enemy counts and spawn enemies

        for (int i = 0; i < EnemyCount; i++)
        {
            Instantiate(LevelProperties.EnemyPrefab, Contents.transform);
        }

        //Enemy Behaviour
        Physics2D.SyncTransforms();
        return geneIndex;
    }
}