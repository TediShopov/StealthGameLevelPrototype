using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPathGenerator
{
    public System.Random LevelRandom { get; set; }
    public Graph<Vector2> Roadmap { get; set; }
    public List<Vector2> GeneratePath();

    public List<List<Vector2>> GeneratePaths(int paths);
}