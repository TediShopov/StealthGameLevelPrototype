using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolPath : MonoBehaviour
{
    public List<Transform> Transforms = new List<Transform>();
    public List<Vector2> Positions = new List<Vector2>();
    public List<Vector2> InitialPositions = new List<Vector2>();
    private int _wayPointIndex;
    Graph<Vector2> RoadmapGraph;
    public Vector2 NextWP => Positions.ElementAtOrDefault(_wayPointIndex+1);
    public Vector2 CurrentWP => Positions.ElementAtOrDefault(_wayPointIndex);
    [SerializeField] public Vector2 Velocity;
    [SerializeField] public float Speed;
    public    FieldOfView FieldOfView;
    private Rigidbody2D _rigidBody2D;
    public float ReachRadius;
    public float DebugRadius;
    public float TimeSincePathStart = 1.0f;
    public bool DrawFuturePosition=false;
    public bool SeekPath=true;

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
        InitialPositions=new List<Vector2>(Positions);
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
        if (SeekPath == false) { return; }
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
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < Positions.Count - 1; i++) 
        {
            Vector2 t = Positions[i];
            if (t.Equals(CurrentWP))
                Gizmos.color = Color.blue;
            else if (t.Equals(NextWP))
                Gizmos.color = Color.red;
            else 
                Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(t, DebugRadius);
            //Draw path 
            Vector2 t1 = Positions[i+1];

            Gizmos.DrawLine(t1, t);

        }
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
                Gizmos.color = Color.blue;
        
        if(DrawFuturePosition) 
        {

            Gizmos.DrawSphere(CalculateFuturePosition(TimeSincePathStart).Item1, 0.2f);
            var flatPos = CalculateFuturePosition(TimeSincePathStart);
        }

    }
    public Tuple<Vector2,Vector2> CalculateFuturePosition(float time)
    {
        if(Positions == null || Positions.Count < 2) return new Tuple<Vector2, Vector2>(Vector2.zero,Vector2.zero);
        // Calculate the character's future position based on time and 
        float distanceCovered = Speed * time;


        // Interpolate the character's position along the path
        Vector3 newPosition = Vector3.zero;
        Vector2 newDirection = Vector2.zero;
        float distance = 0.0f;
        var waypoints = new List<Vector2>(InitialPositions);
        int i = 0;
        while (distance <= distanceCovered)
        {
            if (i >= waypoints.Count-1)
            {
                waypoints.Reverse();
                i = 0;
            }
            float segmentLength = Vector3.Distance(waypoints[i], waypoints[i + 1]);
            if (distance + segmentLength >= distanceCovered)
            {
                float t = (distanceCovered - distance) / segmentLength;
                newPosition = Vector3.Lerp(waypoints[i], waypoints[i + 1], t);
                newDirection= (waypoints[i+1] - waypoints[i]).normalized;
                break;
            }
            distance += segmentLength;

            i++;
        }



        newPosition.z = time;
        return new Tuple<Vector2, Vector2>(newPosition,newDirection);
    }
}
