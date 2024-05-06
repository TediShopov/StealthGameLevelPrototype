using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class PropertyMeasurements : List<float>
{
    public PropertyMeasurements(int count)
        : base(new float[count])
    {
    }

    public PropertyMeasurements(IList<float> measurements)
        : base(measurements)
    {
        if (IsInValidMeasureRange(measurements) == false)
        {
            throw new ArgumentException("Invalid Measurements");
        }
    }

    public static PropertyMeasurements Average(
        IList<PropertyMeasurements> measurementInstances)
    {
        if (measurementInstances == null
            || measurementInstances.Count() <= 0)
        {
            return null;
        }

        PropertyMeasurements averageMeasurement =
            new PropertyMeasurements(
                measurementInstances.First().Count);

        foreach (var propertyMeasuremnts in measurementInstances)
        {
            try
            {
                averageMeasurement += propertyMeasuremnts;
            }
            catch (Exception)
            {
            }
        }
        averageMeasurement.DividceEach(measurementInstances.Count);
        return averageMeasurement;
    }

    public static PropertyMeasurements Average(
      PropertyMeasurements one, PropertyMeasurements two)
    {
        PropertyMeasurements averageMeasurement =
            new PropertyMeasurements(one.Count);
        averageMeasurement += one;
        averageMeasurement += two;
        averageMeasurement.DividceEach(2);
        return averageMeasurement;
    }

    public static PropertyMeasurements operator +(
PropertyMeasurements one
, PropertyMeasurements two)
    {
        if (one.Count != two.Count)
            throw new System.ArgumentException();
        else
        {
            PropertyMeasurements levelPropertyMeasurements =
                new PropertyMeasurements(one.Count);

            for (int i = 0; i < one.Count; i++)
            {
                levelPropertyMeasurements[i]
                    = one[i] + two[i];
            }
            return levelPropertyMeasurements;
        }
    }

    public float AveragePropertyDistance(
        PropertyMeasurements other
        )
    {
        float avgDitance = 0;
        for (int i = 0; i < other.Count; i++)
        {
            avgDitance += PropertyDistance(other, i);
        }
        avgDitance /= (float)this.Count;
        return avgDitance;
    }
    public void DividceEach(float a)
    {
        for (int i = 0; i < this.Count; i++)
        {
            this[i] /= a;
        }
    }
    //The raw signed change occuring on a property
    public float PropertyChange(PropertyMeasurements other, int i)
    {
        return other[i] - this[i];
    }
    //Distance is always an ABSOLUTE value
    public float PropertyDistance(PropertyMeasurements other, int i)
    {
        return MathF.Abs(other[i] - this[i]);
    }
    /// <summary>
    /// Checks if measurements are all in the range of
    /// 0 ... 1
    /// </summary>
    /// <param name=""></param>
    private static bool IsInValidMeasureRange(IList<float> measures)
    {
        foreach (var measure in measures)
        {
            if (measure < 0 || measure > 1)
                return false;
        }
        return true;
    }
}