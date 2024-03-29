using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace StealthLevelEvaluation
{
    public interface IMeasure<T>
    {
        public string Name { get; }
        public double Time { get; }
        public MeasureResult? Result { get; }

        public void DoMeasure(T data);
    }

    public struct MeasureResult
    {
        public string Name;
        public bool IsValidation;
        public Type Type;
        public string Value;
        public double Time;

        public override string ToString()
        {
            //Get the value getter so value is sure to be calculated
            return $"{Name}: {Value}, For: {Time} \n";
        }
    }

    public abstract class MeasureMono : MonoBehaviour, IMeasure<GameObject>
    {
        private bool _evaluted;

        //Validators are always run first.
        public bool IsValidator = false;

        //If a validator is terminating, the no other validation and/or evaluation should be carried out.
        [HideInInspector] public bool IsTerminating { get; protected set; }

        [HideInInspector] public string Name { get; protected set; }

        [SerializeField] protected string _value;
        protected double _time;
        public GameObject Phenotype;

        public bool RunNow = false;
        public bool RunOnStart = false;

        public void Start()
        {
            Phenotype = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
            if (Phenotype != null)
            {
                if (RunOnStart)
                {
                    Init(Phenotype);
                    Evaluate();
                }
            }
        }

        public void Update()
        {
            if (RunNow)
            {
                RunNow = false;
                Evaluate();
            }
        }

        public abstract void Init(GameObject phenotype);

        public virtual void Init(GameObject phenotype, string name)
        {
            Phenotype = phenotype;
            Name = name;
            _value = "";
            _evaluted = false;
        }

        //        public MeasureMono(GameObject phenotype, string name, double defValue)
        //        {
        public string Value
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

        public MeasureResult? Result { get; private set; }

        //Accepts the phenotype of a generated level and assigns a fitness value
        public abstract string Evaluate();

        public void DoMeasure(GameObject data)
        {
            Phenotype = data;
            var measurment = Evaluate();
            if (Result.HasValue == false)
                Result = new MeasureResult
                {
                    Name = Name,
                    Time = _time,
                    Value = measurment,
                    IsValidation = IsValidator,
                    Type = this.GetType()
                };
            else
            {
                MeasureResult m = Result.Value;
                m.Name = Name;
                m.Time = Time;
                m.IsValidation = IsValidator;
                m.Value = measurment;
                m.Type = this.GetType();
            }
        }
    }
}