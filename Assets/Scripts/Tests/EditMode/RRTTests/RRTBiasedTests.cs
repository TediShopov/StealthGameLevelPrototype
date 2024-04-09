using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class RRTBiasedTests : MonoBehaviour
{
    [UnityTest]
    public void RRTInGoalRadius_BiasesStep()
    {
        //        var flObject = new GameObject("FL");
        //        IFutureLevel futureLevel = flObject.AddComponent<ContinuosFutureLevel>();
        //
        //        RRTBiased rrtB = new RRTBiased(futureLevel, 2.0f, 0.01f, 1.0f);
        //
        //        Vector3 start = Vector3.zero;
        //        Vector3 end = new Vector3(5, 5, 5);
        //        rrtB.Setup(new Vector3(0, 0, 0), end, 0);
        //
        //        Vector3 pointInBiasDistacne = (start - end).normalized * rrtB.BiasDistance;
        //        Assert.IsTrue(rrtB.IsInBiasDistance(start, pointInBiasDistacne));
    }
}