using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 창호지 패널 커스텀 메시 (C3: REQ_Lantern.md §C3)
// 창살 벽 안쪽에 붙이는 얇은 발광 사각 패널 (한지 느낌)
// 수업 근거: 커스텀 메시(S04), [ExecuteAlways] + OnValidate 즉시 재생성
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_HanjiPanelBuilder : MonoBehaviour
{
    [Header("크기")]
    public float width  = 1.0f;
    public float height = 1.5f;

    [Header("세분화 (발광 그라데이션 부드러움)")]
    public int segX = 4;
    public int segY = 6;

    [Header("옵션")]
    public float insetFromLattice = 0.015f; // 창살보다 안쪽 (m)

    void OnEnable()   => Rebuild();
    void OnValidate()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall += () => { if (this) Rebuild(); };
#else
        Rebuild();
#endif
    }

    public void Rebuild()
    {
        if (segX < 1) segX = 1;
        if (segY < 1) segY = 1;

        var verts   = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var tris    = new List<int>();

        float hw = width  * 0.5f;
        float hh = height * 0.5f;

        // (segX+1) × (segY+1) 격자 — UV(0,0) = 왼쪽 아래, (1,1) = 오른쪽 위
        // 수업 근거: 격자 메시(S03/S04), uv 직접 채우기
        for (int row = 0; row <= segY; row++)
        {
            float v = row / (float)segY;
            for (int col = 0; col <= segX; col++)
            {
                float u = col / (float)segX;
                float x = Mathf.Lerp(-hw, hw, u);
                float y = Mathf.Lerp(-hh, hh, v);
                // 패널은 XY 평면, 법선은 -Z(창살 안쪽 → 바깥 방향)
                verts.Add(new Vector3(x, y, insetFromLattice));
                normals.Add(Vector3.back); // 창살 바깥을 향해 빛을 내보냄
                uvs.Add(new Vector2(u, v));
            }
        }

        int stride = segX + 1;
        for (int row = 0; row < segY; row++)
            for (int col = 0; col < segX; col++)
            {
                int bl = row * stride + col;
                int br = bl + 1;
                int tl = bl + stride;
                int tr = tl + 1;
                // CCW (앞면 = -Z 방향)
                tris.Add(bl); tris.Add(tl); tris.Add(tr);
                tris.Add(bl); tris.Add(tr); tris.Add(br);
            }

        var mesh = new Mesh { name = "HanjiPanel" };
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = mesh;
        // Cast Shadows = Off: C4 InteriorLight가 패널에 막히지 않게
        GetComponent<MeshRenderer>().shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
