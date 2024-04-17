using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

[RequireComponent(typeof(DiscretePathGenerator))]
[RequireComponent(typeof(IFutureLevel))]
[ExecuteInEditMode]
public class VoronoiDirectObstacleEnemyPathingStrategy : LevelPhenotypeGenerator
{
    public int LoydRealaxationRuns = 0;
    public Vector2Int SampleGridPoints = Vector2Int.one;
    public int EnemyCount = 3;
    [HideInInspector] public DiscretePathGenerator PathGenerator;
    public DiscreteRecalculatingFutureLevel FutureLevel;

    public void Awake()
    {
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

        int geneIndex = GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();

        Physics2D.SyncTransforms();

        //Copy grid components of the level prototype.
        var otherGrid = Data.AddComponent<Grid>();
        otherGrid.cellSize = this.GetComponent<Grid>().cellSize;
        otherGrid.cellSwizzle = this.GetComponent<Grid>().cellSwizzle;
        otherGrid.cellLayout = this.GetComponent<Grid>().cellLayout;

        //        var roadmap = RoadmapGenerator.PrototypeComponent(Data);
        //        roadmap.Init(to);
        //        roadmap.DoMeasure(to);
        //        chromosome.Measurements.Add(roadmap.Result);

        //Use the generated roadmap to assign guard paths
        AssignPaths(geneIndex, chromosome.EnemyRoadmap);

        //Initialize the future level
        //CopyComponent(FutureLevel, To).Init(To);
        var futurePrototype = FutureLevel.PrototypeComponent(Data);
        futurePrototype.Init();

        Debug.Log("Generation of phenotype finished");
    }

    public void GenerateVoronoiObstacles()
    {
    }

    public void AssignPaths(int geneIndex, Graph<Vector2> roadmap)
    {
        LevelRandom = new System.Random();
        PathGenerator.geneIndex = geneIndex;
        PathGenerator.Init(To);
        PathGenerator.Roadmap = roadmap;
        PathGenerator.LevelRandom = LevelRandom;
        PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
        List<List<Vector2>> paths = PathGenerator.GeneratePaths(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            enemyPaths[i].InitPatrol(paths[i]);
        }
        geneIndex = PathGenerator.geneIndex;
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(Contents.transform);

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