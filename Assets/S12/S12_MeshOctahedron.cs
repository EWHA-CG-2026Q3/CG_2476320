using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S12_MeshOctahedron : MonoBehaviour
{
    void Start()
    {
        Vector3[] v = new Vector3[]
        {
            new Vector3( 0,  1,  0),  // 0 위
            new Vector3( 1,  0,  0),  // 1 오른쪽
            new Vector3( 0,  0,  1),  // 2 앞
            new Vector3(-1,  0,  0),  // 3 왼쪽
            new Vector3( 0,  0, -1),  // 4 뒤
            new Vector3( 0, -1,  0),  // 5 아래
        };

        int[] t = new int[]
        {
            0,2,1,  0,3,2,  0,4,3,  0,1,4,  // 위 4면
            5,1,2,  5,2,3,  5,3,4,  5,4,1,  // 아래 4면
        };

        Mesh mesh = new Mesh();
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial =
            new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
