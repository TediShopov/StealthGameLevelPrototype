using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class PolygonBoundary : PolygonObstacle
{

    private void Start()
    {
        IsExpandingOutWards = false;
        PolygonCollider = GetComponent<PolygonCollider2D>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Boundary Hit {collision.contacts[0]}");
    }
    
}
