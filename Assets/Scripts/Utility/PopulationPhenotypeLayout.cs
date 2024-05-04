using GeneticSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Interface of how population phenotype is layed out in Unity editor
//

[Serializable]
public class PopulationPhenotypeLayout : Population
{
    public int Min = 10;
    public int Max = 20;
    public Vector2 ExtraSpacing;
    public GridObjectLayout GridLayout = null;
    public GameObject IEGameObject;

    public PopulationPhenotypeLayout()
        : base(10, 20, null)
    {
        CreationDate = DateTime.Now;
    }

    public PopulationPhenotypeLayout(PopulationPhenotypeLayout other, GameObject gameObj, IChromosome adam)
        : base(other.Min, other.Max, adam)
    {
        this.Min = other.Min;
        this.Max = other.Max;
        CreationDate = DateTime.Now;
        this.ExtraSpacing = other.ExtraSpacing;
        this.IEGameObject = gameObj;
    }

    public override void CreateInitialGeneration()
    {
        GridLayout = new GridObjectLayout(ExtraSpacing);
        GridLayout.SpawnGrid(Max, IEGameObject.transform);

        Debug.Log("Created Initial Through Mono Pop");
        base.CreateInitialGeneration();
    }

    public override void CreateNewGeneration(IList<IChromosome> chromosomes)
    {
        List<IChromosome> chromosomesToKeep = new List<IChromosome>();
        List<GameObject> manifestationsToKeep = new List<GameObject>();
        if (this.CurrentGeneration != null)
        {
            var groupLeaders = this.CurrentGeneration.Chromosomes.GroupBy(x => x)
                .Select(x => x.First(x => ((LevelChromosomeBase)x).Manifestation != null));

            //Interesect with all new chromosome to find chromosome
            //which are in both population anb should be kept
            chromosomesToKeep = groupLeaders.Intersect(chromosomes).ToList();

            //Assign manifestation to similiar indiviudals across populations
            //This only happnes if somehow an exact same replica of individual
            //is produced without cloning it. (Common in one-way crossovers)
            foreach (var chromosomeToKeep in chromosomesToKeep)
            {
                var newPopToKeep = chromosomes.First(x => x.Equals(chromosomeToKeep));
                ((LevelChromosomeBase)newPopToKeep).Manifestation =
                    ((LevelChromosomeBase)chromosomeToKeep).Manifestation;
            }

            //Transform to their gameobejcts
            manifestationsToKeep =
               chromosomesToKeep.Select(x => ((LevelChromosomeBase)x).Manifestation)
               .ToList();
        }

        var chromosomeGroups = chromosomes.GroupBy(x => x).ToList();
        GridLayout.currentIndex = -1;
        foreach (var group in chromosomeGroups)
        {
            //Pick group leader
            var groupLeader = group.FirstOrDefault(x => ((LevelChromosomeBase)x).Manifestation);
            if (groupLeader == null)
                groupLeader = group.First();

            LevelChromosomeBase levelChromosome = (LevelChromosomeBase)groupLeader;

            if (chromosomesToKeep.Contains(levelChromosome))
            {
                //Skip the generation of the leader chromosome if it
                //persists into next generation
                if (manifestationsToKeep.Contains(levelChromosome.Manifestation))
                    continue;
            }

            GameObject nextFreeManifestation = null;
            //Pick until the manifstation does not need to persist in the
            // next civilization
            do
            {
                nextFreeManifestation = GridLayout.GetNextLevelObject();
            } while (manifestationsToKeep.Contains(nextFreeManifestation));

            //Clear the game object just in case
            GridLayout.RemoveObject(nextFreeManifestation);
            //Assign the free manifesation spot to the chromosome and generate
            levelChromosome.Manifestation = nextFreeManifestation;
            levelChromosome.
                PhenotypeGenerator
                .Generate(levelChromosome, levelChromosome.Manifestation);
        }

        while (GridLayout.currentIndex < GridLayout.GetMaxIndex)
        {
            var manifest = GridLayout.GetNextLevelObject();
            if (manifestationsToKeep.Contains(manifest))
            {
                continue;
            }
            GridLayout.RemoveObject(manifest);
        }

        Debug.Log($"Population Count Is: {chromosomeGroups.Count}");
        base.CreateNewGeneration(chromosomes);
    }
}

/// <summary>
/// Create a matrix of empty evenly spaced gameobjects
/// </summary>
public class GridObjectLayout
{
    public Vector2 ExtraSpacing = Vector2.zero;

    public GridObjectLayout(Vector2 spacing)
    {
        ExtraSpacing = spacing;
    }

    //Matrix of game object
    public GameObject[,] LevelObjects;

    //Index of the current object to retrieve
    public int currentIndex = -1;

    //The dimensions of the matrix.
    //One property as matrix is always squre
    private int GridDimension;

    public int GetMaxIndex => GridDimension * GridDimension - 1;

    public GameObject GetNextLevelObject()
    {
        currentIndex++;
        if (currentIndex >= GridDimension * GridDimension)
            return null;
        return LevelObjects[currentIndex / GridDimension, currentIndex % GridDimension];
    }

    //    public void ReorderObjectTransform(GameObject target,int index)
    //    {
    //        if (LevelObjects == null || target == null)
    //        {
    //            return null;
    //        }
    //

    //        // Loop through the LevelObjects to find the target GameObject
    //        for (int i = 0; i < LevelObjects.GetLength(0); i++)
    //        {
    //            for (int j = 0; j < LevelObjects.GetLength(1); j++)
    //            {
    //                if (LevelObjects[i, j] == target)
    //                {
    //                    return (i, j); // Return the indices if found
    //                }
    //            }
    //        }
    //
    //        return null; // Return null if not found

    //
    //    }

    public void SpawnGrid(int populationCount, Transform transform)
    {
        GridDimension = Mathf.CeilToInt(Mathf.Sqrt(populationCount));

        float step = 0.1f;
        //Setup Generator Prototype
        LevelObjects = new GameObject[GridDimension, GridDimension];

        for (int i = 0; i < GridDimension; i++)
        {
            for (int j = 0; j < GridDimension; j++)
            {
                Vector3 levelGridPosition =
                    new Vector3(
                        i * ExtraSpacing.x,
                        j * ExtraSpacing.y,
                        0);
                var g = new GameObject($"{i * GridDimension + j}");
                g.transform.position = levelGridPosition;
                g.transform.parent = transform;
                LevelObjects[i, j] = g;
            }
        }
    }

    public void PrepareForNewGeneration()
    {
        //Clearing old data
        DisposeOldPopulation();
        //Resetting index
        currentIndex = -1;
    }

    private string ResetPhenotypeGameObjectName(GameObject gameObject)
    {
        if (gameObject == null) return "";
        string gameobjectName = gameObject.name;
        int indexOfFirstWhiteSpace = gameobjectName.IndexOf(" ");
        if (indexOfFirstWhiteSpace == -1) return gameobjectName;
        return gameobjectName.Substring(0, indexOfFirstWhiteSpace);
    }

    //Once a new population has been started the gameobject generated must be cleared
    private void DisposeOldPopulation()
    {
        Debug.Log("Disposing generated artefacts of previous levels");
        foreach (var item in LevelObjects)
        {
            if (item == null) return;
            RemoveObject(item);
        }
    }

    public void RemoveNext()
    {
        int nextIndex = currentIndex + 1;
        RemoveObject(LevelObjects[nextIndex / GridDimension, nextIndex % GridDimension]);
    }

    public void RemoveObject(GameObject item)
    {
        if (item == null) return;
        var tempList = item.transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
        item.name = ResetPhenotypeGameObjectName(item);
    }
}