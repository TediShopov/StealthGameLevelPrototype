using UnityEngine;

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

    public void Init()
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
                PatrolPaths[i].SetPatrolPath(paths[i]);
                //                PatrolPaths[i].Positions = paths[i];
                //                PatrolPaths[i].SetInitialPositionToPath();
            }
        }
        Debug.Log("Roamd and patrol paths initialized");
    }

    private PatrolPath[] GetPatrolPaths()
    {
        GameObject level = Helpers.SearchForTagUpHierarchy(this.gameObject, "Level");
        if (level == null)
            return new PatrolPath[0];
        return level.GetComponentsInChildren<PatrolPath>();
    }
}