using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

//GENOTYPE desciprtion:

[Serializable]
internal class LevelChromosomeMono : MonoBehaviour
{
    [SerializeField] public LevelChromosome Chromosome;
}

public class LevelPhenotypeGenerator : LevelGeneratorBase
{
    public bool RunOnStart = false;
    public bool IsRandom = false;
    public int RandomChromosomeSeed;
    public bool DisposeNow = false;
    public int MinEnemiesSpawned = 1;
    public int MaxEnemiesSpawned = 3;
    public int StartingObstacleCount = 3;

    protected LevelChromosomeBase LevelChromosome;
    public int IndexOfChromosome;

    public void Awake()
    {
        if (RunOnStart)
        {
            if (IsRandom)
                RandomChromosomeSeed = new System.Random().Next();
            LevelChromosome = new LevelChromosome(StartingObstacleCount * 5 + 4, this, new System.Random(RandomChromosomeSeed));
            Generate(LevelChromosome, this.gameObject);
        }
    }

    public virtual void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        //Base generation of the chromosome templatej
        this.CreateLevelStructure(to);

        AttachChromosome(chromosome);

        GenerateLevelContent(chromosome);

        PlaceBoundaryVisualPrefabs();
        //Solvers
        InitializeAdditionalLevelData();

        Debug.Log("Generation of phenotype finished");
    }

    private void AttachChromosome(LevelChromosomeBase chromosome)
    {
        LevelChromosome = chromosome;
        LevelChromosomeMono chromosomeMono = Data.AddComponent<LevelChromosomeMono>();
        chromosomeMono.Chromosome = (LevelChromosome)chromosome;
    }

    protected virtual int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        int geneIndex = 0;
        int ObstaclesSpawned = (chromosome.Length - 4) / 5;
        var Obstacles = new GameObject("Obstacles");

        //Test for off by oen errors
        for (int i = 0; i < ObstaclesSpawned; i++)
        {
            SpawnObstacle(ref geneIndex, LevelBounds, Obstacles);
        }

        //Read enemy counts and spawn enemies
        int enemyCount = Mathf.CeilToInt(
            Mathf.Lerp(
                MinEnemiesSpawned,
                MaxEnemiesSpawned,
                geneIndex));

        for (int i = 0; i < enemyCount; i++)
        {
            Instantiate(LevelProperties.EnemyPrefab, To.transform);
        }

        //Enemy Behaviour

        var pathGenerator =
            To.GetComponentInChildren<PathGeneratorClass>();

        //Enemy path geenerator seed
        int pathSeed = Mathf.CeilToInt(
            GetGeneValue(geneIndex)
            * GetGeneValue(geneIndex + 1)
            * GetGeneValue(geneIndex + 2)
            * GetGeneValue(geneIndex + 3));
        geneIndex += 4;
        pathGenerator.LevelRandom = new System.Random(pathSeed);

        Physics2D.SyncTransforms();
        return geneIndex;
    }

    protected void InitializeAdditionalLevelData()
    {
        var levelInitializer = To.gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
        //var multipleRRTSolvers = To.gameObject.GetComponentInChildren<MultipleRRTRunner>();
        Helpers.LogExecutionTime(levelInitializer.Init, "Level Initializer Time");
        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
    }

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

    protected GameObject SpawnObstacle(ref int geneIndex, BoxCollider2D box, GameObject Obstacles)
    {
        //Get Obstacle Variant
        int prefabIndex = (int)(GetGeneValue(geneIndex) * LevelProperties.ObstaclePrefabs.Count + 1);
        GameObject ObstaclePrefabVariant = LevelProperties.ObstaclePrefabs[prefabIndex - 1];

        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, GetGeneValue(geneIndex + 1));
        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, GetGeneValue(geneIndex + 2));
        float rot = Mathf.Lerp(0, 360, GetGeneValue(geneIndex + 3));
        float scl = Mathf.Lerp(MinObjectScale, MaxObjectScale, GetGeneValue(geneIndex + 4));

        geneIndex += 5;

        var obs = Instantiate(ObstaclePrefabVariant,
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, rot),
            Obstacles.transform);
        ObstaclePrefabVariant.transform.localScale = new Vector3(scl, scl, 0);
        return obs;
    }

    protected GameObject SpawnGameObject(ref int geneIndex, BoxCollider2D box, GameObject Prefab)
    {
        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, GetGeneValue(geneIndex));
        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, GetGeneValue(geneIndex + 1));

        geneIndex += 2;
        var player = Instantiate(Prefab,
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, 0),
            To.transform);
        return player;
    }

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