using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine;

[ExecuteInEditMode]
public class RRTPerformanceTestSuite : MonoBehaviour
{
    private GridObjectLayout gridObjectLayout;
    public bool Reorganize;
    public bool LogPerformances;

    public List<LevelEngagementEvaluator> levelEngagementEvaluators
        = new List<LevelEngagementEvaluator>();

    public GameObject[] Levels;

    // Update is called once per frame
    private void Update()
    {
        if (Reorganize)
        {
            ReorganizeObjects();
            Reorganize = false;
        }
        if (LogPerformances)
        {
            LogPerformances = false;
            TestRRT();
        }
    }
    public void ClearAllPreviousRuns()
    {
        EvaluatorMono[] monos = this.transform.GetComponentsInChildren<EvaluatorMono>();
        foreach (var mono in monos)
            DestroyImmediate(mono.gameObject);
    }
    public void TestRRT()
    {
        StringBuilder sb = new StringBuilder();
        ClearAllPreviousRuns();
        sb.Append(GetHeader());
        foreach (var level in Levels)
        {
            foreach (var evaluators in levelEngagementEvaluators)
            {
                sb.Append($"{level.name},");
                var eval = (LevelEngagementEvaluator)evaluators.AttachToAndEvaluate(
                    level.GetComponentInChildren<LevelChromosomeMono>().Chromosome);
                sb.Append($"{EvaluatorName(eval)},");
                AppendRRT(sb, eval.RRTList);
                sb.Append('\n');
            }
        }
        Helpers.SaveToCSV($"Tests/{GetFilename()}.txt", sb.ToString());
    }

    public void AppendRRT(StringBuilder sb, List<RRT> rrts)
    {
        foreach (var rrtRun in rrts)
        {
            sb.Append($"{rrtRun.Succeeded()},{rrtRun.Time},");
        }
    }

    public string GetFilename()
    {
        return $"RRT_Perfroamnce_Comp";
    }
    public string GetHeader()
    {
        string header = "GEN,TYPE,";
        foreach (var evaluator in levelEngagementEvaluators)
        {
            for (int i = 0; i < evaluator.RRTAttemps; i++)
            {
                string numberedEval = $"Run{i + 1}_Succeeded,Run{i + 1}_Time";
                header += numberedEval;
                //                string numberedEval = $"{EvaluatorName(evaluator)}({i})";
                //                header += $"{numberedEval}_Suceess,{numberedEval}_Time";
            }
        }
        header += "\n";
        return header;
    }
    public string EvaluatorName(LevelEngagementEvaluator evaluator)
    {
        return $"{evaluator.RRT.GetType().Name}_STEP{evaluator.RRT.SteerStep}_I{evaluator.RRT.MaxIterations}";
    }

    public void ReorganizeObjects()
    {
        //Get levels
        Levels = this.transform
            .GetComponentsInChildren<LevelChromosomeMono>()
            .Select(x => x.transform.parent.gameObject)
            .ToArray();

        gridObjectLayout = new GridObjectLayout(new Vector2(40, 40));
        gridObjectLayout.SpawnGrid(Levels.Length, this.transform);

        foreach (GameObject level in Levels)
        {
            var gridObject = gridObjectLayout.GetNextLevelObject();
            level.transform.position = gridObject.transform.position;
            level.gameObject.name = gridObject.gameObject.name;
        }
        gridObjectLayout.PrepareForNewGeneration();
        foreach (var obj in gridObjectLayout.LevelObjects)
        {
            DestroyImmediate(obj);
        }
    }
}