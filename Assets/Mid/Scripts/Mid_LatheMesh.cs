using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 범용 회전체 메시 생성기 — 단면 곡선(profile)을 Y축 둘레로 회전시켜 메시를 만든다
// 수업 근거: 커스텀 메시(S04 vertices/normals/triangles 직접 채우기),
//           Y축 회전 행렬 합성(S10), UV v좌표 = 호 길이 비율(S16)
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_LatheMesh : MonoBehaviour
{
    [Header("단면 곡선 (x=반지름 cm, y=높이 cm)")]
    public List<Vector2> profile = new List<Vector2>
    {
        new Vector2(4f, 0f), new Vector2(7f, 5f),
        new Vector2(5f, 9f), new Vector2(3.5f, 10f),
    };

    [Header("옵션")]
    [Min(3)] public int    segments    = 24;
    public bool            closeBottom = false;
    public bool            closeTop    = false;
    public bool            doubleSided = false;
    [Min(0f)] public float thickness   = 0f; // cm, >0이면 안쪽 벽+테두리 생성

    Mesh _mesh;

    void OnEnable() => Rebuild();

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
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
#endif
        if (profile == null || profile.Count < 2) return;
        _mesh = Build();
        var mf = GetComponent<MeshFilter>();
        if (mf) mf.sharedMesh = _mesh;
    }

    // ── 메인 빌더 ──────────────────────────────────────────────
    Mesh Build()
    {
        const float CM = 0.01f;
        var V = new List<Vector3>(); var N = new List<Vector3>();
        var U = new List<Vector2>(); var T = new List<int>();

        float[] arc    = CalcArc(profile, CM);
        float   totArc = arc[arc.Length - 1]; if (totArc < 1e-6f) totArc = 1f;

        bool hasWall = thickness > 0.01f || doubleSided;

        // ── 바깥 면
        int outerBase = 0;
        EmitRings(V, N, U, profile, CM, arc, totArc, false);
        EmitQuads(T, outerBase, profile.Count, segments, false);

        // ── 안쪽 면 (두께 또는 doubleSided)
        int innerBase = V.Count;
        List<Vector2> iProf = null;
        if (hasWall)
        {
            iProf = InnerProfile(thickness > 0.01f ? thickness : 0f);
            float[] iArc = CalcArc(iProf, CM);
            float   iTot = iArc[iArc.Length - 1]; if (iTot < 1e-6f) iTot = 1f;
            EmitRings(V, N, U, iProf, CM, iArc, iTot, true); // 법선 반전
            EmitQuads(T, innerBase, iProf.Count, segments, true);

            // 위 테두리 링 — 두께가 있을 때 윗면에서 테두리가 보임
            if (thickness > 0.01f)
            {
                int rimBase = V.Count;
                int topJ = profile.Count - 1;
                float rO = profile[topJ].x * CM, rI = iProf[topJ].x * CM, yTop = profile[topJ].y * CM;
                for (int i = 0; i <= segments; i++)
                {
                    float u = i / (float)segments, theta = u * 2f * Mathf.PI;
                    float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
                    V.Add(new Vector3(rO * c, yTop, rO * s)); N.Add(Vector3.up); U.Add(new Vector2(u, 1f));
                    V.Add(new Vector3(rI * c, yTop, rI * s)); N.Add(Vector3.up); U.Add(new Vector2(u, 0f));
                }
                for (int i = 0; i < segments; i++)
                {
                    // 위에서 볼 때 CCW: 바깥→바깥next→안next / 바깥→안next→안
                    int b = rimBase + i * 2;
                    T.Add(b); T.Add(b + 2); T.Add(b + 3);
                    T.Add(b); T.Add(b + 3); T.Add(b + 1);
                }
            }
        }

        // ── 바닥 cap (외부 반지름 기준 솔리드 디스크)
        if (closeBottom)
        {
            int capBase = V.Count;
            float r = profile[0].x * CM, y = profile[0].y * CM;
            V.Add(new Vector3(0, y, 0)); N.Add(Vector3.down); U.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < segments; i++)
            {
                float theta = i / (float)segments * 2f * Mathf.PI;
                float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
                V.Add(new Vector3(r * c, y, r * s)); N.Add(Vector3.down);
                U.Add(new Vector2(0.5f + 0.5f * c, 0.5f + 0.5f * s));
            }
            for (int i = 0; i < segments; i++)
            {
                // 아래에서 볼 때 CCW (법선 아래 방향)
                T.Add(capBase); T.Add(capBase + 1 + (i + 1) % segments); T.Add(capBase + 1 + i);
            }
        }

        // ── 위 cap
        if (closeTop)
        {
            int capBase = V.Count;
            float r = profile[profile.Count - 1].x * CM, y = profile[profile.Count - 1].y * CM;
            V.Add(new Vector3(0, y, 0)); N.Add(Vector3.up); U.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < segments; i++)
            {
                float theta = i / (float)segments * 2f * Mathf.PI;
                float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
                V.Add(new Vector3(r * c, y, r * s)); N.Add(Vector3.up);
                U.Add(new Vector2(0.5f + 0.5f * c, 0.5f + 0.5f * s));
            }
            for (int i = 0; i < segments; i++)
            {
                // 위에서 볼 때 CCW (법선 위 방향)
                T.Add(capBase); T.Add(capBase + 1 + i); T.Add(capBase + 1 + (i + 1) % segments);
            }
        }

        // ── 조립
        var mesh = new Mesh { name = gameObject.name + "_Lathe" };
        if (V.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U);
        mesh.SetTriangles(T, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── 헬퍼 ───────────────────────────────────────────────────

    // 단면 호 길이 누적
    float[] CalcArc(List<Vector2> prof, float scale)
    {
        var a = new float[prof.Count];
        for (int i = 1; i < prof.Count; i++)
        {
            Vector2 d = (prof[i] - prof[i - 1]) * scale;
            a[i] = a[i - 1] + Mathf.Sqrt(d.x * d.x + d.y * d.y);
        }
        return a;
    }

    // 두께만큼 반지름 줄인 안쪽 프로파일
    List<Vector2> InnerProfile(float th)
    {
        var p = new List<Vector2>(profile.Count);
        foreach (var v in profile) p.Add(new Vector2(Mathf.Max(0f, v.x - th), v.y));
        return p;
    }

    // 각 단면 점마다 segments+1개 정점 생성
    // 법선: 높이 접선 Tv × 둘레 접선 Th 의 외적 (S04 수업 내용)
    void EmitRings(List<Vector3> V, List<Vector3> N, List<Vector2> U,
        List<Vector2> prof, float CM, float[] arc, float totArc, bool flipNormal)
    {
        for (int j = 0; j < prof.Count; j++)
        {
            float r = prof[j].x * CM, y = prof[j].y * CM;
            float v = arc[j] / totArc;

            Vector2 tan;
            if      (j == 0)              tan = prof[1] - prof[0];
            else if (j == prof.Count - 1) tan = prof[j] - prof[j - 1];
            else                          tan = prof[j + 1] - prof[j - 1];
            tan.Normalize();

            for (int i = 0; i <= segments; i++)
            {
                float u = i / (float)segments, theta = u * 2f * Mathf.PI;
                float c = Mathf.Cos(theta), s = Mathf.Sin(theta);
                V.Add(new Vector3(r * c, y, r * s));
                // Tv = (tan.x*c, tan.y, tan.x*s), Th = (-s, 0, c)
                Vector3 Tv = new Vector3(tan.x * c, tan.y, tan.x * s);
                Vector3 Th = new Vector3(-s, 0f, c);
                Vector3 n  = Vector3.Cross(Tv, Th).normalized;
                N.Add(flipNormal ? -n : n);
                U.Add(new Vector2(u, v));
            }
        }
    }

    // 인접 링 사이 쿼드 삼각형
    void EmitQuads(List<int> T, int baseIdx, int ringCount, int segs, bool flip)
    {
        int stride = segs + 1;
        for (int j = 0; j < ringCount - 1; j++)
            for (int i = 0; i < segs; i++)
            {
                int LB = baseIdx + j * stride + i, RB = LB + 1;
                int LT = LB + stride,              RT = LT + 1;
                if (!flip) { T.Add(LB); T.Add(LT); T.Add(RT); T.Add(LB); T.Add(RT); T.Add(RB); }
                else       { T.Add(LB); T.Add(RT); T.Add(LT); T.Add(LB); T.Add(RB); T.Add(RT); }
            }
    }

    // ── Bake ───────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Bake Mesh Asset")]
    void BakeMeshAsset()
    {
        if (_mesh == null) Rebuild();
        if (_mesh == null) return;
        string path = "Assets/Mid/Meshes/Mesh_" + gameObject.name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(Instantiate(_mesh), path);
        AssetDatabase.SaveAssets();
        Debug.Log("[Mid_LatheMesh] Baked → " + path);
    }
#endif
}
