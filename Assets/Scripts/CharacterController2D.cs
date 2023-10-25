using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;

using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    [SerializeField] public Vector2 Velocity;
    [SerializeField] public float MaxSpeed;
    [SerializeField] public float Acceleration;
    [SerializeField] public float Friction;

    private Rigidbody2D _rigidBody2D;
    // Start is called before the first frame update
    void Start()
    {
       this._rigidBody2D = this.GetComponent<Rigidbody2D>(); 
    }
    

    // Update is called once per frame
    void Update()
    {

    }
    void FixedUpdate()
    {
        //Store user input as a movement vector
        Vector3 m_Input = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"),0);
        Velocity = Helpers.VectorForceManipulation(Velocity, m_Input, MaxSpeed, Acceleration, Friction);
        if (!Helpers.CompareVectors(Velocity, new Vector3(0, 0, 0), 0.01f))
        {
            Vector3 translation = new Vector3(Velocity.x, Velocity.y) * Time.deltaTime;
            _rigidBody2D.velocity = Velocity;
        }
        else
        {
            Velocity = Vector3.zero;
        }
    }
}
