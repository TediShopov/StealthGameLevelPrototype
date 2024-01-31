using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomLevelGridInitializer : MonoBehaviour
{
    public int Rows = 5; // Number of rows in the grid
    public int Columns = 5; // Number of columns in the grid
    public Vector2 LevelSize = new Vector2(1.0f, 1.0f); // Size of each object
    public int Seed;
    public bool RandomizeSeed;
    public System.Random RandomSeedGenerator;
    public GameObject LevelSpawnerPrefab;


    void Start()
    {
        if(RandomizeSeed)
            RandomSeedGenerator = new System.Random(DateTime.Now.Second);
        else
            RandomSeedGenerator = new System.Random(Seed);
        SpawnGrid();
    }

    void SpawnGrid()
    {
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                Vector3 spawnPosition = new Vector3(col * LevelSize.y,  row * LevelSize.y,0f);
                //Ensure position paramters are even
                spawnPosition.x =Snapping.Snap(spawnPosition.x, 2);
                spawnPosition.y =Snapping.Snap(spawnPosition.y, 2);
                SpawnLevelRandomizer(row,col,spawnPosition);

            }
        }
    }
    GameObject SpawnLevelRandomizer(int row, int col,Vector3 spawnPosition) 
    {
        int seed= RandomSeedGenerator.Next();
        //var level = new GameObject($"L_1_1_{seed}", typeof(SpawnRandomStealthLevel));
        var level = Instantiate(LevelSpawnerPrefab, this.transform.position, Quaternion.identity, this.transform);
        level.name = $"L_{row}_{col}_{seed}";
        level.transform.localPosition = spawnPosition;
        level.transform.SetParent(this.transform,true);
        var spawner =level.GetComponent<SpawnRandomStealthLevel>();
        spawner.RandomSeed = seed;
        spawner.MinDimension = LevelSize.x;
        spawner.MaxDimension = LevelSize.y;
        return level;
    }
}
