using GeneticSharp.Domain.Chromosomes;
using Mono.Cecil;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

//GENOTYPE desciprtion:
public class LevelPhenotypeGenerator : LevelGeneratorBase
{
    public bool RunOnStart = false;
    public bool IsRandom = false;
    public int RandomChromosomeSeed;
    public bool DisposeNow = false;
    private LevelChromosomeBase LevelChromosome;
    private GameObject To;

    public void Awake()
    {
        if (RunOnStart)
        {
            if (IsRandom)
                RandomChromosomeSeed = new System.Random().Next();
            LevelChromosome = new LevelChromosome(this, new System.Random(RandomChromosomeSeed));
            Generate(LevelChromosome, this.gameObject);
        }
    }

    public void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        To = to;
        LevelChromosome = chromosome;
        int geneIndex = 0;
        //Boundary
        To.tag = "Level";
        //Boundary constructed first 2 genes
        BoxCollider2D box = InitLevelBoundary(LevelProperties.LevelSize.x, LevelProperties.LevelSize.y, to);

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(To.transform, false);
        PlaceBoundaryVisualPrefabs(box, Obstacles);

        box.size = new Vector2(
            box.size.x - PlayerPrefab.GetComponent<Collider2D>().bounds.extents.x / 2.0f - VisualBoundWidth / 2.0f,
            box.size.y - PlayerPrefab.GetComponent<Collider2D>().bounds.extents.y / 2.0f - VisualBoundWidth / 2.0f
            );

        //Player
        //var playerInstance = SpawnGameObject(ref geneIndex, box, PlayerPrefab);
        var playerInstance = SpawnGameObjectAtRelative(LevelProperties.RelativeStartPosition, box, PlayerPrefab);
        //Destination
        //var destinationIntance = SpawnGameObject(ref geneIndex, box, DestinationPrefab);
        var destinationIntance = SpawnGameObjectAtRelative(LevelProperties.RelativeEndPosiiton, box, DestinationPrefab);

        //obstacles

        //Test for off by oen errors
        for (int i = 0; i < ObstaclesSpawned; i++)
        {
            SpawnObstacle(ref geneIndex, box, Obstacles);
        }

        //Read enemy counts and spawn enemies
        int enemyCount = Mathf.CeilToInt(Mathf.Lerp(MinEnemiesSpawned, MaxEnemiesSpawned, geneIndex));
        for (int i = 0; i < enemyCount; i++)
        {
            Instantiate(EnemyPrefab, To.transform);
        }

        Physics2D.SyncTransforms();
        //Solvers
        Instantiate(LevelInitializer, To.transform);
        var levelInitializer = To.gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
        var multipleRRTSolvers = To.gameObject.GetComponentInChildren<MultipleRRTRunner>();
        var pathGenerator = To.gameObject.GetComponentInChildren<PathGeneratorClass>();

        //Enemy path geenerator seed
        int pathSeed = Mathf.CeilToInt(
            GetGeneValue(geneIndex)
            * GetGeneValue(geneIndex + 1)
            * GetGeneValue(geneIndex + 2)
            * GetGeneValue(geneIndex + 3));
        geneIndex += 4;

        pathGenerator.LevelRandom = new System.Random(pathSeed);
        Helpers.LogExecutionTime(levelInitializer.Init, "Level Initializer Time");
        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
        Helpers.LogExecutionTime(multipleRRTSolvers.Run, "Multiple RRT Runs Time");
        Debug.Log("Generation of phenotype finished");

        Debug.Log($"Genotype length is:{LevelChromosome.Length}, Read genes are: {geneIndex}");
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

    private GameObject SpawnObstacle(ref int geneIndex, BoxCollider2D box, GameObject Obstacles)
    {
        //Get Obstacle Variant
        int prefabIndex = (int)(GetGeneValue(geneIndex) * ObstaclePrefabs.Count + 1);
        GameObject ObstaclePrefabVariant = ObstaclePrefabs[prefabIndex - 1];

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

    private GameObject SpawnGameObject(ref int geneIndex, BoxCollider2D box, GameObject Prefab)
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

    private GameObject SpawnGameObjectAtRelative(Vector2 coord, BoxCollider2D box, GameObject Prefab)
    {
        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, coord.x);
        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, coord.y);
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