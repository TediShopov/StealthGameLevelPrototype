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
using CGALDotNet.Hulls;
using StealthLevelEvaluation;

//Visualizes the outtput of CGAL delanay traingulation
public class DelanayTrinagulationUnityMono : MonoBehaviour
{
    public DelaunayTriangulation2<EEK> Triangulation2;
    public UnityEngine.Vector2 VoronoiFacePosition;

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
            if (seg.IsFinite == false)
                continue;
            DrawLabelelLine(seg, iter.ToString(), Color.white);
            iter += 1;
        }

        iter = 0;
        //        Triangle2d[] triangles = new Triangle2d[Triangulation2.TriangleCount];
        //        Triangulation2.GetTriangles(triangles, Triangulation2.TriangleCount);
        //        foreach (var triangle in triangles)
        //        {
        //            Segment2d a = new Segment2d(triangle.A, triangle.B);
        //            Segment2d b = new Segment2d(triangle.C, triangle.B);
        //            Segment2d c = new Segment2d(triangle.A, triangle.C);
        //            DrawLabelelLine(a, iter.ToString(), Color.green);
        //            DrawLabelelLine(b, iter.ToString(), Color.green);
        //            DrawLabelelLine(c, iter.ToString(), Color.green);
        //
        //            iter += 1;
        //            Gizmos.color = Color.green;
        //            UnityEngine.Vector2 ceneter = ToGlobalUnity(triangle.CircumCircle.Center);
        //            Gizmos.DrawSphere(new Vector3(ceneter.x, ceneter.y, -1), 0.1f);
        //
        //            //Gizmos.DrawWireSphere(ceneter, (float)triangle.CircumRadius);
        //        }

        Point2d pointingAt = new Point2d(VoronoiFacePosition.x, VoronoiFacePosition.y);
        TriVertex2 vertex;
        if (Triangulation2.LocateVertex(pointingAt, 2.0f, out vertex))
            DrawVoronoiFaceIndex(vertex.Index);
    }

    public UnityEngine.Vector2 ToGlobalUnity(Point2d d)
    {
        return this.transform.TransformPoint(ToUnity(d));
    }
    public UnityEngine.Vector2 ToUnity(Point2d d)
    {
        return new UnityEngine.Vector2((float)d.x, (float)d.y);
    }
}

public class VDOPESLevelChromosome : LevelChromosomeBase
{
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
    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetInt(0, 2));
    }

    public override int GetHashCode()
    {
        int hash = 0;
        Gene[] genes = GetGenes();
        foreach (Gene gene in genes)
        {
            hash ^= (int)gene.Value;
        }
        return hash;
    }
    public override string ToString()
    {
        return string.Join(string.Empty, GetGenes().Select(g => g.Value.ToString()).ToArray());
    }
}

[RequireComponent(typeof(DiscretePathGenerator))]
[ExecuteInEditMode]
public class VoronoiDirectObstacleEnemyPathingStrategy : ObstacleTransformEnemyPathingStrategyLevelGenerator
{
    public int LoydRealaxationRuns = 0;
    //public float AllowedProximityToLevelPositions;

    public List<UnityEngine.Vector2> Points = new List<UnityEngine.Vector2>();

    public double Radius = 1;

    [Header("Voronoi grid setup")]
    public Vector2Int SampleGridPoints = Vector2Int.one;

    public int Samples = 50;
    private Graph<UnityEngine.Vector2> CurrentRoadMap;

    private DelaunayTriangulation2<EEK> triangulation;
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
    public void Awake()
    {
        //this.RoadmapGenerator = new FloodfilledRoadmapGenerator();
        PathGenerator = GetComponent<DiscretePathGenerator>();
    }

    public void CreatePolygonObstacle(List<UnityEngine.Vector2> points)
    {
        if (points.Count == 0) return;
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
    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        To = to;
        EnsureComponentValidity();

        //        if (chromosome is not OTEPSLevelChromosome)
        //            throw new System.ArgumentException("OTEPS Level generator requries OTEPS level chromosome");

        chromosome.Phenotype = new LevelPhenotype();
        int geneIndex = 0;

        MeasureResult geomtry = MeasureResultFromStep("Geometry Construction",
            () => { geneIndex = GenerateGeometry(chromosome, to); });

        MeasureResult roadmap = MeasureResultFromStep("Roadmap",
            () => { AssignRoadmToPhenotype(chromosome, to); });

        chromosome.Phenotype.Threats = new List<IPredictableThreat>();
        //        MeasureResult paths = MeasureResultFromStep("Path Assignment",
        //            () => { AssignPaths(geneIndex, chromosome); });

        MeasureResult future = MeasureResultFromStep("Level Future",
             () => { CalculateLevelFuture(chromosome); });

        chromosome.AddOrReplace(geomtry);
        chromosome.AddOrReplace(roadmap);
        //chromosome.AddOrReplace(paths);
        chromosome.AddOrReplace(future);

        //Add extra visualizer to provide disegner insight
        // into ediot view
        this.Data.AddComponent<RoadmapVisualizer>();
        this.Data.AddComponent<FutureLevelSlider>();
        var dt = this.Data.AddComponent<DelanayTrinagulationUnityMono>();
        dt.Triangulation2 = triangulation;

        UnityEngine.Debug.Log("Generation of phenotype finished");
    }
    public void GenerateVoronoiObstacles(UnboundedGrid grid)
    {
        //CurrentRoadMap = new Graph<UnityEngine.Vector2>();
        int width = SampleGridPoints.x;
        int height = SampleGridPoints.y;

        var points = GridBasedPoints(grid);

        //        for (int i = 0; i < points.Count; i++)
        //        {
        //            points[i] = new Point2d(points[i].x - width / 2.0f, points[i].y - height / 2.0f);
        //        }
        triangulation = new DelaunayTriangulation2<EEK>();
        var array = points.ToArray();
        triangulation.Insert(array, array.Length);
        if (triangulation == null) return;
    }
    public List<Point2d> GridBasedPoints(UnboundedGrid grid)
    {
        UnityEngine.Vector2 min = new UnityEngine.Vector2(
            -this.LevelProperties.LevelSize.x / 2.0f,
            -this.LevelProperties.LevelSize.y / 2.0f
            );
        UnityEngine.Vector2 max = new UnityEngine.Vector2(
            this.LevelProperties.LevelSize.x / 2.0f,
            this.LevelProperties.LevelSize.y / 2.0f
            );

        Vector3Int minCell = grid.WorldToCell(min);
        Vector3Int maxCell = grid.WorldToCell(max);

        var points = new List<Point2d>();
        for (int i = minCell.x; i <= maxCell.x; i++)
        {
            for (int j = minCell.y; j <= maxCell.y; j++)
            {
                Vector3 cellCenter = grid.GetCellCenterWorld(new Vector3Int(i, j, 0));
                //Randmoize a value inside the cell
                Vector3 localRandomPos = new Vector3(
                    UnityEngine.Random.Range(-grid.cellSize / 2.0f, grid.cellSize / 2.0f),
                    UnityEngine.Random.Range(-grid.cellSize / 2.0f, grid.cellSize / 2.0f),
                    0
                    );

                points.Add(new Point2d(cellCenter.x + localRandomPos.x, cellCenter.y + localRandomPos.y));
            }
        }
        return points;
    }
    public override LevelChromosomeBase GetAdamChromosome(int s)
    {
        return new VDOPESLevelChromosome(SampleGridPoints.x * SampleGridPoints.y, this, new System.Random(s));
    }
    public List<UnityEngine.Vector2> GetPointsOfVoronoiRegion(int i)
    {
        if (i <= 0 || i > triangulation.TriangleCount) return new List<UnityEngine.Vector2>();
        // each voronoi face corrsponds to DT vertex
        //Get dt vertex at i

        TriVertex2 triVertex2 = new TriVertex2();
        triangulation.GetVertex(i, out triVertex2);

        var triangles = new Triangle2d[triangulation.TriangleCount];
        triangulation.GetTriangles(triangles, triangulation.TriangleCount);

        var relatedTriangles = triangles.
            Where(x => x.A == triVertex2.Point
            || x.B == triVertex2.Point
            || x.C == triVertex2.Point)
            .ToList();

        int iter = 1;

        var CGALPoints = new List<Point2d>();
        foreach (var triangle in relatedTriangles)
        {
            var center = triangle.CircumCircle.Center;
            if (center.x > SampleGridPoints.x || center.y > SampleGridPoints.y)
                return new List<UnityEngine.Vector2>();
            if (center.x < 0 || center.y < 0)
                return new List<UnityEngine.Vector2>();
            CGALPoints.Add(triangle.CircumCircle.Center);
        }
        if (relatedTriangles.Count < 3)
            return new List<UnityEngine.Vector2>();

        ConvexHull2<EEK> convexHull2 = new ConvexHull2<EEK>();
        var hull = convexHull2.CreateHull(CGALPoints.ToArray(), CGALPoints.Count, HULL_METHOD.DEFAULT);
        var pointsToReturn = new Point2d[hull.Count];
        hull.GetPoints(pointsToReturn, hull.Count);
        if (hull.IsValid())
            return pointsToReturn.Select(x => ToGlobalUnity(x)).ToList();
        return new List<UnityEngine.Vector2>();
    }
    public UnityEngine.Vector2 ToGlobalUnity(Point2d d)
    {
        return this.transform.TransformPoint(ToUnity(d));
    }
    public UnityEngine.Vector2 ToUnity(Point2d d)
    {
        return new UnityEngine.Vector2((float)d.x, (float)d.y);
    }
    protected override int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;
        GenerateVoronoiObstacles(new UnboundedGrid(chromosome.Manifestation.transform.position, 1.0f));

        Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(Contents.transform);

        List<UnityEngine.Vector2> polygonPoints = new List<UnityEngine.Vector2>();

        //Get the triangulation index count
        for (int i = 0; i < triangulation.IndiceCount; i++)
        {
            //Get gene
            bool isObstacle = i % 1 == 0;
            if (isObstacle)
            {
                CreatePolygonObstacle(GetPointsOfVoronoiRegion(i));
            }

            polygonPoints.Clear();
        }

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