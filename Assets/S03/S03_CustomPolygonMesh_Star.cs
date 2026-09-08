using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Star : MonoBehaviour
{
    void Start()
    {
        Vector3[] vertices = new Vector3[]
        {
            new Vector3( 0.000f,  0.000f, 0f),
            new Vector3( 0.000f,  1.000f, 0f),
            new Vector3(-0.951f,  0.309f, 0f),
            new Vector3(-0.588f, -0.809f, 0f),
            new Vector3( 0.588f, -0.809f, 0f),
            new Vector3( 0.951f,  0.309f, 0f),
            new Vector3( 0.235f,  0.324f, 0f),
            new Vector3(-0.235f,  0.324f, 0f),
            new Vector3(-0.381f, -0.124f, 0f),
            new Vector3( 0.000f, -0.400f, 0f),
            new Vector3( 0.381f, -0.124f, 0f),
        };

        int[] triangles = new int[]
        {
            0, 1, 7,
            0, 7, 2,
            0, 2, 8,
            0, 8, 3,
            0, 3, 9,
            0, 9, 4,
            0, 4, 10,
            0, 10, 5,
            0, 5, 6,
            0, 6, 1,
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
