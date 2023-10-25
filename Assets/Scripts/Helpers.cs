using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers : MonoBehaviour
{
    public static bool CompareFloats(float a, float b, float bias)
    {
        return Mathf.Abs(a - b) < bias;
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
}
