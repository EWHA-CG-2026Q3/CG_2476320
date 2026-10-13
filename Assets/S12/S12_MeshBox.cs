using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S12_MeshBox : MonoBehaviour
{
    void Start()
    {
        Vector3[] v = new Vector3[]
        {
            new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0), // 앞면 0-3
            new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1), // 뒷면 4-7
        };

        int[] t = new int[]
        {
            0,2,1, 0,3,2,  // 앞면
            5,7,4, 5,6,7,  // 뒷면
            4,3,0, 4,7,3,  // 왼쪽
            1,6,5, 1,2,6,  // 오른쪽
            3,6,2, 3,7,6,  // 윗면
            0,1,5, 0,5,4,  // 아랫면
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
