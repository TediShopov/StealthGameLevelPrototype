using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FieldOfView : MonoBehaviour
{
    public MeshFilter meshFilter;
    Mesh mesh;
    public float FOV = 90.0f;
    public float ViewDistance = 50f;
    public int RayCount;
    public float PointingAngle;
    
    // Start is called before the first frame update
    void Start()
    {
        mesh=new Mesh();
        meshFilter.mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 LocalOrigin = Vector3.zero;
        Vector3 GlobalOrigin = this.transform.position;

        float angle = GetStartinAngle();
        float angleIncrease = FOV / RayCount;

        Vector3[] vertices = new Vector3[RayCount + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[RayCount*3];

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
            RaycastHit2D raycastHit2D = Physics2D.Raycast(GlobalOrigin, Helpers.GetVectorFromAngle(globalAngle), ViewDistance);
            if (raycastHit2D.collider == null)
            {
                //Local Angle
                vertex =  LocalOrigin + Helpers.GetVectorFromAngle(angle) * ViewDistance;
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
                triangles[trinagleIndex + 1] = vertexIndex -1;
                triangles[trinagleIndex + 2] = vertexIndex;
                trinagleIndex += 3;
            }
            vertexIndex++;
            angle -= angleIncrease;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }
    public float GetStartinAngle ()
    {
        return (PointingAngle + FOV / 2.0f); 
    }
    public void OnDrawGizmos()
    {
    }
}
