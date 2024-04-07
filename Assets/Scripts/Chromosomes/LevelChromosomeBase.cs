using GeneticSharp;
using PlasticPipe.PlasticProtocol.Messages;
using System.Collections.Generic;
using System.Security.Claims;

//Level chromosome base holds a reference to the phenotype/level generator
// as to allow easy change and iteration via mono scirpts in the unity editor.

public abstract class LevelChromosomeBase : ChromosomeBase
{
    protected LevelChromosomeBase(int length, LevelPhenotypeGenerator generator) : base(length)
    {
        PhenotypeGenerator = generator;
    }

    public LevelPhenotypeGenerator PhenotypeGenerator;
    public MeasurementsData Measurements { get; set; }
    public List<float> AestheticProperties;

    public override IChromosome Clone()
    {
        //var clone =new LevelChromosome(Length, PhenotypeGenerator);
        var clone = (LevelChromosomeBase)base.Clone();
        clone.Measurements = Measurements;
        clone.AestheticProperties = AestheticProperties;
        return clone;
    }
}