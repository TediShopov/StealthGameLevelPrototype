using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class LineRendererColliderSync : MonoBehaviour
{
    private LineRenderer LineRenderer;
    private MeshCollider MeshCollider;
    public bool UpdateCollider = false;

    // Start is called before the first frame update
    private void Start()
    {
        MeshCollider = GetComponent<MeshCollider>();
        LineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (UpdateCollider)
        {
            SetMeshCollider();
            UpdateCollider = false;
        }
    }

    private void SetMeshCollider()
    {
        Mesh mesh = new Mesh();
        LineRenderer.BakeMesh(mesh, true);
        MeshCollider.sharedMesh = mesh;
    }

    //    private void SetEdgeCollider(LineRenderer lineRenderer)
    //    {
    //        List<Vector2> edges = new List<Vector2>();
    //        for (int i = 0; i < lineRenderer.positionCount; i++)
    //        {
    //            Vector3 lineRenderPoint = lineRenderer.GetPosition(i);
    //            edges.Add(lineRenderPoint);
    //        }
    //        EdgeCollider2D.SetPoints(edges);
    //    }
}