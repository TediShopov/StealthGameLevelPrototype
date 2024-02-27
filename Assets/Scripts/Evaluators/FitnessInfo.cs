using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StealthLevelEvaluation;
using UnityEditor;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

public class FitnessInfo
{
    public FitnessInfo(params PhenotypeFitnessEvaluation[] evals)
    {
        this.FitnessEvaluations = evals.ToList();
    }

    public List<PhenotypeFitnessEvaluation> FitnessEvaluations;
}

public class FitnessInfoVisualizer : MonoBehaviour
{
    //    public static FitnessInfo AttachInfo(GameObject to, )
    //    {
    //        var obj = new GameObject("FitnessInfo", new System.Type[] { typeof(FitnessInfo) });
    //        obj.transform.SetParent(to.transform, false);
    //        var info = obj.GetComponent<FitnessInfo>();
    //        info.FitnessEvaluations = evals.ToList();
    //        return info;
    //    }
    public static FitnessInfo AttachInfo(GameObject to, FitnessInfo info)
    {
        var obj = new GameObject("FitnessInfo", new System.Type[] { typeof(FitnessInfoVisualizer) });
        obj.transform.SetParent(to.transform, false);
        var infoMono = obj.GetComponent<FitnessInfoVisualizer>();
        infoMono.Info = info;
        return info;
    }

    public FitnessInfo Info;

    public void OnDrawGizmosSelected()
    {
        //        foreach (var eval in Info.FitnessEvaluations)
        //        {
        //            eval.OnSelected();
        //        }
        string allEvals = "";
        foreach (var evaluation in Info.FitnessEvaluations)
        {
            allEvals += evaluation.ToString();
        }
        Handles.Label(this.transform.position, allEvals.ToString());
    }
}