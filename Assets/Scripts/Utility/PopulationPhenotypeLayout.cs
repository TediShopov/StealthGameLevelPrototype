using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Interface of how population phenotype is layed out in Unity editor
//
public interface IPopulationLayout
{
    public void Reset();

    public bool IsDefaultSpotAvailable();

    public GameObject DefaultEvalutionSpot();

    public bool InsertIntoLayout(GameObject obj);
}

public class PopulationPhenotypeLayout : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }
}