using CGALDotNetGeometry.Shapes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CGALDotNetGeometry.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class RRTWDelaunay : DiscreteDistanceBasedRRTSolver
{
    //Trinagles that reside in free space - achieved from delaunay trinagulation
    private Triangle2d[] freeSpaceTriangles;

    //Cached summed area of all trinagles
    private float totalArea;

    private System.Random Random;

    public RRTWDelaunay(IFutureLevel discretizedLevel, float bias, float goalDist, float maxvel)
        : base(discretizedLevel, bias, goalDist, maxvel)
    {
        Random = new System.Random();
    }

    public void SetTrianglesInFreeSpace(Triangle2d[] tris)
    {
        freeSpaceTriangles = tris;
        //Assign total area
        totalArea = (float)freeSpaceTriangles.Sum(x => x.Area);
    }

    public override Vector3 GetRandomState()
    {
        if (freeSpaceTriangles == null)
            return base.GetRandomState();
        else
        {
            Vector2 point =
                GetRandomPointInTriangle(GetRandomTriangle());
            Vector3 pointInTime = new Vector3(
                    point.x,
                    point.y,
                    GetRandomReachableTime(point));
            return pointInTime;
        }
    }

    /// <summary>
    /// Gets a random triangle from the triangle array.
    /// The chance of each triangle to get pick is based on its
    /// area.
    /// </summary>
    /// <returns></returns>
    public Triangle2d GetRandomTriangle()
    {
        float randomValue = Helpers.GetRandomFloat(Random, 0, totalArea);

        // Pick a random triangle based on weighted area
        float currentArea = 0.0f;
        int selectedIndex = 0;
        while (currentArea < randomValue && selectedIndex < freeSpaceTriangles.Length + 1)
        {
            currentArea += (float)freeSpaceTriangles[selectedIndex].Area;
            selectedIndex++;
        }
        return freeSpaceTriangles[selectedIndex - 1];
    }

    public Vector2 GetRandomPointInTriangle(Triangle2d triangle)
    {
        if (triangle == null) return Vector2.zero;
        Vector2 a = new Vector2((float)triangle.A.x, (float)(triangle.A.y));
        Vector2 b = new Vector2((float)triangle.B.x, (float)(triangle.B.y));
        Vector2 c = new Vector2((float)triangle.C.x, (float)(triangle.C.y));
        // Calculate vectors for two edges of the triangle
        Vector2 edge1 = b - a;
        Vector2 edge2 = c - a;

        // Generate random weights for the two edges (u, v)
        float u = Helpers.GetRandomFloat(Random, 0, 1);
        float v = Helpers.GetRandomFloat(Random, 0, 1);
        // Ensure u + v <= 1 to stay within triangle area
        if (u + v > 1.0f)
        {
            u = 1.0f - u;
            v = 1.0f - v;
        }
        // Calculate the final point using barycentric coordinates
        Vector2 randomPoint = a + (u * edge1) + (v * edge2);
        return randomPoint;
    }
}