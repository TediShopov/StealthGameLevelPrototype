using GeneticSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NativeRandomProvider : IRandomization
{
    private System.Random _random;

    public NativeRandomProvider()
    {
        _random = new System.Random();
    }

    public NativeRandomProvider(int seed)
    {
        _random = new System.Random(seed);
    }

    public double GetDouble()
    {
        return _random.NextDouble();
    }

    public double GetDouble(double min, double max)
    {
        // Get a random double between min and max
        return min + _random.NextDouble() * (max - min);
    }

    public float GetFloat()
    {
        // Convert a random double to float
        return (float)_random.NextDouble();
    }

    public float GetFloat(float min, float max)
    {
        // Get a random float between min and max
        return min + (float)(_random.NextDouble() * (max - min));
    }

    public int GetInt(int min, int max)
    {
        // Return a random integer between min (inclusive) and max (exclusive)
        return _random.Next(min, max);
    }

    public int[] GetInts(int length, int min, int max)
    {
        int[] result = new int[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = GetInt(min, max);
        }
        return result;
    }

    public int[] GetUniqueInts(int length, int min, int max)
    {
        // Ensure we have enough range to get unique integers
        if (max - min < length)
        {
            throw new ArgumentException("Range is too small for the requested number of unique integers.");
        }

        HashSet<int> uniqueInts = new HashSet<int>();
        while (uniqueInts.Count < length)
        {
            uniqueInts.Add(GetInt(min, max));
        }
        return uniqueInts.ToArray();
    }
}