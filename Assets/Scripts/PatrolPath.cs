using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolPath : MonoBehaviour
{
    public List<Transform> Transforms = new List<Transform>();
    private int _wayPointIndex;
    public Transform NextWP => Transforms.ElementAtOrDefault(_wayPointIndex+1);
    public Transform CurrentWP => Transforms.ElementAtOrDefault(_wayPointIndex);
    [SerializeField] public Vector2 Velocity;
    [SerializeField] public float Speed;
    private Rigidbody2D _rigidBody2D;
    public float ReachRadius;
    public float DebugRadius;
    // Start is called before the first frame update
    void Start()
    {
        _rigidBody2D = this.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public Vector3 SeekNextWaypoint() 
    {
        return (NextWP.position - this.transform.position).normalized;
    }
    public bool ReachedNextWayPoint() 
    {
        if (NextWP != null)
            return Vector3.Distance(NextWP.position, this.transform.position) < ReachRadius;
        else
            return false;
    }
    void FixedUpdate()
    {
        if (ReachedNextWayPoint()) 
        {
            _wayPointIndex++;
            if(_wayPointIndex+1 >= Transforms.Count) 
            {
                Transforms.Reverse();
                _wayPointIndex = 0; 
            }
        } 
        //Store user input as a movement vector
        Velocity = SeekNextWaypoint();
        if (!Helpers.CompareVectors(Velocity, new Vector3(0, 0, 0), 0.01f))
        {
            _rigidBody2D.MovePosition(_rigidBody2D.position + Velocity* Speed * Time.fixedDeltaTime);
        }
        else
        {
            Velocity = Vector3.zero;
        }
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (Transform t in Transforms)
        {
            if (t.Equals(CurrentWP))
                Gizmos.color = Color.blue;
            else if (t.Equals(NextWP))
                Gizmos.color = Color.red;
            else 
                Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(t.position, DebugRadius);
        }

    }
}
