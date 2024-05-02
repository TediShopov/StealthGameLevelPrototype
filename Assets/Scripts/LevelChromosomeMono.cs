using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelChromosomeMono : MonoBehaviour
{
    [SerializeReference] public LevelChromosomeBase Chromosome;

    public static LevelChromosomeMono Find(GameObject gameObject)
    {
        var level = Helpers.SearchForTagUpHierarchy(gameObject, "Level");
        if (level == null)
            return null;
        return level.GetComponentInChildren<LevelChromosomeMono>();
    }

    public LevelPhenotype GetPhenotype()
    {
        return this.Chromosome.Phenotype;
    }
}