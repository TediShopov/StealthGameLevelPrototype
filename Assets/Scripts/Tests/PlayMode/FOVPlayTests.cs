using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class FOVPlayTests
{
    //    [SerializeField] public DefaultEnemyProperties EnemyProperties;
    //
    //    // A Test behaves as an ordinary method
    //    [Test]
    //    public void FOVPlayTestsSimplePasses()
    //    {
    //        // Use the Assert class to test conditions
    //    }
    //
    //    [UnityTest]
    //    public IEnumerator FOVDistanceRange_CorrectValues()
    //    {
    //        EnemyProperties = new DefaultEnemyProperties();
    //        EnemyProperties.Speed = 1.0f;
    //        EnemyProperties.ViewDistance = 1.0f;
    //        EnemyProperties.FOV = 30.0f;
    //        var enemyPath = new List<Vector2>() { new Vector2(2, 0), new Vector2(0, 0) };
    //        var enemy = new GameObject("Enemy", new System.Type[] { typeof(PatrolPath) });
    //
    //        var enemyPatrolPath = enemy.GetComponent<PatrolPath>();
    //        enemyPatrolPath.EnemyProperties = EnemyProperties;
    //        enemyPatrolPath.SetPatrolPath(enemyPath);
    //
    //        Vector2 testPosition = new Vector2(0, 0);
    //
    //        yield return new WaitForSeconds(1.0f);
    //
    //        bool visibilityAfterSecond = FieldOfView.TestCollision(
    //            testPosition,
    //            PatrolPath.GetPathOrientedTransform(enemyPatrolPath.BacktrackPatrolPath),
    //            EnemyProperties.FOV,
    //            EnemyProperties.ViewDistance,
    //            LayerMask.GetMask("Obstacles"));
    //
    //        Assert.IsFalse(visibilityAfterSecond);
    //        yield return new WaitForSeconds(1.0f);
    //        bool visibilityAfterTwoSecond = FieldOfView.TestCollision(
    //            testPosition,
    //            PatrolPath.GetPathOrientedTransform(enemyPatrolPath.BacktrackPatrolPath),
    //            EnemyProperties.FOV,
    //            EnemyProperties.ViewDistance,
    //            LayerMask.GetMask("Obstacles"));
    //        Assert.IsTrue(visibilityAfterTwoSecond);
    //        yield return null;
    //    }
}