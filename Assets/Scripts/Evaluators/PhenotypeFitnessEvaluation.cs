using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace StealthLevelEvaluation
{
    public abstract class PhenotypeFitnessEvaluation
    {
        private bool _evaluted;
        public string Name { get; }
        protected double _value;
        protected double _time;
        protected GameObject Phenotype;

        public PhenotypeFitnessEvaluation(GameObject phenotype, string name, double defValue)
        {
            Phenotype = phenotype;
            Name = name;
            _value = defValue;
            _evaluted = false;
        }

        public double Value
        {
            get
            {
                if (!_evaluted)
                {
                    _evaluted = true;
                    Profiler.BeginSample(Name);
                    _time = Helpers.TrackExecutionTime(() => _value = Evaluate());
                    Profiler.EndSample();
                }
                else
                {
                }
                return _value;
            }
        }

        public double Time => _time;

        //Accepts the phenotype of a generated level and assigns a fitness value
        public abstract float Evaluate();

        public override string ToString()
        {
            //Get the value getter so value is sure to be calculated
            return $"{Name}: {Value}, For: {_time} \n";
        }

        public virtual void OnSelected()
        { }
    }
}