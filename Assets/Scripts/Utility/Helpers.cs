using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Helpers : MonoBehaviour
{
    public static T SafeGetComponentInChildren<T>(GameObject from) where T : MonoBehaviour
    {
        var components = from.GetComponentsInChildren<T>();
        return components.FirstOrDefault(x => x.isActiveAndEnabled == true);
    }

    public static GameObject SearchForTagUpHierarchy(GameObject startFrom, string tag)
    {
        // The root object the stealth level
        GameObject level = startFrom;
        while (level != null)
        {
            if (level.CompareTag(tag))
                break;
            if (level.transform.parent != null)
                level = level.transform.parent.gameObject;
            else
                return null;
        }

        return level;
    }

    public static float GetRandomFloat(System.Random random, float minValue, float maxValue)
    {
        // Generate a random float within a custom range
        double range = (double)maxValue - minValue;
        return (float)(random.NextDouble() * range + minValue);
    }

    public static bool CompareFloats(float a, float b, float bias)
    {
        return Mathf.Abs(a - b) < bias;
    }

    public static float LogExecutionTime(System.Action function, string funName)
    {
        var ms = TrackExecutionTime(function);
        // Log the execution time
        UnityEngine.Debug.Log($"{funName} executed in {ms} milliseconds");
        return ms;
    }

    public static bool IsColidingCell(Vector3 worldPosition, Vector2 size, LayerMask obstacle)
    {
        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        Vector2 halfBoxSize = size * 0.5f;

        // Perform a BoxCast to check for obstacles in the area
        RaycastHit2D hit = Physics2D.BoxCast(
            origin: position2D,
            size: halfBoxSize,
            angle: 0f,
            direction: Vector2.zero,
            distance: 0.01f,
            layerMask: obstacle
        );

        return hit.collider != null;
    }

    public static float CalculateStandardDeviation(IEnumerable<float> values)
    {
        float mean = values.Average();
        float varience =
            values.Sum(x => Mathf.Pow(x - mean, 2)) / (values.Count() - 1);
        return Mathf.Sqrt(varience); // Standard deviation
    }

    public static float CalculateRelativeVariance(IEnumerable<float> values)
    {
        float mean = values.Average();
        float standardDeviation = CalculateStandardDeviation(values);
        if (mean == 0)
        {
            throw new ArgumentException("The mean cannot be zero when calculating relative variance.");
        }
        float coefficientOfVariation = (standardDeviation / mean);
        return coefficientOfVariation;
    }

    public static Bounds GetLevelBounds(GameObject obj)
    {
        var _boundary = Physics2D.OverlapPoint(obj.transform.position, LayerMask.GetMask("Boundary"));
        if (_boundary != null)
        {
            return _boundary.gameObject.GetComponent<Collider2D>().bounds;
        }
        throw new NotImplementedException();
    }

    public static Collider2D GetLevelBoundaryCollider(GameObject obj)
    {
        var _boundary = Physics2D.OverlapPoint(obj.transform.position, LayerMask.GetMask("Boundary"));
        if (_boundary != null)
        {
            return _boundary.gameObject.GetComponent<Collider2D>();
        }
        throw new NotImplementedException();
    }

    public static float TrackExecutionTime(System.Action function)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        function.Invoke();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    public static float[] TrackExecutionTime(System.Action function, int iterations)
    {
        var results = new float[iterations];
        for (int i = 0; i < iterations; i++)
        {
            results[i] = TrackExecutionTime(function);
        }
        return results;
    }

    public static void SaveRunToCsv(string filepath, float[] results)
    {
        string s = "";
        foreach (var run in results)
        {
            s += run.ToString() + ",";
            s = s.Remove(s.Length - 1, 1);
            s += "\n";
        }
        Helpers.SaveToCSV(filepath, s);
    }

    public static void SaveToCSV(string filePath, string s)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                // Write CSV header
                writer.WriteLine(s);
            }

            UnityEngine.Debug.Log($"Performance data saved to {filePath}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log($"Error saving performance data: {ex.Message}");
        }
    }

    public static bool CompareVectors(Vector3 a, Vector3 b, float bias)
    {
        return CompareFloats(a.x, b.x, bias) && CompareFloats(a.y, b.y, bias) && CompareFloats(a.z, b.z, bias);
    }

    public static Vector3 VectorForceManipulation(Vector3 movement, Vector3 input, float topSpeed, float acceleration, float friction)
    {
        Vector3 modifiedComponent = movement;
        modifiedComponent -= movement.normalized * friction * Time.deltaTime;
        modifiedComponent += input * acceleration * Time.deltaTime;
        modifiedComponent = modifiedComponent.normalized * Mathf.Clamp(modifiedComponent.magnitude, 0, topSpeed);
        return modifiedComponent;
    }

    public static Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180.0f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }

    public static float GetAngleFromVectorFloat(Vector3 dir)
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (n < 0)
        {
            n += 360;
        }
        return n;
    }

    public static Bounds IntersectBounds(Bounds a, Bounds b)
    {
        Vector3 min = Vector3.Max(a.min, b.min);
        Vector3 max = Vector3.Min(a.max, b.max);
        var overlapp = new Bounds(min, new Vector3(0, 0, 0));
        overlapp.SetMinMax(min, max);
        return overlapp;
    }
}