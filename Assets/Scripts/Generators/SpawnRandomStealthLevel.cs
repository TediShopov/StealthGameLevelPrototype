using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

//Class used to proved utility functions for generating
//standartizes level structure
public class LevelGeneratorBase : MonoBehaviour, ILevelPhenotypeGenerator
{
    public LevelProperties LevelProperties;
    public System.Random LevelRandom;

    public virtual IStealthLevelPhenotype GeneratePhenotype(LevelChromosomeBase levelChromosome)
    {
        throw new NotImplementedException();
    }
}