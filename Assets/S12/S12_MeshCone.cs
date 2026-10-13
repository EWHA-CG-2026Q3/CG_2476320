using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S12_MeshCone : MonoBehaviour
{
    void Start()
    {
        int n = 8;
        Vector3[] v = new Vector3[n + 1];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            v[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
        }
        v[n] = new Vector3(0f, 1f, 0f); // 꼭대기

        int[] t = new int[n * 6];
        int idx = 0;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            t[idx++] = 0; t[idx++] = next; t[idx++] = i;   // 밑면 (팬)
            t[idx++] = i; t[idx++] = next; t[idx++] = n;    // 옆면
        }

        Mesh mesh = new Mesh();
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sharedMaterial =
            new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
