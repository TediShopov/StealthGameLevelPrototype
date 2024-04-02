using GeneticSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveEvalutation : MonoBehaviour
{
    private StealthLevelIEMono IEMono;
    private IChromosome Chromosome;
    public float FitnessValue = 0;
    public float NewFitnessValue = 0;
    public bool DoEval = false;

    private void Start()
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
    }

    // Update is called once per frame
    private void Update()
    {
        if (Chromosome.Fitness.HasValue)
        {
            FitnessValue = (float)Chromosome.Fitness.Value;
        }
        if (DoEval)
        {
            if (Chromosome is not null)
            {
                if (Chromosome.Fitness.HasValue)
                {
                    Chromosome.Fitness = NewFitnessValue;
                    FitnessValue = NewFitnessValue;
                }
            }
            DoEval = !DoEval;
        }
    }
}