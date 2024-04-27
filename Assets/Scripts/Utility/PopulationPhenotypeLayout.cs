using GeneticSharp;
using System;
using System.Collections;
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
    public ILevelManifestor LevelManifestor;

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
        Debug.Log("Created New Through Mono Pop");
        GridLayout.PrepareForNewGeneration();

        foreach (var chromosome in chromosomes)
        {
            LevelChromosomeBase levelChromosome = (LevelChromosomeBase)chromosome;
            levelChromosome.Phenotype = GridLayout.GetNextLevelObject();
            levelChromosome.ActualLevelPhenotype =
                levelChromosome.PhenotypeGenerator.GeneratePhenotype(levelChromosome);
            this.LevelManifestor.Manifest(levelChromosome, levelChromosome.Phenotype);
        }

        base.CreateNewGeneration(chromosomes);
    }
}

public class GridObjectLayout
{
    public Vector2 ExtraSpacing = Vector2.zero;

    public GridObjectLayout(Vector2 spacing)
    {
        ExtraSpacing = spacing;
    }

    private GameObject[,] LevelObjects;
    private int currentIndex = -1;
    private int GridDimension;

    public GameObject GetNextLevelObject()
    {
        currentIndex++;
        if (currentIndex >= GridDimension * GridDimension)
            return null;
        return LevelObjects[currentIndex / GridDimension, currentIndex % GridDimension];
    }

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
            if (item == null) continue;
            var tempList = item.transform.Cast<Transform>().ToList();
            foreach (var child in tempList)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
            item.name = ResetPhenotypeGameObjectName(item);
        }
    }
}