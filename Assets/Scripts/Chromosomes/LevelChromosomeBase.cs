using GeneticSharp.Domain.Chromosomes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

public abstract class LevelChromosomeBase : ChromosomeBase
{
    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
    }

    public LevelPhenotypeGenerator PhenotypeGenerator;
    public FitnessInfo FitnessInfo { get; set; }
}