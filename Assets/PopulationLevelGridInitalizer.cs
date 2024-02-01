using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulationLevelGridInitalizer : MonoBehaviour
{
    public int Rows = 5; // Number of rows in the grid
    public int Columns = 5; // Number of columns in the grid
    public int Seed;
    public bool RandomizeSeed;

    public Vector2 LevelSize = new Vector2(1.0f, 1.0f); // Size of each object
    public System.Random RandomSeedGenerator;

    //public SpawnRandomStealthLevel LevelSpawnerPrefab;
    // Start is called before the first frame update
}