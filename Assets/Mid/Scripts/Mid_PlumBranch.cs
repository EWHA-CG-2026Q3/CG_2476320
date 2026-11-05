using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 매화 가지 + 꽃 메시 생성기
// 수업 근거: 커스텀 메시(S04), 계층구조(S10 — 꽃 자식 오브젝트),
//           Mid_TubeAlongCurve.BuildTube 재사용, 시드 고정 Random으로 재현 가능한 배치
[ExecuteAlways]
public class Mid_PlumBranch : MonoBehaviour
{
    [Header("가지")]
    [Min(2)] public int   segments     = 4;    // 메인 가지 마디 수
    public float          segmentLen   = 0.04f; // 마디 길이 (m)
    public float          rBase        = 0.012f; // 뿌리 반지름 (m)
    public float          rTip         = 0.003f; // 끝 반지름 (m)
    [Range(0f, 60f)]
    public float          bendAngle    = 25f;   // 마디당 최대 꺾임 각도
    [Min(0)] public int   sideBranches = 2;

    [Header("꽃")]
    [Range(1, 20)] public int flowerCount  = 10;
    public float              flowerRadius = 0.012f; // 꽃 반지름 (m)
    public int                seed         = 42;

    [Header("머티리얼")]
    public Material branchMat;
    public Material flowerMat;

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
        ClearChildren();

        var rng = new System.Random(seed);

        // 꽃 메시는 공유
        var fMesh = BuildFlowerMesh(flowerRadius);

        // ── 메인 가지 경로 (로컬 좌표, 위로 자람)
        var mainPath = GeneratePath(Vector3.zero, Vector3.up, segments, segmentLen, bendAngle, rng);

        // ── 가지 메시 (메인 + 곁가지를 하나로 합침)
        var allV = new List<Vector3>(); var allN = new List<Vector3>();
        var allU = new List<Vector2>(); var allT = new List<int>();
        var flowerPos = new List<Vector3>();

        AppendTube(allV, allN, allU, allT, mainPath, rBase, rTip);
        flowerPos.Add(mainPath[mainPath.Count - 1]);

        // 곁가지
        for (int b = 0; b < sideBranches; b++)
        {
            int si = Mathf.Clamp(1 + b * Mathf.Max(1, mainPath.Count / (sideBranches + 1)), 1, mainPath.Count - 2);
            Vector3 dir = (mainPath[si] - mainPath[si - 1]).normalized;
            var rot = Quaternion.Euler((float)rng.NextDouble() * 50f - 25f,
                                       (float)rng.NextDouble() * 90f - 45f, 0f);
            var subPath = GeneratePath(mainPath[si], rot * dir,
                Mathf.Max(2, segments - 1), segmentLen * 0.7f, bendAngle * 1.3f, rng);

            float subR = Mathf.Lerp(rBase, rTip, (float)si / mainPath.Count);
            AppendTube(allV, allN, allU, allT, subPath, subR, rTip * 0.5f);
            flowerPos.Add(subPath[subPath.Count - 1]);
        }

        // 나머지 꽃은 가지 위 랜덤 위치
        for (int f = flowerPos.Count; f < flowerCount; f++)
        {
            int idx = 1 + rng.Next(mainPath.Count - 1);
            flowerPos.Add(mainPath[idx] + RandDir(rng) * segmentLen * 0.25f);
        }

        // ── 가지 오브젝트
        var bGO = MakeChild("Branches");
        var bm  = new Mesh { name = "PlumBranches" };
        if (allV.Count > 65535) bm.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        bm.SetVertices(allV); bm.SetNormals(allN); bm.SetUVs(0, allU);
        bm.SetTriangles(allT, 0); bm.RecalculateBounds();
        bGO.AddComponent<MeshFilter>().sharedMesh = bm;
        var br = bGO.AddComponent<MeshRenderer>();
        if (branchMat) br.sharedMaterial = branchMat;

        // ── 꽃 오브젝트
        foreach (var pos in flowerPos)
        {
            var fGO = MakeChild("Flower");
            fGO.transform.localPosition = pos;
            fGO.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, 0f);
            fGO.AddComponent<MeshFilter>().sharedMesh = fMesh;
            var fr = fGO.AddComponent<MeshRenderer>();
            if (flowerMat) fr.sharedMaterial = flowerMat;
        }
    }

    // ── 가지 경로 생성 ─────────────────────────────────────────
    List<Vector3> GeneratePath(Vector3 start, Vector3 dir, int segs,
        float len, float maxBend, System.Random rng)
    {
        var path = new List<Vector3> { start };
        Vector3 d = dir.normalized;
        for (int i = 0; i < segs; i++)
        {
            path.Add(path[path.Count - 1] + d * len);
            var rot = Quaternion.Euler((float)(rng.NextDouble() * 2 - 1) * maxBend,
                                       (float)(rng.NextDouble() * 2 - 1) * maxBend * 0.5f, 0f);
            d = (rot * d).normalized;
        }
        return path;
    }

    // 경로를 튜브로 변환해 combined mesh 버퍼에 추가
    // Mid_TubeAlongCurve.BuildTube 재사용 (5-1)
    void AppendTube(List<Vector3> V, List<Vector3> N, List<Vector2> U, List<int> T,
        List<Vector3> path, float rStart, float rEnd)
    {
        if (path.Count < 2) return;
        int baseIdx = V.Count;
        var tube = Mid_TubeAlongCurve.BuildTube(path.ToArray(), rStart, rEnd, 5);

        var tv = new List<Vector3>(); var tn = new List<Vector3>();
        var tu = new List<Vector2>(); var tt = new List<int>();
        tube.GetVertices(tv); tube.GetNormals(tn); tube.GetUVs(0, tu); tube.GetIndices(tt, 0);

        V.AddRange(tv); N.AddRange(tn); U.AddRange(tu);
        foreach (int idx in tt) T.Add(baseIdx + idx);
    }

    // ── 꽃잎 5장 메시 ─────────────────────────────────────────
    // XZ 평면에 5개 꽃잎, 각 꽃잎 = 중심점 fan (sin 커브로 반지름 조절)
    // 수업 근거: 커스텀 메시(S04), 삼각함수 활용
    Mesh BuildFlowerMesh(float r)
    {
        const int petals  = 5;
        const int fanSegs = 4; // 꽃잎당 부채꼴 분할
        float halfW = Mathf.PI * 0.55f / petals; // 꽃잎 반각폭

        var V = new List<Vector3>(); var N = new List<Vector3>();
        var U = new List<Vector2>(); var T = new List<int>();

        // 중심점 (인덱스 0)
        V.Add(Vector3.zero); N.Add(Vector3.up); U.Add(Vector2.one * 0.5f);

        for (int p = 0; p < petals; p++)
        {
            float mid = p / (float)petals * 2f * Mathf.PI;
            int rimBase = V.Count;

            for (int k = 0; k <= fanSegs; k++)
            {
                float t   = k / (float)fanSegs; // 0→1 (꽃잎 왼쪽→오른쪽)
                float ang = mid + (t - 0.5f) * 2f * halfW;
                // sin으로 꽃잎 모양: 가운데(t=0.5)에서 최대, 양 끝에서 0에 가까움
                float rr  = Mathf.Max(r * 0.05f, r * Mathf.Sin(t * Mathf.PI));
                V.Add(new Vector3(rr * Mathf.Cos(ang), 0f, rr * Mathf.Sin(ang)));
                N.Add(Vector3.up);
                U.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(ang), 0.5f + 0.5f * Mathf.Sin(ang)));
            }

            for (int k = 0; k < fanSegs; k++)
            {
                // 앞면 (위에서 보이는 면, CCW from above)
                T.Add(0); T.Add(rimBase + k); T.Add(rimBase + k + 1);
                // 뒷면 (아래에서도 보이도록)
                T.Add(0); T.Add(rimBase + k + 1); T.Add(rimBase + k);
            }
        }

        // 꽃술: 중앙에 작은 원뿔 (선택 — 색 차이를 위해)
        // → 머티리얼 색상 차이만으로 충분, 생략

        var mesh = new Mesh { name = "PlumFlower" };
        mesh.SetVertices(V); mesh.SetNormals(N); mesh.SetUVs(0, U);
        mesh.SetTriangles(T, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── 유틸 ───────────────────────────────────────────────────
    GameObject MakeChild(string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, false);
        return go;
    }

    static Vector3 RandDir(System.Random rng) =>
        new Vector3((float)rng.NextDouble() * 2 - 1,
                    (float)rng.NextDouble() * 2 - 1,
                    (float)rng.NextDouble() * 2 - 1).normalized;

    void ClearChildren()
    {
        var list = new List<Transform>();
        foreach (Transform t in transform) list.Add(t);
        foreach (var t in list)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(t.gameObject);
            else Destroy(t.gameObject);
#else
            Destroy(t.gameObject);
#endif
        }
    }
}
