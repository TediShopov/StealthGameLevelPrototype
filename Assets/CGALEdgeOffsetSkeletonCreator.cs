//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor.SearchService;
//using UnityEngine;
//
//struct CustomCollision
//{
//    public Vector2 point;
//    public int haseCode;
//    public int hashCodeTwo;
//}
//public class CGALEdgeOffsetSkeletonCreator : MonoBehaviour
//{
//    List<PolygonObstacle> PolygonObstacles = new List<PolygonObstacle>();
//    public double offsetIteration = 0.5f;
//    public int offsetsDone = 0;
//    public int InstantIterationOffsetCount = 0;
//    // Start is called before the first frame update
//    void Start()
//    {
//         PolygonObstacles =FindObjectsOfType<PolygonObstacle>().ToList();
//    }
//    List<CustomCollision> DetectedCollisions= new System.Collections.Generic.List<CustomCollision>();
//
//    // Update is called once per frame
//    void Update()
//    {
//
//        if(Input.GetKeyDown(KeyCode.I))
//        {
//            offsetsDone--;
//            OffsetAllShapes(-offsetIteration);
//            Debug.Log($"Offsets Done {offsetsDone}");
//        }
//        if(Input.GetKeyDown(KeyCode.O))
//        {
//            offsetsDone++;
//            Debug.Log($"Offsets Done {offsetsDone}");
//            OffsetAllShapes(offsetIteration);
//            CheckCollisions();
//        }
//        if (Input.GetKeyDown(KeyCode.M))
//        {
//            for (int i = 0; i < InstantIterationOffsetCount; i++)
//            {
//                OffsetAllShapes(offsetIteration);
//                CheckCollisions();
//            }
//            for (int i = 0; i < InstantIterationOffsetCount; i++)
//            {
//                OffsetAllShapes(-offsetIteration);
//            }
//
//
//        }
//    }
//    public void OffsetAllShapes(double offset)
//    {
//            foreach (var obstacle in PolygonObstacles)
//            {
//                obstacle.OffsetPoints(offset);
//            }
//    }
//    public void CheckCollisions()
//    {
//            foreach (var obstacle in PolygonObstacles)
//            {
//                foreach (var otherObstaclej in PolygonObstacles)
//                {
//
//                    if (CollisionCanHappen(obstacle,otherObstaclej))
//                    {
//                        Vector2 point = Vector2.zero;
//                        if (obstacle.CheckIntersection(otherObstaclej, ref point))
//                        {
//                            Debug.Log($"Collision Detected between {obstacle.name} and {otherObstaclej.name}");
//                            Debug.Log($"Collision tagged as {obstacle.GetHashCode()} and {otherObstaclej.GetHashCode()}");
//                            AddCollision(point, obstacle,otherObstaclej);
//                        }
//                    }
//                }
//            }
//    }
//    private void OnDrawGizmos()
//    {
//        Gizmos.color = Color.red;
//        foreach (var customCollision in DetectedCollisions)
//        {
//            Gizmos.DrawSphere(customCollision.point, 0.2f);
//        }
//    }
//    public void AddCollision(Vector2 point,PolygonObstacle obstacle, PolygonObstacle other)
//    {
//        DetectedCollisions.Add(new CustomCollision { point = point, haseCode = obstacle.GetHashCode(), hashCodeTwo = other.GetHashCode() });
//    }
//    public bool CollisionCanHappen(PolygonObstacle obstacle, PolygonObstacle other)
//    {
//        if (obstacle == other) return false;
//        int hashOne = obstacle.GetHashCode();
//        int hashTwo = other.GetHashCode();
//        return !DetectedCollisions.Any(x => (x.haseCode == hashOne && x.hashCodeTwo == hashTwo) || (x.haseCode == hashTwo && x.hashCodeTwo == hashOne));
//    }
//}