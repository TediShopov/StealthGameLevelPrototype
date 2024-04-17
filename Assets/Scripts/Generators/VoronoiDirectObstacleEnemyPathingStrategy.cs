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

        var roadmap = RoadmapGenerator.PrototypeComponent(Data);
        roadmap.Init(to);
        //        chromosome.Measurements.Add(roadmap.Result);
        GenerateVoronoiObstacles();
        roadmap.RoadMap = CurrentRoadMap;

        int geneIndex = GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();

        Physics2D.SyncTransforms();

        //Copy grid components of the level prototype.
        var otherGrid = Data.AddComponent<Grid>();
        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;

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

    public void CreatePolygonAtVoronoiSegment(List<UnityEngine.Vector2> points)
    {
        var poly = new GameObject("Poly");
        var polygonCollider = poly.AddComponent<PolygonCollider2D>();
        polygonCollider.points = points.ToArray();
        poly.transform.SetParent(Obstacles.transform, true);
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(Contents.transform);

        List<UnityEngine.Vector2> polygonPoints = new List<UnityEngine.Vector2>();
        for (int i = 0; i < chromosome.Length; i++)
        {
            //bool isObstacle = (bool)chromosome.GetGene(i).Value;
            TriFace2 face = triangulation.GetFace(i);
            IEnumerable<TriVertex2> triVertices = face.EnumerateVertices(triangulation);
            foreach (var triVertex in triVertices)
            {
                polygonPoints.Add(new UnityEngine.Vector2((float)triVertex.Point.x, (float)triVertex.Point.y));
            }
            polygonPoints.Clear();
        }

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