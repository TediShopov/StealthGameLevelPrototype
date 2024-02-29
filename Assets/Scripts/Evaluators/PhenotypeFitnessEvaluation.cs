using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace StealthLevelEvaluation
{
    public abstract class PhenotypeFitnessEvaluation : MonoBehaviour
    {
        private bool _evaluted;

        //Validators are always run first.
        public bool IsValidator = false;

        //If a validator is terminating, the no other validation and/or evaluation should be carried out.
        public bool IsTerminating = false;

        public string Name;
        [SerializeField] protected double _value;
        protected double _time;
        public GameObject Phenotype;

        public bool RunNow = false;

        public void Update()
        {
            if (RunNow)
            {
                RunNow = false;
                Evaluate();
            }
        }

        public abstract void Init(GameObject phenotype);

        public virtual void Init(GameObject phenotype, string name, double defValue)
        {
            Phenotype = phenotype;
            Name = name;
            _value = defValue;
            _evaluted = false;
        }

        //        public PhenotypeFitnessEvaluation(GameObject phenotype, string name, double defValue)
        //        {
        //            //Phenotype = phenotype;
        //            Name = name;
        //            _value = defValue;
        //            _evaluted = false;
        //        }

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