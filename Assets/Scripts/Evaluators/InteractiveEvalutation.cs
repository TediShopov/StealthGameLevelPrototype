using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveEvalutation : MonoBehaviour
{
    private StealthLevelIEMono IEMono;
    private LevelChromosome Chromosome;

    public void SelectLevel()
    {
        //Find level object
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");

        //Get the level chromosome object and change get ref to
        var chromosomeMono = level?.GetComponentInChildren<LevelChromosomeMono>();
        if (chromosomeMono == null)
            throw new System.ArgumentException("No level chromose has been found");
        // its contents
        Chromosome = chromosomeMono.Chromosome;
        if (Chromosome == null)
            throw new System.ArgumentException("No acutal chromose contntes");

        IEMono = this.GetComponentInParent<StealthLevelIEMono>();
        if (IEMono == null)
            throw new System.ArgumentException("No interactive evolution found");

        Debug.Log($"Selected level {level.gameObject.name}");
        IEMono.SelectChromosome(Chromosome);
    }

    // Update is called once per frame
}