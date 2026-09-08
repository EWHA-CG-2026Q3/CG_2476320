using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S03_CustomPolygonMesh_Star : MonoBehaviour
{
    void Start()
    {
        Vector3[] vertices = new Vector3[]
        {
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
            5, 7, 6,
            5, 8, 7,
            5, 9, 8,
            5, 6, 0,
            6, 7, 1,
            7, 8, 2,
            8, 9, 3,
            9, 5, 4,
        };

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
