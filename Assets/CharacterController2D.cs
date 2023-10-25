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
    public static bool CompareFloats(float a, float b, float bias)
    {
        return Mathf.Abs(a - b) < bias;
    }

    public static bool CompareVectors(Vector3 a, Vector3 b, float bias)
    {
        return CompareFloats(a.x, b.x, bias) && CompareFloats(a.y, b.y, bias) && CompareFloats(a.z, b.z, bias);
    }
    public static Vector3 VectorForceManipulation( Vector3 movement,Vector3 input,float topSpeed, float acceleration, float friction) 
    {
        Vector3 modifiedComponent = movement;
        modifiedComponent -=  movement.normalized * friction * Time.deltaTime;
        modifiedComponent +=  input * acceleration * Time.deltaTime;
        modifiedComponent = modifiedComponent.normalized * Mathf.Clamp(modifiedComponent.magnitude, 0, topSpeed); 
        return modifiedComponent;
    }
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
        Velocity = VectorForceManipulation(Velocity, m_Input, MaxSpeed, Acceleration, Friction);
        if (!CompareVectors(Velocity, new Vector3(0, 0, 0), 0.01f))
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
