using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;

public enum MeasurementType
{
    INITIALIZATION,
    VALIDATION,
    DIFFICULTY,
    PROPERTIES,
    OVERALLFITNESS,
}

namespace StealthLevelEvaluation
{
    public interface IMeasure<T>
    {
        public string GetName();

        public MeasurementType GetCategory();

        public double Time { get; }
        public MeasureResult Result { get; }

        public void DoMeasure(T data);
    }

    [Serializable]
    public class MeasureResult
    {
        public string Name;

        // Method to get the depth of the MeasureResult
        public int GetDepth()
        {
            if (ChildMeasures == null || ChildMeasures.Count == 0)
            {
                return 1; // Leaf node
            }

            int maxDepth = 1;
            foreach (var child in ChildMeasures)
            {
                int childDepth = child.GetDepth();
                if (childDepth + 1 > maxDepth)
                {
                    maxDepth = childDepth + 1;
                }
            }
            return maxDepth;
        }

        public string GetFullName()
        {
            if (IsComposite)
            {
                string allNamesCommaSeparated = Name;
                if (ChildMeasures is null) return allNamesCommaSeparated;
                for (int i = 0; i < ChildMeasures.Count; i++)
                {
                    allNamesCommaSeparated +=
                        $",{ChildMeasures[i].GetFullName()}({i})";
                }
                return allNamesCommaSeparated;
            }
            else
                return Name;
        }

        public MeasurementType Category;
        public Type Type;
        public string Value;
        public double Time;
        public static string DefaultValue = "-";
        public bool IsComposite => ChildMeasures != null && ChildMeasures.Count > 0;
        public List<MeasureResult> ChildMeasures;

        public MeasureResult Parent;

        // Method to add a child measure and set the parent
        public void AddChildMeasure(MeasureResult res)
        {
            if (ChildMeasures == null)
            {
                ChildMeasures = new List<MeasureResult>();
            }
            res.Parent = this; // Set the parent to this instance
            ChildMeasures.Add(res);
        }

        // DFS method that accepts a C# Action
        public void DepthFirstSearch(Action<MeasureResult> action)
        {
            action(this); // Apply the action to the current MeasureResult
            if (ChildMeasures != null)
            {
                foreach (var child in ChildMeasures)
                {
                    child.DepthFirstSearch(action); // Recurse through child measures
                }
            }
        }

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

        //[HideInInspector] public string Name { get; protected set; }
        public abstract string GetName();

        [SerializeField] protected string _value;
        protected double _time;
        public GameObject Phenotype;

        public bool RunNow = false;
        public bool RunOnStart = false;
        public bool DrawOnSelected = false;

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

        //        public virtual void Init(GameObject phenotype, string name)
        //        {
        //            Phenotype = phenotype;
        //            Name = name;
        //            _value = "";
        //            _evaluted = false;
        //        }

        //        public MeasureMono(GameObject phenotype, string name, double defValue)
        //        {
        public string Value
        {
            get
            {
                if (!_evaluted)
                {
                    _evaluted = true;
                    Profiler.BeginSample(GetName());
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

        private MeasureResult _result;

        public MeasureResult Result
        {
            get
            {
                if (_result == null)
                {
                    _result = new MeasureResult()
                    {
                        Name = GetName(),
                        Value = "-",
                        Time = -1,
                        Category = GetCategory(),
                        Type = this.GetType(),
                    };
                }
                return _result;
            }
            set { _result = value; }
        }

        //public MeasureResult? Result { get; private set; }

        //Accepts the phenotype of a generated level and assigns a fitness value
        protected abstract string Evaluate();

        //        public void Measure()
        //        {
        //            _evaluted = true;
        //            Profiler.BeginSample(Name);
        //            _time = Helpers.TrackExecutionTime(() => _value = Evaluate());
        //            Profiler.EndSample();
        //
        //
        //        }

        public void DoMeasure(GameObject data)
        {
            Phenotype = data;
            var measurment = this.Value;
            if (_result == null)
                _result = new MeasureResult();
            _result.Name = GetName();
            _result.Time = Time;
            _result.Category = GetCategory();
            _result.Value = measurment;
            _result.Type = this.GetType();
        }

        public virtual MeasurementType GetCategory()
        {
            return MeasurementType.INITIALIZATION;
        }
    }
}