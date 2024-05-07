using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class FutureLevelSlider : MonoBehaviour
{
    public PatrolEnemyMono[] PatrolObjects = null;
    public LevelPhenotype LevelPhenotype;
    private List<List<Vector3>> SolutionPaths;
    public bool EnableSetLevel = true;
    public bool EnableDiscreteTimes = true;
    public float SetTime = 0.0f;
    public bool DrawHeatmap = true;

    public void Awake()
    {
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        this.PatrolObjects = level.GetComponentsInChildren<PatrolEnemyMono>();
        this.LevelPhenotype = level.GetComponentInChildren<LevelChromosomeMono>()
            .Chromosome.Phenotype;
        this.StartCoroutine(RefreshLevelSolutionObjects());

        for (int i = 0; i < LevelPhenotype.Threats.Count; i++)
        {
            PatrolObjects[i].Patrol = (Patrol)LevelPhenotype.Threats[i];
        }
    }

    public virtual void Update()
    {
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        var rrts = level.GetComponentsInChildren<RapidlyExploringRandomTreeVisualizer>();

        SolutionPaths = rrts.Select(x => x.Path)
            .Where(X => X.Count != 0)
           .ToList();
        if (PatrolObjects == null) return;
        foreach (var threa in PatrolObjects)
        {
            threa.Reset();
            if (EnableDiscreteTimes)
            {
                float step = LevelPhenotype.FutureLevel.Step;
                float discreteTime = step
                    * Mathf.FloorToInt(SetTime / step);
                threa.TimeMove(discreteTime);
            }
            else
            {
                threa.TimeMove(SetTime);
            }
        }
    }

    public void OnDrawGizmosSelected()
    {
        var heatmap = LevelPhenotype.FutureLevel.GetHeatmap();
        var _grid = LevelPhenotype.Zones.Grid;

        if (heatmap != null && DrawHeatmap)
        {
            heatmap.ForEach(
                (x, y) =>
                {
                    Vector3 pos = LevelPhenotype.Zones.Grid.GetCellCenterWorld(
                    heatmap.GetUnityCoord(x, y));
                    float value = heatmap.Get(x, y);
                    float reverse = 1 - value;

                    //Gizmos.color = new Color(reverse, reverse, reverse, 0.5f);
                    //Gizmos.color = new Color(0, 0, 0, value);
                    Gizmos.color = new Color(reverse, reverse, reverse, 0.5f);
                    pos = this.transform.TransformPoint(pos);
                    Gizmos.DrawCube(pos,
                        new Vector3(_grid.cellSize, _grid.cellSize, _grid.cellSize));
                }
                );
        }
        if (EnableSetLevel == false) return;
        if (SolutionPaths == null) return;
        foreach (var path in SolutionPaths)
        {
            Gizmos.color = Color.green;
            Vector2 position = this.transform.TransformPoint(GetPosition(path, SetTime));
            if (EnableDiscreteTimes)
            {
                float step = LevelPhenotype.FutureLevel.Step;
                float discreteTime = step * Mathf.CeilToInt(SetTime / step);
                position = this.transform.TransformPoint(GetPosition(path, discreteTime));
            }
            Gizmos.DrawSphere(position, 0.1f);
        }
    }

    private Vector2 GetPosition(List<Vector3> solutionPath, float time)
    {
        if (time > solutionPath[solutionPath.Count - 1].z)
            return Vector2.zero;

        int index = 0;
        while (index <= solutionPath.Count - 1)
        {
            //If current time is smaller than the time of the path in the next node
            if (time < solutionPath[index].z)
            {
                if (index == 0) return Vector2.zero;

                //Position is on this segment
                float relTime = Mathf.InverseLerp(solutionPath[index - 1].z, solutionPath[index].z, time);
                Vector2 pos = Vector2.Lerp(solutionPath[index - 1], solutionPath[index], relTime);
                return pos;
            }
            index++;
        }
        return Vector2.zero;
    }

    private IEnumerator RefreshLevelSolutionObjects()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(2.0f);
        }
    }
}