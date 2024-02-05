using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class FloodfillTests
{
    private FloodfilledRoadmapGenerator Floodfill;

    // A Test behaves as an ordinary method
    [Test]
    public void FloodfillTestsSimplePasses()
    {
        // Use the Assert class to test conditions
    }

    [Test]
    public void FloodRegions_SequentialCalls_NotAffectedByEachOther()
    {
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator FloodfillTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}