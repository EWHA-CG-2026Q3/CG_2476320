using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 2차 베지에 곡선을 따라가는 튜브 메시 생성기
// 수업 근거: 커스텀 메시(S04), 2차 베지에 B(t)=(1-t)²P0+2t(1-t)P1+t²P2 직접 계산,
//           평행 운반법(Parallel Transport)으로 프레임 비틀림 방지
// 기말 B2: De Casteljau 4점 베지에로 교체 예정
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Mid_TubeAlongCurve : MonoBehaviour
{
    [Header("2차 베지에 제어점 (로컬 좌표)")]
    public Vector3 p0 = new Vector3(-0.07f, -0.03f, 0f);
    public Vector3 p1 = new Vector3(-0.09f,  0.03f, 0f);
    public Vector3 p2 = new Vector3(-0.05f,  0.09f, 0f);

    [Header("형태")]
    [Min(2)] public int   steps  = 12;
    [Min(3)] public int   sides  = 6;
    [Min(0.001f)] public float rStart = 0.008f;
    [Min(0.001f)] public float rEnd   = 0.004f;

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
        _mesh = BuildTube(SampleCurve(), rStart, rEnd, sides);
        var mf = GetComponent<MeshFilter>();
        if (mf) mf.sharedMesh = _mesh;
    }

    // 2차 베지에 샘플링 — B(t) = (1-t)²P0 + 2t(1-t)P1 + t²P2
    Vector3[] SampleCurve()
    {
        var pts = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps, t1 = 1f - t;
            pts[i] = t1 * t1 * p0 + 2f * t * t1 * p1 + t * t * p2;
        }
        return pts;
    }

    // ── 정적 메서드: Mid_PlumBranch에서도 재사용 ──────────────────
    // pts: 경로 점 배열, rStart/rEnd: 시작/끝 반지름, sides: 단면 변 수
    public static Mesh BuildTube(Vector3[] pts, float rStart, float rEnd, int sides = 6)
    {
        if (pts == null || pts.Length < 2) return new Mesh();

        var V = new List<Vector3>(); var N = new List<Vector3>();
        var U = new List<Vector2>(); var T = new List<int>();

        // 호 길이 누적
        float[] cumLen = new float[pts.Length];
        for (int i = 1; i < pts.Length; i++)
            cumLen[i] = cumLen[i - 1] + Vector3.Distance(pts[i], pts[i - 1]);
        float totalLen = cumLen[pts.Length - 1]; if (totalLen < 1e-6f) totalLen = 1f;

        // 접선 계산
        var tangents = new Vector3[pts.Length];
        for (int i = 0; i < pts.Length - 1; i++) tangents[i] = (pts[i + 1] - pts[i]).normalized;
        tangents[pts.Length - 1] = tangents[pts.Length - 2];

        // 평행 운반법(Parallel Transport)으로 프레임 비틀림 방지 (S10 행렬 합성 응용)
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(tangents[0], up)) > 0.9f) up = Vector3.right;
        var frames = new Vector3[pts.Length]; // 단면 법선 방향
        frames[0] = Vector3.Cross(Vector3.Cross(tangents[0], up), tangents[0]).normalized;
        for (int i = 1; i < pts.Length; i++)
        {
            Vector3 axis = Vector3.Cross(tangents[i - 1], tangents[i]);
            if (axis.magnitude > 1e-6f)
            {
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(tangents[i - 1], tangents[i]), -1f, 1f));
                frames[i] = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis.normalized) * frames[i - 1];
            }
            else frames[i] = frames[i - 1];
        }

        // 단면 링 생성
        int stride = sides + 1;
        for (int i = 0; i < pts.Length; i++)
        {
            float v     = cumLen[i] / totalLen;
            float r     = Mathf.Lerp(rStart, rEnd, v);
            Vector3 tan = tangents[i];
            Vector3 nrm = frames[i];
            Vector3 bin = Vector3.Cross(tan, nrm).normalized;

            for (int k = 0; k <= sides; k++)
            {
                float u     = k / (float)sides;
                float theta = u * 2f * Mathf.PI;
                float c     = Mathf.Cos(theta), s = Mathf.Sin(theta);
                Vector3 radial = c * nrm + s * bin;
                V.Add(pts[i] + r * radial);
                N.Add(radial);
                U.Add(new Vector2(u, v));
            }
        }

        // 삼각형
        for (int i = 0; i < pts.Length - 1; i++)
            for (int k = 0; k < sides; k++)
            {
                int LB = i * stride + k, RB = LB + 1, LT = LB + stride, RT = LT + 1;
                T.Add(LB); T.Add(LT); T.Add(RT);
                T.Add(LB); T.Add(RT); T.Add(RB);
            }

        var mesh = new Mesh { name = "TubeAlongCurve" };
        if (V.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U);
        mesh.SetTriangles(T, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

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
        Debug.Log("[Mid_TubeAlongCurve] Baked → " + path);
    }
#endif
}
