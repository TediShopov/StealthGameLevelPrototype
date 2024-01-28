using UnityEngine;
using System.Linq;

//The purpose of the class is to setup all relevant classes/algorithms for initializing a single stealth 
//level
public class InitializeStealthLevel : MonoBehaviour
{
    public FloodfilledRoadmapGenerator RoadMapGenerator;
    public Graph<Vector2> Graph;
    public float BiasPathDistance = 15.0f;
    public int AttempsToGetCorrectBiasPathDistance = 3;
    private PatrolPath[] PatrolPaths;
    public PathGeneratorClass PathGenerator;

    // Start is called before the first frame update
    private void Start()
    {
        if (RoadMapGenerator == null) return;
        //Initialize the roadmap
        RoadMapGenerator.Init();
        //Use voronoi roadmap generator to produce culled roadmap graph
        Graph = RoadMapGenerator.RoadMap;

        //Generate Patrol Paths
        if (PathGenerator != null)
        {
            PathGenerator.Roadmap = Graph;
            PatrolPaths = FindObjectsOfType<PatrolPath>().Where(x => x.Randomized == true).ToArray();
            var paths = PathGenerator.GeneratePaths(PatrolPaths.Length);
            for (int i = 0; i < PatrolPaths.Length; i++)
            {
                PatrolPaths[i].Positions = paths[i];
                PatrolPaths[i].SetInitialPositionToPath();
            }
        }
    }
}