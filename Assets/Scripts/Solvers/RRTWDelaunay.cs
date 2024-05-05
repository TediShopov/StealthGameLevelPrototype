using CGALDotNetGeometry.Shapes;
using System.Linq;
using UnityEngine;

/// <summary>
/// Uses delanay triangulation to sample only geometry in free space.
/// The random sampling consists of two steps: picking a random triangle from
/// the triangulation (probability weighted by normalized area) followed by picking
/// a random point in the selected triangle.
/// Has the same imporvement as RRTBiased
/// </summary>
public class RRTWDelaunay : RRTBiased
{
    private float _totalArea; // Total area of all triangles
    private System.Random _random;
    private Triangle2d[] _freeSpaceTriangles; //Triangles residing in free space

    public RRTWDelaunay()
    {
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
        float u = Helpers.GetRandomFloat(_random, 0, 1);
        float v = Helpers.GetRandomFloat(_random, 0, 1);
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
    public override Vector3 GetRandomState()
    {
        if (_freeSpaceTriangles == null)
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
        float randomValue = Helpers.GetRandomFloat(_random, 0, _totalArea);

        // Pick a random triangle based on weighted area
        float currentArea = 0.0f;
        int selectedIndex = 0;
        while (currentArea < randomValue && selectedIndex < _freeSpaceTriangles.Length + 1)
        {
            currentArea += (float)_freeSpaceTriangles[selectedIndex].Area;
            selectedIndex++;
        }
        return _freeSpaceTriangles[selectedIndex - 1];
    }
    public void SetTrianglesInFreeSpace(Triangle2d[] tris)
    {
        _freeSpaceTriangles = tris;
        //Assign total area
        _totalArea = (float)_freeSpaceTriangles.Sum(x => x.Area);
    }
}