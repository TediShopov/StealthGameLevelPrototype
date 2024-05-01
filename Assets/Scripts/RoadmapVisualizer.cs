using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Grid))]
public class RoadmapVisualizer : MonoBehaviour
{
    public bool DebugDraw = true;
    public UnboundedGrid Grid;
    public List<Color> Colors;
    public LevelPhenotype Phenotype;

    public void Awake()
    {
        Grid = new UnboundedGrid(GetComponent<Grid>());
        Colors = new List<Color>();
        Colors.Add(new Color(1, 0, 0, 0.2f));
        Colors.Add(new Color(0, 1, 0, 0.2f));
        Colors.Add(new Color(0, 0, 1, 0.2f));
        Colors.Add(new Color(1, 1, 0, 0.2f));
        Colors.Add(new Color(1, 0, 1, 0.2f));
        Colors.Add(new Color(0, 1, 1, 0.2f));
        var level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        Phenotype = level.GetComponentInChildren<LevelChromosomeMono>().Chromosome.Phenotype;
    }

    // Start is called before the first frame update
    public Color GetColorForValue(int index)
    {
        if (index >= 0)
        {
            int colorIndex = index % Colors.Count;
            //Circular buffer to assign colors
            return Colors[colorIndex];
        }
        else
        {
            return new Color(0, 0, 0);
        }
    }

    private void DebugDrawGridByIndex()
    {
        Phenotype.Zones.ForEach((row, col) =>
        {
            if (Phenotype.Zones.Get(row, col) != -1)
            {
                Gizmos.color = GetColorForValue(Phenotype.Zones.Get(row, col));
                Vector3 worldPosition = Grid.GetCellCenterWorld(Phenotype.Zones.GetUnityCoord(row, col));
                worldPosition.z = 0;
                Vector3 cellsize = new Vector3(Grid.cellSize, Grid.cellSize, Grid.cellSize);
                cellsize.z = 1;
                Gizmos.DrawCube(worldPosition, cellsize);
            }
        });
    }

    //    private void DebugSimplifiedConnections()
    //    {
    //        Gizmos.color = Color.red;
    //        foreach (var sc in _debugSimplifiedConnections)
    //        {
    //            Vector3 cellsize = new Vector3(Grid.cellSize, Grid.cellSize, Grid.cellSize);
    //            cellsize.z = 1;
    //            Gizmos.DrawLine(sc.Item1, sc.Item2);
    //        }
    //    }

    private void OnDrawGizmosSelected()
    {
        if (DebugDraw)
        {
            if (Phenotype.Zones == null) return;
            Gizmos.color = Color.blue;
            DebugDrawGridByIndex();
            Graph<Vector2>.DebugDrawGraph(Phenotype.Roadmap, Color.red, Color.green, 0.01f);
            //DebugSimplifiedConnections();
            //Debug draw nodes with only one connecitons
        }
    }
}