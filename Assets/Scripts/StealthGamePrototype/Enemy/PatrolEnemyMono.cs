using Codice.Client.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(Rigidbody2D))]
[ExecuteAlways]
public class PatrolEnemyMono : MonoBehaviour, IPredictableThreat
{
    private Patrol Patrol;
    public DefaultEnemyProperties EnemyProperties;
    public List<Vector2> PathPoints;

    public FieldOfView FieldOfView;
    private Rigidbody2D _rigidBody2D;

    //Flag if enemy mono will sync the transform and the rigidbody to
    //the ones from the patrol in the simulation
    public bool IsPaused = false;

    //Checkbox used as button to assign new patrol
    //from path points in the editor
    public bool AssignManualPath;

    //Checkbox used as button to assign enemy as static
    public bool SetStaticTransform;

    public float Time => Patrol.Time;

    public Patrol GetPatrol()
    { return Patrol; }

    // Start is called before the first frame update
    private void Awake()
    {
        _rigidBody2D = this.GetComponent<Rigidbody2D>();
        InitPatrol(PathPoints);
        //        if (BacktrackPatrolPath == null)
        //            if (Transforms.Count > 2)
        //                SetPatrolPath(Transforms.Select(x => (Vector2)x.position).ToList());
    }

    public IPredictableThreat Copy()
    {
        return new Patrol(this.Patrol);
    }

    //Assume that manual changes to patrol points are made
    // and use them to construct new patrol class
    public void AssignPathPointsFromEditor()
    {
        Patrol = new Patrol(EnemyProperties, PathPoints);
    }

    public void InitPatrol(List<Vector2> points)
    {
        PathPoints = points;
        Patrol = new Patrol(EnemyProperties, points);
    }

    public void Update()
    {
        if (AssignManualPath)
        {
            AssignPathPointsFromEditor();
            AssignManualPath = false;
        }
        if (SetStaticTransform)
        {
            FutureTransform InitialTransformFromEditor;
            InitialTransformFromEditor.Position = _rigidBody2D.position;
            InitialTransformFromEditor.Direction =
                Helpers.GetVectorFromAngle(this.transform.rotation.z);

            Patrol = new Patrol(EnemyProperties, InitialTransformFromEditor);
            SetStaticTransform = false;
        }
    }

    private void FixedUpdate()
    {
    }

    //    public void DrawAllSegmentes()
    //    {
    //        if (BacktrackPatrolPath == null) return;
    //        var pathCount = BacktrackPatrolPath.GetPath().Count;
    //        for (int i = 0; i <= pathCount - 1; i++)
    //        {
    //            var seg = BacktrackPatrolPath.GetSegment(i);
    //            Gizmos.DrawLine(seg.Item1, seg.Item2);
    //        }
    //    }

    //    public void OnDrawGizmosSelected()
    //    {
    //        Gizmos.color = Color.yellow;
    //        DrawAllSegmentes();
    //    }

    //    public float GetTimeToTraverse()
    //    {
    //        if (BacktrackPatrolPath is not null)
    //        {
    //            float length = BacktrackPatrolPath.GetTotalLength();
    //            return length * EnemyProperties.Speed;
    //        }
    //        return 0;

    //    }

    //Given a patrol path return the position and the direction facing the direction
    // of the current traversed segment

    public void TimeMove(float deltaTime)
    {
        if (IsPaused) return;
        Patrol.TimeMove(deltaTime);
        _rigidBody2D.position =
            GetTransform().Position;
        this.transform.position = _rigidBody2D.position;
        transform.rotation =
            Quaternion.Euler(0, 0, Helpers.GetAngleFromVectorFloat(GetTransform().Direction));
    }

    public FutureTransform GetTransform()
    {
        if (Patrol == null) return new FutureTransform
        { Position = this.transform.position, Direction = Vector2.right };
        return Patrol.GetTransform();
    }

    public bool TestThreat(Vector2 collision)
    {
        if (Patrol == null) return false;
        return Patrol.TestThreat(collision);
    }

    public Bounds GetBounds()
    {
        if (Patrol == null) return new Bounds();
        return Patrol.GetBounds();
    }

    public void Reset()
    {
        Patrol?.Reset();
    }
}