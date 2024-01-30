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
        var generatorPosition = RoadMapGenerator.gameObject.transform.position;
        RoadMapGenerator.Init();
        //Use voronoi roadmap generator to produce culled roadmap graph
        Graph = RoadMapGenerator.RoadMap;

        //Generate Patrol Paths
        if (PathGenerator != null)
        {
            PathGenerator.Roadmap = Graph;
            PatrolPaths = GetPatrolPaths();
            var paths = PathGenerator.GeneratePaths(PatrolPaths.Length);
            for (int i = 0; i < PatrolPaths.Length; i++)
            {
                PatrolPaths[i].Positions = paths[i];
                PatrolPaths[i].SetInitialPositionToPath();
            }
        }
    }

    private PatrolPath[] GetPatrolPaths()
    {
        // The root object the stealth level
        GameObject level = this.gameObject;
        while (level != null)
        {
            if (level.CompareTag("Level"))
                break;
            if (level.transform.parent != null)
                level = level.transform.parent.gameObject;
            else
                break;
        }
        if (level == null)
            return new PatrolPath[0];
        return level.GetComponentsInChildren<PatrolPath>();
    }
}