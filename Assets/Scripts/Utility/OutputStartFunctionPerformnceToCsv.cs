using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OutputStartFunctionPerformnceToCsv : MonoBehaviour
{
    public List<UnityEvent> Events;
    public int Iterations;
    public string Filepath;
    void Start()
    {
        string s="";
        foreach (var e in Events)
        {

            s += e.GetPersistentTarget(0).ToString();
            float[] results = Helpers.TrackExecutionTime(e.Invoke, Iterations);
            for (var i = 0; i < Iterations; i++) 
            {
                s += results[i].ToString()+",";
            }
            s=s.Remove(s.Length - 1,1);
            s += "\n";
        }
        Helpers.SaveToCSV($"Tests/{Filepath}.txt", s);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
