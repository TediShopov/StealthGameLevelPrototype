using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompareIERunsOfLevelSolvers : MonoBehaviour
{
    public StealthLevelIEMono IE;

    public List<InteractiveEvalutorMono> IEEvaluators;

    public void RunTests()
    {
        IE.FinishIESetup += OnEachIESetup;
        foreach (var evaluator in IEEvaluators)
        {
            IE.PhenotypeEvaluator = evaluator;
            IE.RunWithSyntheticModel();
        }

        //Setup usre preferences

        IE.FinishIESetup -= OnEachIESetup;
    }

    private void OnEachIESetup(object sender, EventArgs e)
    {
        IE.GAGenerationLogger = new GAGenerationLogger(IE.LogEveryGenerations);
        IE.GAGenerationLogger.BindTo(IE);
        IE.GAGenerationLogger.AlgorithmName =
            $"SLVR_{IE.PhenotypeEvaluator.EvaluatorHolder.name}" + IE.GAGenerationLogger.AlgorithmName;
    }
}