using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolPath : MonoBehaviour
{
    public List<Transform> Transforms = new List<Transform>();
    public List<Vector2> Positions = new List<Vector2>();
    private int _wayPointIndex;
    public Vector2 NextWP => Positions.ElementAtOrDefault(_wayPointIndex+1);
    public Vector2 CurrentWP => Positions.ElementAtOrDefault(_wayPointIndex);
    [SerializeField] public Vector2 Velocity;
    [SerializeField] public float Speed;
    public    FieldOfView FieldOfView;
    private Rigidbody2D _rigidBody2D;
    public float ReachRadius;
    public float DebugRadius;

    private Vector2 CurrentPosition => new Vector2(this.transform.position.x, this.transform.position.y); 
    // Start is called before the first frame update
    void Start()
    {

        _rigidBody2D = this.GetComponent<Rigidbody2D>();
        if(Positions.Count > 0 && _rigidBody2D != null) 
        {
            _rigidBody2D.position = Positions.First();
        }
//        Positions = Transforms.Select(t => new Vector2(t.position.x, t.position.y)).ToList();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetInitialPositionToPath() 
    {
        if(Positions.Count > 0 && _rigidBody2D != null) 
        {
            _rigidBody2D.position = Positions.First();
        }
    }
    public Vector2 SeekNextWaypoint() 
    {
        return (NextWP - CurrentPosition).normalized;
    }
    public bool ReachedNextWayPoint() 
    {
        if (NextWP != null)
            return Vector3.Distance(NextWP, CurrentPosition) < ReachRadius;
        else
            return false;
    }
    void FixedUpdate()
    {
        if (ReachedNextWayPoint()) 
        {
            _wayPointIndex++;
            if(_wayPointIndex+1 >= Positions.Count) 
            {
                Positions.Reverse();
                _wayPointIndex = 0; 
            }
        } 
        //Store user input as a movement vector
        Velocity = SeekNextWaypoint();
        LookAtPosition(Velocity);

        if (!Helpers.CompareVectors(Velocity, new Vector3(0, 0, 0), 0.01f))
        {
            _rigidBody2D.MovePosition(_rigidBody2D.position + Velocity* Speed * Time.fixedDeltaTime);
        }
        else
        {
            Velocity = Vector3.zero;
        }
    }
    public void LookAtPosition(Vector3 lookAt) 
    {
        // the second argument, upwards, defaults to Vector3.up
        Quaternion rotation = Quaternion.Euler(0,0,Helpers.GetAngleFromVectorFloat(lookAt));
        transform.rotation = rotation;
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (Vector2 t in Positions)
        {
            if (t.Equals(CurrentWP))
                Gizmos.color = Color.blue;
            else if (t.Equals(NextWP))
                Gizmos.color = Color.red;
            else 
                Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(t, DebugRadius);
        }

    }
}
