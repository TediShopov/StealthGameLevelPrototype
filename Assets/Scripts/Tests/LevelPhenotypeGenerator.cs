using GeneticSharp.Domain.Chromosomes;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class LevelChromosome : ChromosomeBase
{
    private System.Random ChromosomeRandom = new System.Random();
    public FitnessInfo FitnessInfo { get; set; }

    public Gene[] GetRandomGenes(int length)
    {
        Gene[] genes = new Gene[length];
        for (int i = 0; i < length; i++)
        {
            genes[i] = new Gene(Helpers.GetRandomFloat(ChromosomeRandom, 0f, 1f));
        }
        return genes;
    }

    public LevelChromosome(int length = 10, System.Random random = null) : base(length)
    {
        if (random == null)
        {
            ChromosomeRandom = new System.Random();
        }
        else
        {
            ChromosomeRandom = random;
        }
        this.ReplaceGenes(0, GetRandomGenes(length));
    }

    public override IChromosome CreateNew()
    {
        return new LevelChromosome(Length, ChromosomeRandom);
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(Helpers.GetRandomFloat(ChromosomeRandom, 0f, 1f));
    }
}

//GENOTYPE desciprtion:
public class LevelPhenotypeGenerator : LevelGeneratorBase
{
    // < many obstacles in form [type rounded down] [x%] [y%] [rotation] [scale]> :Obstacles
    // [%of possible enemies count rounded down] <4 float number to generate random path seed> :Enemis
    private LevelChromosome LevelChromosome;

    public int RandomSeed;
    public bool isRandom;
    public bool RunOnStart = false;
    public bool DisposeNow = false;

    //    public void Start()
    //    {
    //        if (RunOnStart)
    //        {
    //            if (isRandom)
    //            {
    //                RandomSeed = new System.Random().Next();
    //            }
    //            Generate(new LevelChromosome(35, new System.Random(RandomSeed)));
    //        }
    //    }

    // Start is called before the first frame update
    //    private void Awake()
    //    {
    //       // LevelRandom = new System.Random(RandomSeed);
    //       // int length = 6 + 1 + 4 + ObstaclesSpawned * 5;
    //       // LevelChromosome = new LevelChromosome(0, 1, length,8);
    //       // Debug.Log($"Level chromosome length: {LevelChromosome.Length}");
    //       // LevelChromosome.ReplaceGenes(0, GetRandomGenes(length));
    //       // ManifestPhenotype();
    //    }
    public void Generate(LevelChromosome chromosome)
    {
        LevelRandom = new System.Random(RandomSeed);
        LevelChromosome = chromosome;
        ManifestPhenotype();
        Debug.Log("Generation of phenotype finished");
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
        //for (int i = 0; i < this.transform.childCount; i++)
        //{
        //    Destroy(this.transform.GetChild(i).gameObject);
        //}
    }

    public float GetGeneValue(int index) => (float)LevelChromosome.GetGene(index).Value;

    public void ManifestPhenotype()
    {
        int geneIndex = 0;
        //Boundary
        this.tag = "Level";
        //Boundary constructed first 2 genes
        BoxCollider2D box = InitLevelBoundary(LevelProperties.LevelSize.x, LevelProperties.LevelSize.y);

        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(this.transform, false);
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
            Instantiate(EnemyPrefab, this.transform);
        }
        //for (int i = LevelChromosome.Length; )

        Physics2D.SyncTransforms();
        //Solvers
        Instantiate(LevelInitializer, this.transform);
        var levelInitializer = gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
        var voxelizedLevel = gameObject.GetComponentInChildren<IFutureLevel>();
        var multipleRRTSolvers = gameObject.GetComponentInChildren<MultipleRRTRunner>();
        var pathGenerator = gameObject.GetComponentInChildren<PathGeneratorClass>();
        pathGenerator.LevelRandom = LevelRandom;
        Helpers.LogExecutionTime(levelInitializer.Init, "Level Initializer Time");
        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
        Helpers.LogExecutionTime(multipleRRTSolvers.Run, "Multiple RRT Runs Time");
    }

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
            this.transform);
        return player;
    }

    private GameObject SpawnGameObjectAtRelative(Vector2 coord, BoxCollider2D box, GameObject Prefab)
    {
        float x = Mathf.Lerp(box.bounds.min.x, box.bounds.max.x, coord.x);
        float y = Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, coord.y);
        var player = Instantiate(Prefab,
            new Vector3(x, y, 0),
            Quaternion.Euler(0, 0, 0),
            this.transform);
        return player;
    }

    public Gene[] GetRandomGenes(int length)
    {
        Gene[] genes = new Gene[length];
        for (int i = 0; i < length; i++)
        {
            genes[i] = new Gene(Helpers.GetRandomFloat(LevelRandom, 0f, 1f));
        }
        return genes;
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