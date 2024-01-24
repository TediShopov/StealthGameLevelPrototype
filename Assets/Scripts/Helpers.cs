using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class Helpers : MonoBehaviour
{
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

    public static void SaveToCSV(string filePath, string s)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath))
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
    public static Vector3 VectorForceManipulation( Vector3 movement,Vector3 input,float topSpeed, float acceleration, float friction) 
    {
        Vector3 modifiedComponent = movement;
        modifiedComponent -=  movement.normalized * friction * Time.deltaTime;
        modifiedComponent +=  input * acceleration * Time.deltaTime;
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

}
