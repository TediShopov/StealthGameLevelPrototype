using UnityEngine;
using UnityEngine.Profiling;

[RequireComponent(typeof(MeshFilter))]
[ExecuteAlways]
public class FieldOfView : MonoBehaviour
{
    public int RayCount;
    public float PointingAngle;
    public GameObject SearchedObject;
    public DefaultEnemyProperties EnemyProperties;

    private Mesh _mesh;
    private MeshFilter _meshFilter;

    public LayerMask ObstacleLayerMask;

    // Start is called before the first frame update
    private void Awake()
    {
        _mesh = new Mesh();
        _meshFilter = this.gameObject.GetComponent<MeshFilter>();
        _meshFilter.mesh = _mesh;
    }

    public void RebuidMeshComponent()
    {
        Vector3 LocalOrigin = Vector3.zero;
        Vector3 GlobalOrigin = this.transform.position;

        float angle = GetStartinAngle();
        float angleIncrease = EnemyProperties.FOV / RayCount;

        Vector3[] vertices = new Vector3[RayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[RayCount * 3];

        float modifer = Helpers.GetAngleFromVectorFloat(this.transform.right);
        vertices[0] = LocalOrigin;
        int vertexIndex = 1;
        int trinagleIndex = 0;
        for (int i = 0; i <= RayCount; i++)
        {
            Vector3 vertex;
            //Raycast operrate on a global position
            //Global angle

            float globalAngle = angle + modifer;
            RaycastHit2D raycastHit2D = Physics2D.Raycast(GlobalOrigin, Helpers.GetVectorFromAngle(globalAngle), EnemyProperties.ViewDistance, ObstacleLayerMask);
            if (raycastHit2D.collider == null)
            {
                //Local Angle
                vertex = LocalOrigin + Helpers.GetVectorFromAngle(angle) * EnemyProperties.ViewDistance;
            }
            else
            {
                //Raycast hit point need to be turned to local positionn
                Vector3 localHit = this.transform.InverseTransformPoint(raycastHit2D.point);
                vertex = localHit;
            }
            vertices[vertexIndex] = vertex;
            if (i > 0)
            {
                triangles[trinagleIndex + 0] = 0;
                triangles[trinagleIndex + 1] = vertexIndex - 1;
                triangles[trinagleIndex + 2] = vertexIndex;
                trinagleIndex += 3;
            }
            vertexIndex++;
            angle -= angleIncrease;
        }

        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.triangles = triangles;
    }

    // Update is called once per frame
    private void Update()
    {
        RebuidMeshComponent();
        if (TestCollision(SearchedObject))
        {
            Debug.Log("Game is lost");
            //SceneManager.LoadScene(0);
        }
    }

    public static Bounds GetFovBounds(FutureTransform ft, float vd, float fov)
    {
        Profiler.BeginSample("FOV Bound Calculation");
        Vector2 boundsCenter = ft.Position + ft.Direction * vd / 2.0f;
        Bounds bounds = new Bounds(boundsCenter, new Vector3(0, 0, 0));

        Vector2 fovPeak = ft.Position + ft.Direction * vd;
        Vector2 fovPos = ft.Position;

        Vector2 fovBoundTwo = ft.Position + (Vector2)(Quaternion.AngleAxis(fov / 2.0f, Vector3.forward) * ft.Direction * vd);
        Vector2 fovBoundOne = ft.Position + (Vector2)(Quaternion.AngleAxis(-fov / 2.0f, Vector3.forward) * ft.Direction * vd);

        //            bounds.Encapsulate(minLeft);
        //            bounds.Encapsulate(maxRight);
        bounds.Encapsulate(fovPeak);
        bounds.Encapsulate(fovPos);
        bounds.Encapsulate(fovBoundOne);
        bounds.Encapsulate(fovBoundTwo);
        Profiler.EndSample();
        return bounds;
    }

    public Bounds GetBounds()
    {
        return GetFovBounds(
            new FutureTransform()
            {
                Position = transform.position,
                Direction = GetGlobalDirection()
            },
            EnemyProperties.ViewDistance,
            EnemyProperties.FOV);
    }

    public Vector3 GetGlobalDirection()
    {
        float modifer = Helpers.GetAngleFromVectorFloat(this.transform.right);
        return Helpers.GetVectorFromAngle(PointingAngle + modifer);
    }

    public bool TestCollision(GameObject gameObject)
    {
        //Always return if object is empty
        if (gameObject == null) return false;
        Vector3 testPosition = gameObject.transform.position;
        //Gloabar direction vector
        Vector3 globalPointingDirection = GetGlobalDirection().normalized;
        Vector3 vectorToTarget = (testPosition - transform.position).normalized;
        if (!Physics2D.Linecast(this.transform.position, testPosition, ObstacleLayerMask))
        {
            float angle = Vector3.Angle(globalPointingDirection, vectorToTarget);
            if (angle < EnemyProperties.FOV / 2.0f && Vector3.Distance(testPosition, this.transform.position) <= EnemyProperties.ViewDistance)
            {
                return true;
            }
        }
        return false;
    }

    public bool TestCollision(Vector2 testPosition, Vector2 fovPosition, Vector2 globalDirection)
    {
        //Gloabar direction vector
        Vector2 vectorToTarget = (testPosition - fovPosition).normalized;
        if (!Physics2D.Linecast(testPosition, fovPosition, ObstacleLayerMask))
        {
            float angle = Vector2.Angle(globalDirection, vectorToTarget);
            if (angle < EnemyProperties.FOV / 2.0f && Vector2.Distance(testPosition, fovPosition) <= EnemyProperties.ViewDistance)
            {
                return true;
            }
        }
        return false;
    }

    public static bool TestCollision(Vector2 testPosition, FutureTransform ft, float fovAngle, float distance, LayerMask layerMask)
    {
        //Gloabar direction vector
        Vector2 vectorToTarget = (testPosition - ft.Position).normalized;
        if (Vector2.Distance(testPosition, ft.Position) > distance)
            return false;
        float angle = Vector2.Angle(ft.Direction, vectorToTarget);
        if (angle >= fovAngle / 2.0f)
            return false;
        if (!Physics2D.Linecast(testPosition, ft.Position, layerMask))
        {
            return true;
        }
        return false;
    }

    public float GetStartinAngle()
    {
        return (PointingAngle + EnemyProperties.FOV / 2.0f);
    }

    public void OnDrawGizmos()
    {
        Debug.DrawRay(this.transform.position, GetGlobalDirection(), Color.yellow);
    }
}