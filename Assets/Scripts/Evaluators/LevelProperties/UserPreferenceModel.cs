using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IObserver<T>
{
    void Update(T sub);
}

// Declares a subject interface
public interface ISubject<T>
{
    void Attach(IObserver<T> observer);

    void Detach(IObserver<T> observer);

    void Notify(T subject);
}

public interface IPreferenceModel<T> : ISubject<IList<float>>
{
    IList<float> Weights { get; }

    //Serves as an update to the model. All measurables change their values
    public void AlterPreferences(
        IAestheticMeasurable<T> selected,
        IEnumerable<IAestheticMeasurable<T>> notSelected);
}

public interface IAestheticMeasurable<T>
{
    public float GetAestheticScore(IPreferenceModel<T> preferenceModel);

    PropertyMeasurements GetMeasurements();
}

[Serializable]
public class UserPreferenceModel : IPreferenceModel<LevelChromosomeBase>
{
    private List<IObserver<IList<float>>> Observers;
    [SerializeReference] private List<float> _weights;
    public float Step;

    public IList<float> Weights
    {
        get { return _weights; }
        set { _weights = (List<float>)value; }
    }

    public UserPreferenceModel(int preferencesCount)
    {
        this.Weights = GetDefault(preferencesCount);
    }

    public List<float> GetDefault(int measures)
    {
        var equalWeightProperties = new List<float>();
        for (int i = 0; i < measures; i++)
        {
            equalWeightProperties.Add(1.0f);
        }
        Normalize(equalWeightProperties);
        return equalWeightProperties;
    }

    private float NewMax(
        float oldMax,
        IAestheticMeasurable<LevelChromosomeBase> selected,
        IEnumerable<IAestheticMeasurable<LevelChromosomeBase>> notSelected
        )
    {
        float maxAS = oldMax;
        maxAS = Mathf.Max(selected.GetAestheticScore(this), maxAS);
        foreach (var item in notSelected)
        {
            maxAS = Mathf.Max(item.GetAestheticScore(this), maxAS);
        }
        return maxAS;
    }

    public void AlterPreferences(
        IAestheticMeasurable<LevelChromosomeBase> selected,
        IEnumerable<IAestheticMeasurable<LevelChromosomeBase>> notSelected)
    {
        if (notSelected.Count() <= 0) return;
        var avgUnselectedProps =
            PropertyMeasurements.Average(notSelected.Select(x => x.GetMeasurements()).ToList());

        float maxAS = 0;
        maxAS = NewMax(maxAS, selected, notSelected);
        float prevDistanceToBest =
            MathF.Abs(maxAS - selected.GetAestheticScore(this));
        float distanceToBestAestheticScore =
            MathF.Abs(maxAS - selected.GetAestheticScore(this));

        const int maxIterations = 100;
        for (int z = 0; z < maxIterations; z++)
        {
            //Apply a single step in all weights
            for (int i = 0; i < avgUnselectedProps.Count; i++)
            {
                var changeInWeight = Step *
                    (selected.GetMeasurements()[i] - avgUnselectedProps[i]);

                this.Weights[i] = this.Weights[i] + changeInWeight;
            }
            //Reeavalute the AESTHETIC SCORE
            //Send message to all observer
            //All aesthetic measurables must update their scores
            Normalize(this.Weights);

            maxAS = NewMax(maxAS, selected, notSelected);
            //Update distance to best aesthetic score
            prevDistanceToBest = distanceToBestAestheticScore;
            distanceToBestAestheticScore =
                MathF.Abs(maxAS - selected.GetAestheticScore(this));

            if (distanceToBestAestheticScore < prevDistanceToBest)
            {
                //Preference model is no longer gaining progress
                break;
            }

            if (Mathf.Approximately(distanceToBestAestheticScore, 0))
            {
                //If selected item has the highest aesthetic score
                break;
            }
        }

        Normalize(this.Weights);
        Notify(this.Weights);
    }

    public void SetToDefault()
    {
        this.Weights = GetDefault(Weights.Count);
    }

    public void Normalize(IList<float> weights)
    {
        float sum = weights.Sum(x => Mathf.Abs(x));
        for (int i = 0; i < weights.Count; i++)
        {
            weights[i] = weights[i] / sum;
        }
    }

    public void Attach(IObserver<IList<float>> observer)
    {
        if (Observers == null)
            Observers = new List<IObserver<IList<float>>>();
        if (Observers.Contains(observer))
            return;
        Observers.Add(observer);
        observer.Update(new List<float>(this.Weights));
    }

    public void Detach(IObserver<IList<float>> observer)
    {
        if (Observers != null && Observers.Contains(observer))
            Observers.Remove(observer);
    }

    public void Notify(IList<float> subject)
    {
        foreach (var item in Observers)
        {
            item.Update(new List<float>(this.Weights));
        }
    }
}