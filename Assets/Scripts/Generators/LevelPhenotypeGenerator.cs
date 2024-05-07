using Codice.Client.BaseCommands;
using GeneticSharp;
using JetBrains.Annotations;
using Mono.Cecil;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

//GENOTYPE desciprtion:

public abstract class LevelPhenotypeGenerator : LevelGeneratorBase
{
    public bool RunOnStart = false;
    public bool IsRandom = false;

    //public System.Random RandomSeed;
    public bool DisposeNow = false;

    [SerializeReference, SubclassPicker] public CrossoverBase Crossover;
    [SerializeReference, SubclassPicker] public MutationBase Mutation;

    //    public int MinEnemiesSpawned = 1;
    //    public int MaxEnemiesSpawned = 3;
    //    public int StartingObstacleCount = 3;

    protected LevelChromosomeBase LevelChromosome;
    [HideInInspector] public int IndexOfChromosome;

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

    public abstract LevelChromosomeBase GetAdamChromosome(int seed);

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

    protected void AttachChromosome(LevelChromosomeBase chromosome)
    {
        LevelChromosome = chromosome;
        LevelChromosomeMono chromosomeMono = Data.AddComponent<LevelChromosomeMono>();
        chromosomeMono.Chromosome = chromosome;
    }

    public void ClearName(LevelChromosomeBase chromosome)
    {
        if (chromosome == null || chromosome.Manifestation == null)
            return;

        GameObject levelGameObject = chromosome.Manifestation;
        levelGameObject.name = "";
    }

    public void AppendToName(LevelChromosomeBase chromosome, string text)
    {
        if (chromosome == null || chromosome.Manifestation == null)
            return;

        GameObject levelGameObject = chromosome.Manifestation;
        levelGameObject.name += text;
    }

    public void AppendFitnessToName(LevelChromosomeBase chromosome)
    {
        if (chromosome == null || chromosome.Manifestation == null)
            return;
        if (chromosome.Fitness.HasValue)
        {
            chromosome.Manifestation.name += $"Fitness: {chromosome.Fitness}";
        }
    }

    protected virtual int GenerateLevelContent(LevelChromosomeBase chromosome)
    {
        throw new NotImplementedException();
    }

    protected void InitializeAdditionalLevelData()
    {
        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
    }

    //!WARNING! uses destroy immediate as mulitple level can be geenrated an
    //disposed in the same frame
    public void DisposeOldPopulation()
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
        int prefabIndex = (int)(GetGeneValue(geneIndex) * LevelProperties.ObstaclePrefabs.Count) + 1;
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
            DisposeOldPopulation();
            DisposeNow = false;
        }
    }
}