using Codice.CM.SEIDInfo;
using PlasticPipe.Certificates;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyPathStrategyLG : LevelPhenotypeGenerator
{
    public const int ObstaccleGeneLength = 5;
    public const int EnemyGeneLength = 5;
    public int NecessaryObstacles = 3;
    public int EnemyCount;
    private int EntityCount => (LevelChromosome.Length - 1) / 5;

    public int GetObstaclesToSpawn(ref int geneIndex)
    {
        float obstacleToEnemyRatio = GetGeneValue(geneIndex);
        geneIndex++;
        //Both obstacle and enemies need 5 genes to represent an entity

        //        //Converting 0,1 range to a range [3, chromosomeLength -1]
        //        //Ensures at least 3 obstacle for a level and at least one chromose for enemy
        //        float convertedObstacleToEnemyRatio =
        //            Mathf.Lerp(NecessaryObstacles, EntityCount - 1, obstacleToEnemyRatio);

        //        int enemyWeight = Mathf.FloorToInt(Mathf.Lerp(1, 5, obstacleToEnemyRatio));
        //        int totalWeight = 1 + enemyWeight;
        //
        //        float e = (float)enemyWeight / 6.0f;
        //        int enemyCount = Mathf.CeilToInt(e * EntityCount);
        //        int obstacleCount = EntityCount - enemyCount;

        int obstacleCount = Mathf.CeilToInt(
        Mathf.Lerp(NecessaryObstacles, EntityCount - 1, obstacleToEnemyRatio));

        return obstacleCount;
    }

    public override void Generate(LevelChromosomeBase chromosome, GameObject to = null)
    {
        To = to;
        LevelChromosome = chromosome;
        //Boundary
        To.tag = "Level";

        var data = new GameObject("Data");
        data.transform.SetParent(To.transform);
        LevelChromosomeMono chromosomeMono = data.AddComponent<LevelChromosomeMono>();
        chromosomeMono.Chromosome = (LevelChromosome)chromosome;

        BoxCollider2D box =
            SetupLevelInitials(chromosome, to,
            new GameObject("VisBound"));
        Instantiate(LevelInitializer, To.transform);
        int geneIndex = GenerateLevelContent(chromosome, box);

        var visualBoundary = new GameObject("VisualBoundary");
        visualBoundary.transform.SetParent(To.transform, false);
        PlaceBoundaryVisualPrefabs(box, visualBoundary);

        //Solvers
        var levelInitializer = To.gameObject.GetComponentInChildren<InitializeStealthLevel>();
        //var voxelizedLevel = gameObject.GetComponentInChildren<>();
        var voxelizedLevel = To.gameObject.GetComponentInChildren<IFutureLevel>();
        //var multipleRRTSolvers = To.gameObject.GetComponentInChildren<MultipleRRTRunner>();
        Helpers.LogExecutionTime(voxelizedLevel.Init, "Future Level Logic Time");
        AssignPaths(geneIndex);

        Debug.Log("Generation of phenotype finished");
    }

    public void AssignPaths(int geneIndex)
    {
        LevelRandom = new System.Random();
        var pathGenerator =
            To.GetComponentInChildren<DiscretePathGenerator>();
        pathGenerator.geneIndex = geneIndex;
        pathGenerator.Init();
        FloodfilledRoadmapGenerator floodfilledRoadmapGenerator = To.GetComponentInChildren<FloodfilledRoadmapGenerator>();
        floodfilledRoadmapGenerator.Init();
        floodfilledRoadmapGenerator.FloodRegions();
        pathGenerator.Roadmap = floodfilledRoadmapGenerator.RoadMap;
        pathGenerator.LevelRandom = LevelRandom;
        PatrolEnemyMono[] enemyPaths = To.GetComponentsInChildren<PatrolEnemyMono>();
        List<List<Vector2>> paths = pathGenerator.GeneratePaths(EnemyCount);
        for (int i = 0; i < EnemyCount; i++)
        {
            enemyPaths[i].InitPatrol(paths[i]);
        }
        geneIndex = pathGenerator.geneIndex;
    }

    protected override int GenerateLevelContent(LevelChromosomeBase chromosome, BoxCollider2D box)
    {
        int geneIndex = 0;
        int ObstacleCount = GetObstaclesToSpawn(ref geneIndex);
        EnemyCount = EntityCount - ObstacleCount;
        var Obstacles = new GameObject("Obstacles");
        Obstacles.transform.SetParent(To.transform);

        //Test for off by oen errors
        for (int i = 0; i < ObstacleCount; i++)
        {
            SpawnObstacle(ref geneIndex, box, Obstacles);
        }

        MergeObstacles(Obstacles);

        //Read enemy counts and spawn enemies

        for (int i = 0; i < EnemyCount; i++)
        {
            Instantiate(EnemyPrefab, To.transform);
        }

        //Enemy Behaviour

        Physics2D.SyncTransforms();
        return geneIndex;
    }

    public void MergeObstacles(GameObject Obstacles)
    {
        var colliders = Obstacles.GetComponentsInChildren<Collider2D>().ToList();
        ContactFilter2D filter = new ContactFilter2D
        {
            useDepth = false,
            useLayerMask = true,
            useTriggers = false,
            useOutsideDepth = false,
            layerMask = ObstacleLayerMask
        };
        for (int i = 0; i < colliders.Count; i++)
        {
            var overlapCollider = colliders[i];
            List<Collider2D> overlappingCollider = new List<Collider2D>();
            Physics2D.OverlapCollider(
                overlapCollider,
                filter,
                overlappingCollider
                );
            overlappingCollider.Add(overlapCollider);
            if (overlappingCollider.Count >= 2)
            {
                GameObject gm = new GameObject("Composite");
                gm.transform.SetParent(Obstacles.transform);
                var rb = gm.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var comp = gm.AddComponent<CompositeCollider2D>();
                gm.layer = LayerMask.NameToLayer("Obstacle");

                comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
                comp.generationType = CompositeCollider2D.GenerationType.Manual;
                colliders.RemoveAll(x => overlappingCollider.Contains(x));

                AddShapesToCompositeObject(overlappingCollider, comp);
                Physics2D.SyncTransforms();
                colliders.Add(comp);
                Physics2D.SyncTransforms();
            }
        }
    }

    private static void AddShapesToCompositeObject
        (List<Collider2D> overlappingCollider, CompositeCollider2D comp)
    {
        foreach (var c in overlappingCollider)
        {
            if (c is not CompositeCollider2D)
                SimpleShapeToCompositeCollider(c, comp);
            else
            {
                var otherComposite = (CompositeCollider2D)c;
                var shapesFromOtherComposite = otherComposite.gameObject.GetComponentsInChildren<Collider2D>()
                    .Where(x => x.usedByComposite == true).
                    ToList();
                foreach (var shape in shapesFromOtherComposite)
                {
                    SimpleShapeToCompositeCollider(shape, comp);
                }
            }
        }
        Physics2D.SyncTransforms();
        comp.GenerateGeometry();
    }

    private static void SimpleShapeToCompositeCollider(Collider2D c, CompositeCollider2D comp)
    {
        if (c is not CompositeCollider2D)
        {
            //A simple shape
            //Remove rigidbody as only final composite collider needs to have it
            var rigidbody = c.gameObject.GetComponent<Rigidbody2D>();
            if (rigidbody)
                DestroyImmediate(rigidbody);

            //Have the shape be used from the composite collider
            var collider = c.gameObject.GetComponent<Collider2D>();
            collider.usedByComposite = true;

            //Set the parent shape
            c.gameObject.transform.SetParent(comp.gameObject.transform);
        }
        else
        {
            throw new System.ArgumentException("Cannot nest composite colliders");
        }
    }

    public void MergeCollidersIntoACompositeCollider(CompositeCollider2D toBeComposite, List<Collider2D> colliderToMerge)
    {
        if (colliderToMerge.Count <= 1) return;
        //        Collider2D toBeComposite =
        //            colliderToMerge.OrderBy(x => x is CompositeCollider2D).First();
        //        CompositeCollider2D toBeComposite = new GameObject("Composite", new System.Type[] { typeof(CompositeCollider2D) })
        //            .GetComponent<CompositeCollider2D>();
        //        toBeComposite.transform.SetParent(To.transform);
        //        EditorUtility.SetDirty(toBeComposite.gameObject);
        //
        List<Collider2D> rest = colliderToMerge.Where(x => x != toBeComposite).ToList();

        foreach (var r in rest)
        {
            var rb = r.GetComponent<Rigidbody2D>();
            if (rb)
                DestroyImmediate(rb);
            r.usedByComposite = true;
            //Change the parent to be the parent with the RB2D but do not change position
            r.gameObject.transform.SetParent(toBeComposite.transform, true);
            EditorUtility.SetDirty(r.gameObject);
        }
        EditorUtility.SetDirty(To.gameObject);
    }

    public void SpawnObstacleAndMerge(ref int geneIndex, BoxCollider2D box, GameObject Obstacles)
    {
        var obst = SpawnObstacle(ref geneIndex, box, Obstacles);
        Physics2D.SyncTransforms();
        ContactFilter2D filter = new ContactFilter2D
        {
            useDepth = false,
            useLayerMask = true,
            useTriggers = false,
            useOutsideDepth = false,
            layerMask = ObstacleLayerMask
        };

        List<Collider2D> overlappingCollider = new List<Collider2D>();
        Physics2D.OverlapCollider(
            obst.GetComponent<Collider2D>(),
            filter,
            overlappingCollider
            );
        //overlappingCollider = overlappingCollider.Where(x => x.gameObject.transform.parent == Obstacles).ToList();

        if (overlappingCollider.Count >= 2)
        {
            GameObject gm = new GameObject("Composite");
            gm.transform.SetParent(To.transform);
            var rb = gm.AddComponent<Rigidbody2D>();
            var comp = gm.AddComponent<CompositeCollider2D>();
            EditorUtility.SetDirty(gm);

            //MergeCollidersIntoACompositeCollider(comp, overlappingCollider);
        }

        return;
    }
}