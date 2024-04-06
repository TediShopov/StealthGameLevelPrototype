using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "StealthLevel/EnemyProperties", order = 1)]
public class DefaultEnemyProperties : ScriptableObject
{
    //Controll AestheticProperties
    [Header("Enemy Controll Propertied")]
    [SerializeField] public float Speed;
    public float ReachRadius;
    //Detection AestheticProperties
    [Header("Enemy Sensor AestheticProperties")]
    public float FOV = 90.0f;
    public float ViewDistance = 50f;
    [Header("Enemy Debug AestheticProperties")]
    public float DebugRadius;
}
