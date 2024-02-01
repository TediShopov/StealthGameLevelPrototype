using GeneticSharp.Domain.Chromosomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelChromosome : ChromosomeBase
{

    public LevelChromosome(int length):base(length)
    {
        
    }
    public override IChromosome CreateNew()
    {
        throw new System.NotImplementedException();
    }

    public override Gene GenerateGene(int geneIndex)
    {
        throw new System.NotImplementedException();
    }
}
//GENOTYPE desciprtion:
public class LevelPhenotypeGenerator : LevelGeneratorBase
{
    // [lenght%] [width%] :Bounds
    // [x%] [y%] :Player
    // [x%] [y%] :Destination
    // < many obstacles in form [type rounded down] [x%] [y%] [rotation] [scale]> :Obstacles
    // [%of possible enemies count rounded down] <4 float number to generate random path seed> :Enemis
    private FloatingPointChromosome LevelChromosome;

    public int RandomSeed;
    public bool isRandom;

    // Start is called before the first frame update
    private void Start()
    {
       // LevelRandom = new System.Random(RandomSeed);
       // int length = 6 + 1 + 4 + ObstaclesSpawned * 5;
       // LevelChromosome = new FloatingPointChromosome(0, 1, length,8);
       // Debug.Log($"Level chromosome length: {LevelChromosome.Length}");
       // LevelChromosome.ReplaceGenes(0, GetRandomGenes(length));
       // ManifestPhenotype();
    }
    public void Generate(FloatingPointChromosome chromosome)
    {
        LevelChromosome = chromosome;
        ManifestPhenotype();
    }
    public void Dispose() 
    {
        for (int i = 0; i < this.transform.childCount; i++)
        {
            Destroy(this.transform.GetChild(i).gameObject);
        }
    }

    public float GetGeneValue(int index) => (float)LevelChromosome.GetGene(index).Value;

    public void ManifestPhenotype()
    {
        int geneIndex = 0;
        //Boundary
        this.tag = "Level";
        //Boundary constructed first 2 genes
        BoxCollider2D box = InitLevelBoundary(
            Mathf.Lerp(MinDimension, MaxDimension, GetGeneValue(geneIndex + 0)),
            Mathf.Lerp(MinDimension, MaxDimension, GetGeneValue(geneIndex + 1))
            );
        PlaceBoundaryVisualPrefabs(box);
        geneIndex += 2;

        //Player
        var playerInstance = SpawnGameObject(ref geneIndex, box, PlayerPrefab);
        //Destination
        var destinationIntance = SpawnGameObject(ref geneIndex, box, DestinationPrefab);

        //obstacles
        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(this.transform);
        Obstacles.transform.localPosition = new Vector3(0, 0, 0);
        CompositeVisualBoundary.transform.SetParent(Obstacles.transform, false);

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

        //Solvers
        Instantiate(LevelInitializer, this.transform);
        var levelInitializer = gameObject.GetComponentInChildren<InitializeStealthLevel>();
        var voxelizedLevel = gameObject.GetComponentInChildren<VoxelizedLevel>();
        var multipleRRTSolvers = gameObject.GetComponentInChildren<MultipleRRTRunner>();
        levelInitializer.Init();
        voxelizedLevel.Init();
        multipleRRTSolvers.Run();
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
    }
}