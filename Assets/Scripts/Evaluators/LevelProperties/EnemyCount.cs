using JetBrains.Annotations;
using StealthLevelEvaluation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class EnemyCount : LevelPropertiesEvaluator
{
    public Vector2 EnemySize;
    public LevelProperties LevelProperties;
    public float ExpectedPercentageOfEnemyOccupiedSpace = 0.2f;
    public int MaxEnemies = 0;

    public override void Init(GameObject phenotype)
    {
        base.Init(phenotype);
    }

    protected override float MeasureProperty()
    {
        var enemyCounts =
            Manifestation.GetComponentsInChildren<PatrolEnemyMono>()
            .Where(x => x is not null)
            .Count();
        MaxEnemies = CalculateUpperBoundOfEnemyRange();
        return Mathf.InverseLerp(0, MaxEnemies, enemyCounts);
    }

    public int CalculateUpperBoundOfEnemyRange()
    {
        int maxEnemyCountH = MaxEnemyCountHeuristic();
        //A heursistic of destribution of the level is
        //40% occupied space which leaves 60% possible space to be occupied by enemies
        return Mathf.FloorToInt((float)maxEnemyCountH * ExpectedPercentageOfEnemyOccupiedSpace);
    }

    public int MaxEnemyCountHeuristic()
    {
        int fitEnemyOnXAxis =
            Mathf.FloorToInt(LevelProperties.LevelSize.x / EnemySize.x);
        int fitEnemyOnYAxis =
            Mathf.FloorToInt(LevelProperties.LevelSize.y / EnemySize.y);
        return fitEnemyOnXAxis * fitEnemyOnYAxis;
    }
}