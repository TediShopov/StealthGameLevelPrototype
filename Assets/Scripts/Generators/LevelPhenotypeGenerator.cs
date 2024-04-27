using JetBrains.Annotations;
using Mono.Cecil;
using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.U2D;

//GENOTYPE desciprtion:
public class LevelObstalceData
{
    public LevelObstalceData(List<Vector2> obstaclePoints)
    {
        if (obstaclePoints.Count <= 2)
        {
            throw new System.ArgumentException(
                $"Obstacle polygon cannot be created with P{obstaclePoints.Count}");
        }
        PolygonData = obstaclePoints;
        Position = Vector3.zero;
        Rotaiton = 0;
        Scale = 1;
    }

    public Vector2 Position;
    public float Rotaiton = 0;
    public float Scale = 1;
    public List<Vector2> PolygonData = new List<Vector2>();
}

public interface IStealthLevelPhenotype
{
    //This are the bounds of the level in 2d
    Bounds GetBounds();

    Vector2 StartPosition { get; }
    Vector2 GoalPosition { get; }

    //Obstacles info
    List<LevelObstalceData> GetObstacles();

    void AddObstacle(LevelObstalceData obstacle);

    //Dynamic threats of the level
    List<IPredictableThreat> GetThreats();
}

public class StealthLevel : IStealthLevelPhenotype
{
    public StealthLevel(Bounds bounds)
    {
        this.Bounds = bounds;
        this.Obstacles = new List<LevelObstalceData>();
        this.Threats = new List<IPredictableThreat>();
    }

    public Vector2 StartPosition { get; set; }
    public Vector2 GoalPosition { get; set; }

    public Bounds GetBounds() => Bounds;

    public List<LevelObstalceData> GetObstacles() => Obstacles;

    public List<IPredictableThreat> GetThreats() => Threats;

    public void AddObstacle(LevelObstalceData obstacle)
    {
        this.Obstacles.Add(obstacle);
    }

    private Bounds Bounds;
    private List<LevelObstalceData> Obstacles;
    private List<IPredictableThreat> Threats;
}

public interface ILevelPhenotypeGenerator
{
    IStealthLevelPhenotype GeneratePhenotype(LevelChromosomeBase levelChromosome);
}

[Serializable]
internal class LevelChromosomeMono : MonoBehaviour
{
    [SerializeField] public LevelChromosomeBase Chromosome;
}

public abstract class LevelPhenotypeGenerator : LevelGeneratorBase
{
    public bool RunOnStart = false;
    public bool IsRandom = false;

    //public System.Random RandomSeed;
    public bool DisposeNow = false;

    public int MinEnemiesSpawned = 1;
    public int MaxEnemiesSpawned = 3;
    public int StartingObstacleCount = 3;

    [Range(1, 5)]
    public float MinObjectScale;

    [Range(1, 5)]
    public float MaxObjectScale;

    protected LevelChromosomeBase LevelChromosome;
    public int IndexOfChromosome;
    public Sprite Sprite;

    //    public void Awake()
    //    {
    //        if (RunOnStart)
    //        {
    //            if (IsRandom)
    //                RandomChromosomeSeed = new System.Random().Next();
    //            LevelChromosome = new OTEPSLevelChromosome(StartingObstacleCount * 5 + 4, this, new System.Random(RandomChromosomeSeed));
    //            Generate(LevelChromosome, this.gameObject);
    //        }
    //    }

    public List<Vector2> SquarePoints = new List<Vector2>
    {
            new Vector2(-0.5f,-0.5f),
            new Vector2(0.5f,-0.5f),
            new Vector2(0.5f,0.5f),
            new Vector2(-0.5f,0.5f)
    };

    public List<Vector2> TrianglePoints = new List<Vector2>
    {
            new Vector2(0,0.5f),
            new Vector2(-0.25f, 0),
            new Vector2(0.25f,0),
    };

    protected LevelObstalceData SpawnObstacleFromListData(ref int geneIndex, IStealthLevelPhenotype level)
    {
        //Get Obstacle Variant
        int prefabIndex =
            (int)(GetGeneValue(geneIndex) * LevelProperties.ObstaclePrefabs.Count);
        //It is possible to retrieve points on the spline of the sprite shape controller.
        //However that is harder to do https://forum.unity.com/threads/tell-us-about-your-experience-with-sprite-shape.686305/page-3#post-6083610
        //        List<Vector2> polygonPoints = new List<Vector2>();
        //        var controller = LevelProperties.ObstaclePrefabs[prefabIndex].GetComponent<PolygonCollider2D>();
        //        Spline spline = controller.spline;
        //        for (int i = 0; i < spline.GetPointCount(); i++)
        //        {
        //            polygonPoints.Add(spline.GetPosition(i));
        //        }
        List<Vector2> polygonPoints;
        if (prefabIndex == 0)
            polygonPoints = SquarePoints;
        else polygonPoints = TrianglePoints;

        Bounds bounds = level.GetBounds();
        float x = Mathf.Lerp(bounds.min.x, bounds.max.x, GetGeneValue(geneIndex + 1));
        float y = Mathf.Lerp(bounds.min.y, bounds.max.y, GetGeneValue(geneIndex + 2));
        float rot = Mathf.Lerp(0, 360, GetGeneValue(geneIndex + 3));
        float scl = Mathf.Lerp(MinObjectScale, MaxObjectScale, GetGeneValue(geneIndex + 4));

        LevelObstalceData squareObstacle = new LevelObstalceData(polygonPoints);
        squareObstacle.Position = new Vector2(x, y);
        squareObstacle.Scale = scl;
        squareObstacle.Rotaiton = rot;

        geneIndex += 5;
        level.AddObstacle(squareObstacle);
        return squareObstacle;
    }

    public override IStealthLevelPhenotype GeneratePhenotype(LevelChromosomeBase levelChromosome)
    {
        LevelChromosome = levelChromosome;
        Bounds levelBounds = new Bounds(
            new Vector2(0, 0),
            LevelProperties.LevelSize);

        StealthLevel stealthLevel = new StealthLevel(levelBounds);

        int geneIndex = 0;
        int ObstaclesSpawned = (levelChromosome.Length - 4) / 5;

        //Test for off by oen errors
        for (int i = 0; i < ObstaclesSpawned; i++)
        {
            SpawnObstacleFromListData(ref geneIndex, stealthLevel);
        }
        return stealthLevel;
    }

    public abstract LevelChromosomeBase GetAdamChromosome(int seed);

    //    public virtual void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    //    {
    //        //Base generation of the chromosome templatej
    //        this.CreateLevelStructure(to);
    //
    //        AttachChromosome(chromosome);
    //
    //        GenerateLevelContent(chromosome);
    //
    //        PlaceBoundaryVisualPrefabs();
    //        //Solvers
    //        InitializeAdditionalLevelData();
    //
    //        Debug.Log("Generation of phenotype finished");
    //    }

    public void ClearName(LevelChromosomeBase chromosome)
    {
        if (chromosome == null || chromosome.Phenotype == null)
            return;

        GameObject levelGameObject = chromosome.Phenotype;
        levelGameObject.name = "";
    }

    public void AppendToName(LevelChromosomeBase chromosome, string text)
    {
        if (chromosome == null || chromosome.Phenotype == null)
            return;

        GameObject levelGameObject = chromosome.Phenotype;
        levelGameObject.name += text;
    }

    public void AppendFitnessToName(LevelChromosomeBase chromosome)
    {
        if (chromosome == null || chromosome.Phenotype == null)
            return;
        if (chromosome.Fitness.HasValue)
        {
            chromosome.Phenotype.name += $"Fitness: {chromosome.Fitness}";
        }
    }

    //    protected virtual int GenerateLevelContent(LevelChromosomeBase chromosome)
    //    {
    //        int geneIndex = 0;
    //        int ObstaclesSpawned = (chromosome.Length - 4) / 5;
    //        var Obstacles = new GameObject("Obstacles");
    //ILevelManifestor manifestor
    //        //Test for off by oen errors
    //        for (int i = 0; i < ObstaclesSpawned; i++)
    //        {
    //            SpawnObstacle(ref geneIndex, LevelBounds, Obstacles);
    //        }
    //
    //        //Read enemy counts and spawn enemies
    //        int enemyCount = Mathf.CeilToInt(
    //            Mathf.Lerp(
    //                MinEnemiesSpawned,
    //                MaxEnemiesSpawned,
    //                geneIndex));
    //
    //        for (int i = 0; i < enemyCount; i++)
    //        {
    //            Instantiate(LevelProperties.EnemyPrefab, To.transform);
    //        }
    //
    //        //Enemy Behaviour
    //
    //        var pathGenerator =
    //            To.GetComponentInChildren<PathGeneratorClass>();
    //
    //        //Enemy path geenerator seed
    //        int pathSeed = Mathf.CeilToInt(
    //            GetGeneValue(geneIndex)
    //            * GetGeneValue(geneIndex + 1)
    //            * GetGeneValue(geneIndex + 2)
    //            * GetGeneValue(geneIndex + 3));
    //        geneIndex += 4;
    //        pathGenerator.LevelRandom = new System.Random(pathSeed);
    //
    //        Physics2D.SyncTransforms();
    //        return geneIndex;
    //    }
    //
    //    protected void InitializeAdditionalLevelData()
    //    {
    //        var levelInitializer = To.gameObject.GetComponentInChildren<InitializeStealthLevel>();
    //        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
    //        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
    //        //var multipleRRTSolvers = To.gameObject.GetComponentInChildren<MultipleRRTRunner>();
    //        Helpers.LogExecutionTime(levelInitializer.Init, "Level Initializer Time");
    //        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
    //    }

    //!WARNING! uses destroy immediate as mulitple level can be geenrated an
    //disposed in the same frame
    public void Dispose()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    public float GetGeneValue(int index) => (float)LevelChromosome.GetGene(index).Value;

    //    protected GameObject SpawnObstacle(ref int geneIndex, BoxCollider2D box, GameObject Obstacles)
    //    {
    //        //Get Obstacle Variant
    //        int prefabIndex = (int)(GetGeneValue(geneIndex) * LevelProperties.ObstaclePrefabs.Count) + 1;
    //        GameObject ObstaclePrefabVariant = LevelProperties.ObstaclePrefabs[prefabIndex - 1];
    //
    //        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, GetGeneValue(geneIndex + 1));
    //        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, GetGeneValue(geneIndex + 2));
    //        float rot = Mathf.Lerp(0, 360, GetGeneValue(geneIndex + 3));
    //        float scl = Mathf.Lerp(MinObjectScale, MaxObjectScale, GetGeneValue(geneIndex + 4));
    //
    //        geneIndex += 5;
    //
    //        var obs = Instantiate(ObstaclePrefabVariant,
    //            new Vector3(x, y, 0),
    //            Quaternion.Euler(0, 0, rot),
    //            Obstacles.transform);
    //        ObstaclePrefabVariant.transform.localScale = new Vector3(scl, scl, 0);
    //        return obs;
    //    }
    //
    //    protected GameObject SpawnGameObject(ref int geneIndex, BoxCollider2D box, GameObject Prefab)
    //    {
    //        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, GetGeneValue(geneIndex));
    //        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, GetGeneValue(geneIndex + 1));
    //
    //        geneIndex += 2;
    //        var player = Instantiate(Prefab,
    //            new Vector3(x, y, 0),
    //            Quaternion.Euler(0, 0, 0),
    //            To.transform);
    //        return player;
    //    }

    // Update is called once per frame
    private void Update()
    {
        if (DisposeNow)
        {
            Dispose();
            DisposeNow = false;
        }
    }
}