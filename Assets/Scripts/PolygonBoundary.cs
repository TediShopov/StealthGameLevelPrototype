using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class PolygonBoundary : MonoBehaviour
{
    private void Start()
    {
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Boundary Hit {collision.contacts[0]}");
    }
}