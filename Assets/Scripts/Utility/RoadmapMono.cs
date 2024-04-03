using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class RoadmapMono : MonoBehaviour
{
    public Grid Grid;

    //The colors used to differentiate between map zones
    public List<Color> Colors = new List<Color>();

    //the roadmap graph. The graph to be used by patrol to form their path
    public Graph<Vector2> RoadMap = new Graph<Vector2>();

    //Transforms the unity grid to c# binary represenetaion of the level
    //Holds the zones used the generated the roadmap
    public NativeGrid<int> LevelGrid;

    public bool DebugDraw = false;

    private void OnDrawGizmosSelected()
    {
        if (DebugDraw)
        {
            if (LevelGrid == null) return;
            Gizmos.color = Color.blue;
            DebugDrawGridByIndex();
            Graph<Vector2>.DebugDrawGraph(RoadMap, Color.red, Color.green, 0.01f);
            //DebugSimplifiedConnections();
            //Debug draw nodes with only one connecitons
        }
    }

    private void Awake()
    {
        Colors = new List<Color>();
        Colors.Add(new Color(1, 0, 0, 0.2f));
        Colors.Add(new Color(0, 1, 0, 0.2f));
        Colors.Add(new Color(0, 0, 1, 0.2f));
        Colors.Add(new Color(1, 1, 0, 0.2f));
        Colors.Add(new Color(1, 0, 1, 0.2f));
        Colors.Add(new Color(0, 1, 1, 0.2f));
    }

    private void DebugDrawGridByIndex()
    {
        LevelGrid.ForEach((row, col) =>
        {
            if (LevelGrid.Get(row, col) != -1)
            {
                Gizmos.color = GetColorForValue(LevelGrid.Get(row, col));
                Vector3 worldPosition = Grid.GetCellCenterWorld(LevelGrid.GetUnityCoord(row, col));
                worldPosition.z = 0;
                Vector3 cellsize = Grid.cellSize;
                cellsize.z = 1;
                Gizmos.DrawCube(worldPosition, Grid.cellSize);
            }
        });
    }

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
}