using UnityEngine;

// 과제: 높이(y)에 비례해 x축 방향으로 미는 shear 변환
// k = (학번 끝자리 + 1) ÷ 5 = (0 + 1) ÷ 5 = 0.2
[ExecuteAlways]
[RequireComponent(typeof(DiamondMesh))]
public class S09_Shear : MonoBehaviour
{
    [SerializeField] float k = 0.2f;

    DiamondMesh diamondMesh;

    void OnEnable()
    {
        diamondMesh = GetComponent<DiamondMesh>();
    }

    void Update()
    {
        if (diamondMesh == null || diamondMesh.BaseVertices == null) return;

        float[,] S = ShearMatrixRaw(k);
        Vector3[] baseVertices = diamondMesh.BaseVertices;
        Vector3[] verts = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
            verts[i] = FromHomogeneous(MultiplyMatrixVectorRaw(S, ToHomogeneous(baseVertices[i])));
        diamondMesh.SetVertices(verts);
    }

    void OnValidate()
    {
        // 꼭대기 정점 (0.5, 1, 0.5) 결과 출력
        Vector3 top = new Vector3(0.5f, 1f, 0.5f);
        Vector4 result = MultiplyMatrixVectorRaw(ShearMatrixRaw(k), ToHomogeneous(top));
        Debug.Log($"[S09_Shear] k={k} / 꼭대기 정점 {top} → {FromHomogeneous(result)}");
    }

    // e₁, e₃, 원점은 그대로 / e₂(0,1,0) → (k, 1, 0): x가 y에 비례해 밀림
    float[,] ShearMatrixRaw(float k)
    {
        return new float[,] {
            { 1f, k,  0f, 0f },
            { 0f, 1f, 0f, 0f },
            { 0f, 0f, 1f, 0f },
            { 0f, 0f, 0f, 1f }
        };
    }

    Vector4 ToHomogeneous(Vector3 v) => new Vector4(v.x, v.y, v.z, 1f);

    Vector3 FromHomogeneous(Vector4 h) => new Vector3(h.x, h.y, h.z);

    Vector4 MultiplyMatrixVectorRaw(float[,] M, Vector4 v)
    {
        float[] input = { v.x, v.y, v.z, v.w };
        float[] result = new float[4];
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 4; col++)
                result[row] += M[row, col] * input[col];
        return new Vector4(result[0], result[1], result[2], result[3]);
    }
}
