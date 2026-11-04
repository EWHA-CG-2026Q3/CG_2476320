using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 청사초롱 커스텀 메시 생성기 (REQ_Lantern.md §4)
// [ExecuteAlways] + OnValidate → Inspector 슬라이더 변경 시 즉시 재생성
// 수업 근거: 커스텀 메시(S04 vertices/normals/triangles 직접 채우기),
//           Hierarchy와 행렬 합성(S10 — Pivot 하나 돌리면 자식 전체 따라옴)
[ExecuteAlways]
public class Mid_LanternBuilder : MonoBehaviour
{
    [Header("형태 (다각형)")]
    public int   sides        = 8;      // 단면 변 수 (4/6/8)
    public int   rings        = 10;     // 세로 분할 수
    public float height       = 0.45f;  // 몸통 높이 (m)
    public float radius       = 0.15f;  // 기본 반지름 (m)
    public float bulge        = 0.18f;  // 배부름 (0=원기둥)

    [Header("청사 띠 (UV v 비율)")]
    [Range(0f,0.5f)] public float bandBottom = 0.12f; // 아래 띠 비율
    [Range(0f,0.5f)] public float bandTop    = 0.12f; // 위 띠 비율

    [Header("살")]
    public float ribWidth     = 0.008f; // 살 두께 (m)

    [Header("덮개")]
    public float capOverhang  = 1.25f;  // 덮개 반지름 배율
    public float eaveLift     = 0.03f;  // 추녀 들림 (m)

    [Header("줄 / 술")]
    public float stringLength = 0.35f;  // 줄 길이 (m)
    public int   tasselStrands = 12;    // 술 가닥 수
    public float tasselLength  = 0.18f; // 술 길이 (m)

    [Header("머티리얼")]
    public Material silkMat;
    public Material silkInnerMat;
    public Material frameMat;
    public Material capMat;
    public Material tasselMat;

    // Bake용 메시 참조
    Mesh _bodyMesh, _hookMesh, _stringMesh, _tasselMesh;

    // ───────────────────────────────────────────────────────
    // 라이프사이클
    // ───────────────────────────────────────────────────────
    void OnEnable()   => Rebuild();

    void OnValidate()
    {
        // OnValidate는 직렬화 직후 호출 — GameObject 조작은 한 프레임 뒤로 미룸
#if UNITY_EDITOR
        EditorApplication.delayCall += () => { if (this) Rebuild(); };
#else
        Rebuild();
#endif
    }

    // ───────────────────────────────────────────────────────
    // 재생성 메인
    // ───────────────────────────────────────────────────────
    public void Rebuild()
    {
#if UNITY_EDITOR
        // 프리팹 에셋 자체에서는 실행하지 않음 (씬 인스턴스에서만)
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
#endif
        if (sides < 3) sides = 3;
        if (rings < 1) rings = 1;

        ClearChildren();

        // ── Pivot: 흔들림 회전은 여기서만 (S10 Hierarchy 수업 내용)
        var pivot = Make("Pivot", transform);

        // ── 고리 (토러스)
        var hookGO = MakeMesh("Hook", pivot.transform);
        _hookMesh = BuildTorus(0.02f, 0.004f, 8, 6);
        hookGO.GetComponent<MeshFilter>().sharedMesh = _hookMesh;
        SetMat(hookGO, frameMat);

        // ── 줄 (6각기둥)
        var stringGO = MakeMesh("String", pivot.transform);
        _stringMesh = BuildHexCylinder(0.003f, stringLength);
        stringGO.GetComponent<MeshFilter>().sharedMesh = _stringMesh;
        stringGO.transform.localPosition = new Vector3(0f, -stringLength * 0.5f, 0f);
        SetMat(stringGO, frameMat);

        // ── 몸통 (서브메시 4개)
        var bodyGO = MakeMesh("Body", pivot.transform);
        bodyGO.transform.localPosition = new Vector3(0f, -stringLength, 0f);
        _bodyMesh = BuildBody();
        bodyGO.GetComponent<MeshFilter>().sharedMesh = _bodyMesh;
        var bodyR = bodyGO.GetComponent<MeshRenderer>();
        bodyR.sharedMaterials = new Material[] { silkMat, silkInnerMat, frameMat, capMat };

        // ── 내부 조명 (InnerLight)
        var lightGO = Make("InnerLight", bodyGO.transform);
        var lt = lightGO.AddComponent<Light>();
        lt.type = LightType.Point;
        ColorUtility.TryParseHtmlString("#FEDCBF", out var warmCol);
        lt.color = warmCol;
        lt.intensity = 1.5f;
        lt.range = 4f;
        lt.shadows = LightShadows.None;
        lt.enabled = false; // 씬 배치 시 개별로 켬 (URP 추가 조명 수 제한)

        // ── 술 Pivot: 2차 흔들림 (S10 행렬 합성 수업 내용)
        var tasselPivot = Make("TasselPivot", bodyGO.transform);
        tasselPivot.transform.localPosition = new Vector3(0f, -height * 0.5f, 0f);

        var tasselGO = MakeMesh("Tassel", tasselPivot.transform);
        _tasselMesh = BuildTassel();
        tasselGO.GetComponent<MeshFilter>().sharedMesh = _tasselMesh;
        SetMat(tasselGO, tasselMat);
    }

    // ───────────────────────────────────────────────────────
    // 보조 — 계층 생성
    // ───────────────────────────────────────────────────────
    GameObject Make(string n, Transform parent)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        return go;
    }

    GameObject MakeMesh(string n, Transform parent)
    {
        var go = Make(n, parent);
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        return go;
    }

    void SetMat(GameObject go, Material m)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r && m) r.sharedMaterial = m;
    }

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

    // ───────────────────────────────────────────────────────
    // 반지름 곡선: r(t) = radius*(1 + bulge*sin(π*t))
    // t=0 바닥, t=1 위. 가운데가 가장 넓어서 배부른 실루엣.
    // ───────────────────────────────────────────────────────
    float R(float t)    => radius * (1f + bulge * Mathf.Sin(Mathf.PI * t));
    float dRdt(float t) => radius * bulge * Mathf.PI * Mathf.Cos(Mathf.PI * t);

    // 호 길이 테이블 → v UV (단순 높이 비율 대신 곡선 길이 기준)
    float[] ArcLengths()
    {
        var a = new float[rings + 1];
        for (int j = 1; j <= rings; j++)
        {
            float dr = R(j / (float)rings) - R((j - 1f) / rings);
            float dy = height / rings;
            a[j] = a[j - 1] + Mathf.Sqrt(dr * dr + dy * dy);
        }
        return a;
    }

    // ───────────────────────────────────────────────────────
    // 몸통 메시 — 서브메시 4개
    // ───────────────────────────────────────────────────────
    Mesh BuildBody()
    {
        var verts   = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs     = new List<Vector2>();
        var t0 = new List<int>(); // 바깥 비단
        var t1 = new List<int>(); // 안쪽 비단
        var t2 = new List<int>(); // 살
        var t3 = new List<int>(); // 덮개+받침

        float[] arc = ArcLengths();
        float totalArc = arc[rings] > 0f ? arc[rings] : 1f;

        // ── 서브메시 0·1: 비단 패널 ─────────────────────────
        // 각 변(panel)마다 정점을 따로 만든다 → 패널 사이 모서리 각짐,
        // 패널 내부는 부드러운 법선 (S04 수업 내용: 같은 위치 다른 법선)
        for (int i = 0; i < sides; i++)
        {
            float aL = 2f * Mathf.PI * i / sides;
            float aR = 2f * Mathf.PI * (i + 1) / sides;
            int   bi = verts.Count;

            for (int j = 0; j <= rings; j++)
            {
                float t  = j / (float)rings;
                float r  = R(t);
                float y  = height * t - height * 0.5f;
                float v  = arc[j] / totalArc;      // 호 길이 기준 UV
                float dr = dRdt(t);

                // 왼쪽 모서리
                float cL = Mathf.Cos(aL), sL = Mathf.Sin(aL);
                // 법선: 높이 접선 × 둘레 접선의 외적 (수업 근거: 외적 = 법선)
                Vector3 TvL = new Vector3(dr * cL, 1f, dr * sL); // 높이 방향 접선
                Vector3 ThL = new Vector3(-sL, 0f, cL);           // 둘레 접선
                Vector3 nL  = Vector3.Cross(TvL.normalized, ThL).normalized;
                verts.Add(new Vector3(r * cL, y, r * sL));
                normals.Add(nL);
                uvs.Add(new Vector2(0f, v));

                // 오른쪽 모서리
                float cR = Mathf.Cos(aR), sR = Mathf.Sin(aR);
                Vector3 TvR = new Vector3(dr * cR, 1f, dr * sR);
                Vector3 ThR = new Vector3(-sR, 0f, cR);
                Vector3 nR  = Vector3.Cross(TvR.normalized, ThR).normalized;
                verts.Add(new Vector3(r * cR, y, r * sR));
                normals.Add(nR);
                uvs.Add(new Vector2(1f, v));
            }

            for (int j = 0; j < rings; j++)
            {
                int LB = bi + j * 2, RB = LB + 1, LT = LB + 2, RT = LB + 3;
                // 서브메시 0: CCW → 바깥쪽 앞면
                t0.Add(LB); t0.Add(LT); t0.Add(RT);
                t0.Add(LB); t0.Add(RT); t0.Add(RB);
                // 서브메시 1: 감김 뒤집기 → 안쪽 앞면 (S10 DiamondMesh_Rewinded 참고)
                t1.Add(LB); t1.Add(RT); t1.Add(LT);
                t1.Add(LB); t1.Add(RB); t1.Add(RT);
            }
        }

        // ── 서브메시 2: 살 ─────────────────────────────────
        // 완전 각진 법선(flat shading): 각 사각형마다 정점 분리 → 비단의 부드러운 법선과 대비
        const float eps = 0.003f; // 비단 위로 살짝 돌출
        // 세로 살
        for (int i = 0; i < sides; i++)
        {
            float theta  = 2f * Mathf.PI * i / sides;
            float ct = Mathf.Cos(theta), st = Mathf.Sin(theta);
            float ha = ribWidth * 0.5f / Mathf.Max(R(0.5f), 0.001f); // 각도 반폭
            float aL = theta - ha, aR = theta + ha;

            for (int j = 0; j < rings; j++)
            {
                float t0_ = j / (float)rings, t1_ = (j + 1f) / rings;
                float r0 = R(t0_) + eps, r1 = R(t1_) + eps;
                float y0 = height * t0_ - height * 0.5f;
                float y1 = height * t1_ - height * 0.5f;

                Vector3 BL = new Vector3(r0 * Mathf.Cos(aL), y0, r0 * Mathf.Sin(aL));
                Vector3 BR = new Vector3(r0 * Mathf.Cos(aR), y0, r0 * Mathf.Sin(aR));
                Vector3 TL = new Vector3(r1 * Mathf.Cos(aL), y1, r1 * Mathf.Sin(aL));
                Vector3 TR = new Vector3(r1 * Mathf.Cos(aR), y1, r1 * Mathf.Sin(aR));
                Vector3 fn = new Vector3(ct, 0f, st); // flat normal (수업 근거: 삼각형 법선)

                int b = verts.Count;
                verts.Add(BL); verts.Add(BR); verts.Add(TL); verts.Add(TR);
                for (int k = 0; k < 4; k++) { normals.Add(fn); uvs.Add(Vector2.zero); }
                t2.Add(b); t2.Add(b+2); t2.Add(b+3);
                t2.Add(b); t2.Add(b+3); t2.Add(b+1);
            }
        }

        // 가로 살 4개 (맨 위·아래 + 청사 띠 경계 2곳)
        float[] bandTs = { 0f, bandBottom, 1f - bandTop, 1f };
        float rh = ribWidth * 0.5f;
        foreach (float bt in bandTs)
        {
            float r = R(bt) + eps, y = height * bt - height * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                float a0 = 2f * Mathf.PI * i / sides;
                float a1 = 2f * Mathf.PI * (i + 1) / sides;
                Vector3 fn = new Vector3(Mathf.Cos((a0+a1)*0.5f), 0f, Mathf.Sin((a0+a1)*0.5f));
                Vector3 BL = new Vector3(r*Mathf.Cos(a0), y-rh, r*Mathf.Sin(a0));
                Vector3 BR = new Vector3(r*Mathf.Cos(a1), y-rh, r*Mathf.Sin(a1));
                Vector3 TL = new Vector3(r*Mathf.Cos(a0), y+rh, r*Mathf.Sin(a0));
                Vector3 TR = new Vector3(r*Mathf.Cos(a1), y+rh, r*Mathf.Sin(a1));

                int b = verts.Count;
                verts.Add(BL); verts.Add(BR); verts.Add(TL); verts.Add(TR);
                for (int k = 0; k < 4; k++) { normals.Add(fn); uvs.Add(Vector2.zero); }
                t2.Add(b); t2.Add(b+2); t2.Add(b+3);
                t2.Add(b); t2.Add(b+3); t2.Add(b+1);
            }
        }

        // ── 서브메시 3: 덮개 + 받침 ─────────────────────────
        float capR  = R(1f) * capOverhang;
        float topY  = height * 0.5f;
        float capH  = capR * 0.25f;         // 꼭대기 높이
        int   cSeg  = 4;                    // 변당 분할 수

        // 덮개 윗면 — 추녀 들림 (한옥 처마)
        // w(φ) = pow(|cos(φ*π)|, 2): 꼭짓점(φ=0,1)에서 1, 변 중간(φ=0.5)에서 0
        // (수업 근거: 삼각함수 활용, 조각적 함수로 모서리 강조)
        Vector3 capTop = new Vector3(0f, topY + capH, 0f);
        for (int i = 0; i < sides; i++)
        {
            for (int k = 0; k < cSeg; k++)
            {
                float wt0 = k / (float)cSeg, wt1 = (k + 1f) / cSeg;
                float w0 = Mathf.Pow(Mathf.Abs(Mathf.Cos(wt0 * Mathf.PI)), 2f);
                float w1 = Mathf.Pow(Mathf.Abs(Mathf.Cos(wt1 * Mathf.PI)), 2f);
                float a0 = 2f * Mathf.PI * (i + wt0) / sides;
                float a1 = 2f * Mathf.PI * (i + wt1) / sides;

                Vector3 E0 = new Vector3(capR*Mathf.Cos(a0), topY + eaveLift*w0, capR*Mathf.Sin(a0));
                Vector3 E1 = new Vector3(capR*Mathf.Cos(a1), topY + eaveLift*w1, capR*Mathf.Sin(a1));
                Vector3 fn = Vector3.Cross(capTop - E0, E1 - E0).normalized; // 외적 법선

                int b = verts.Count;
                verts.Add(E0); verts.Add(E1); verts.Add(capTop);
                normals.Add(fn); normals.Add(fn); normals.Add(fn);
                uvs.Add(new Vector2(E0.x, E0.z)); uvs.Add(new Vector2(E1.x, E1.z)); uvs.Add(Vector2.zero);
                t3.Add(b); t3.Add(b+2); t3.Add(b+1); // 위쪽 → CCW from above

                // 덮개 아랫면 (아래에서 보이는 면)
                Vector3 E0d = E0; E0d.y = topY;
                Vector3 E1d = E1; E1d.y = topY;
                Vector3 Cd  = new Vector3(0f, topY, 0f);
                Vector3 fd  = Vector3.Cross(E1d - E0d, Cd - E0d).normalized;
                b = verts.Count;
                verts.Add(E0d); verts.Add(E1d); verts.Add(Cd);
                normals.Add(fd); normals.Add(fd); normals.Add(fd);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); uvs.Add(Vector2.zero);
                t3.Add(b); t3.Add(b+1); t3.Add(b+2); // 아래쪽 → 반전
            }
        }

        // 받침 (몸통 바닥 아래 작은 disk)
        float baseR = R(0f) * capOverhang * 0.75f;
        float bY0 = -height * 0.5f - 0.01f, bY1 = -height * 0.5f + ribWidth;
        for (int i = 0; i < sides; i++)
        {
            float a0 = 2f * Mathf.PI * i / sides, a1 = 2f * Mathf.PI * (i+1) / sides;
            // 윗면
            Vector3 T0 = new Vector3(baseR*Mathf.Cos(a0), bY1, baseR*Mathf.Sin(a0));
            Vector3 T1 = new Vector3(baseR*Mathf.Cos(a1), bY1, baseR*Mathf.Sin(a1));
            Vector3 TC = new Vector3(0f, bY1, 0f);
            int b = verts.Count;
            verts.Add(T0); verts.Add(T1); verts.Add(TC);
            normals.Add(Vector3.up); normals.Add(Vector3.up); normals.Add(Vector3.up);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); uvs.Add(Vector2.zero);
            t3.Add(b); t3.Add(b+2); t3.Add(b+1);
            // 아랫면
            Vector3 B0 = new Vector3(baseR*Mathf.Cos(a0), bY0, baseR*Mathf.Sin(a0));
            Vector3 B1 = new Vector3(baseR*Mathf.Cos(a1), bY0, baseR*Mathf.Sin(a1));
            Vector3 BC = new Vector3(0f, bY0, 0f);
            b = verts.Count;
            verts.Add(B0); verts.Add(B1); verts.Add(BC);
            normals.Add(Vector3.down); normals.Add(Vector3.down); normals.Add(Vector3.down);
            uvs.Add(Vector2.zero); uvs.Add(Vector2.zero); uvs.Add(Vector2.zero);
            t3.Add(b); t3.Add(b+1); t3.Add(b+2);
            // 옆면
            Vector3 fn = new Vector3(Mathf.Cos((a0+a1)*0.5f), 0f, Mathf.Sin((a0+a1)*0.5f));
            b = verts.Count;
            verts.Add(B0); verts.Add(B1); verts.Add(T0); verts.Add(T1);
            for (int k=0;k<4;k++){normals.Add(fn);uvs.Add(Vector2.zero);}
            t3.Add(b); t3.Add(b+2); t3.Add(b+3);
            t3.Add(b); t3.Add(b+3); t3.Add(b+1);
        }

        // ── 메시 조립
        var mesh = new Mesh { name = "LanternBody" };
        if (verts.Count > 65535) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = 4;
        mesh.SetTriangles(t0, 0); // 바깥 비단
        mesh.SetTriangles(t1, 1); // 안쪽 비단
        mesh.SetTriangles(t2, 2); // 살
        mesh.SetTriangles(t3, 3); // 덮개+받침
        mesh.RecalculateBounds();
        return mesh;
    }

    // ───────────────────────────────────────────────────────
    // 고리 — 토러스 매개변수식
    // x(u,v)=(R+r·cos v)·cos u, y(u,v)=r·sin v, z(u,v)=(R+r·cos v)·sin u
    // 법선: (cos v·cos u, sin v, cos v·sin u) (수업 근거: 매개변수 곡면의 법선)
    // ───────────────────────────────────────────────────────
    Mesh BuildTorus(float majorR, float minorR, int uSeg, int vSeg)
    {
        var vs = new List<Vector3>(); var ns = new List<Vector3>();
        var us = new List<Vector2>(); var ts = new List<int>();

        for (int u = 0; u <= uSeg; u++)
        {
            float uf = u / (float)uSeg * 2f * Mathf.PI;
            float cu = Mathf.Cos(uf), su = Mathf.Sin(uf);
            for (int v = 0; v <= vSeg; v++)
            {
                float vf = v / (float)vSeg * 2f * Mathf.PI;
                float cv = Mathf.Cos(vf), sv = Mathf.Sin(vf);
                vs.Add(new Vector3((majorR + minorR*cv)*cu, minorR*sv, (majorR + minorR*cv)*su));
                ns.Add(new Vector3(cv*cu, sv, cv*su));
                us.Add(new Vector2(u/(float)uSeg, v/(float)vSeg));
            }
        }
        int stride = vSeg + 1;
        for (int u = 0; u < uSeg; u++)
            for (int v = 0; v < vSeg; v++)
            {
                int b = u*stride + v;
                ts.Add(b); ts.Add(b+1);        ts.Add(b+stride);
                ts.Add(b+1); ts.Add(b+stride+1); ts.Add(b+stride);
            }

        var m = new Mesh { name = "LanternHook" };
        m.SetVertices(vs); m.SetNormals(ns); m.SetUVs(0, us); m.SetTriangles(ts, 0);
        m.RecalculateBounds();
        return m;
    }

    // ───────────────────────────────────────────────────────
    // 줄 — 6각기둥
    // ───────────────────────────────────────────────────────
    Mesh BuildHexCylinder(float r, float len)
    {
        var vs = new List<Vector3>(); var ns = new List<Vector3>();
        var us = new List<Vector2>(); var ts = new List<int>();
        float hl = len * 0.5f;
        for (int i = 0; i < 6; i++)
        {
            float a0 = 2f*Mathf.PI*i/6f, a1 = 2f*Mathf.PI*(i+1)/6f;
            Vector3 fn = new Vector3(Mathf.Cos((a0+a1)*0.5f), 0f, Mathf.Sin((a0+a1)*0.5f));
            Vector3 BL = new Vector3(r*Mathf.Cos(a0),-hl,r*Mathf.Sin(a0));
            Vector3 BR = new Vector3(r*Mathf.Cos(a1),-hl,r*Mathf.Sin(a1));
            Vector3 TL = new Vector3(r*Mathf.Cos(a0), hl,r*Mathf.Sin(a0));
            Vector3 TR = new Vector3(r*Mathf.Cos(a1), hl,r*Mathf.Sin(a1));
            int b = vs.Count;
            vs.Add(BL); vs.Add(BR); vs.Add(TL); vs.Add(TR);
            for(int k=0;k<4;k++){ns.Add(fn);us.Add(Vector2.zero);}
            ts.Add(b); ts.Add(b+2); ts.Add(b+3);
            ts.Add(b); ts.Add(b+3); ts.Add(b+1);
        }
        var m = new Mesh { name = "LanternString" };
        m.SetVertices(vs); m.SetNormals(ns); m.SetUVs(0, us); m.SetTriangles(ts, 0);
        m.RecalculateBounds();
        return m;
    }

    // ───────────────────────────────────────────────────────
    // 술 — 매듭 구(위도·경도 구) + 가닥들
    // ───────────────────────────────────────────────────────
    Mesh BuildTassel()
    {
        var vs = new List<Vector3>(); var ns = new List<Vector3>();
        var us = new List<Vector2>(); var ts = new List<int>();

        // 매듭 구
        int lat = 6, lon = 8;
        float kr = 0.015f, kOff = -kr;
        for (int la = 0; la <= lat; la++)
        {
            float phi = Mathf.PI * la / lat - Mathf.PI * 0.5f;
            for (int lo = 0; lo <= lon; lo++)
            {
                float th = 2f * Mathf.PI * lo / lon;
                Vector3 n = new Vector3(Mathf.Cos(phi)*Mathf.Cos(th), Mathf.Sin(phi), Mathf.Cos(phi)*Mathf.Sin(th));
                vs.Add(n*kr + Vector3.up*kOff); ns.Add(n);
                us.Add(new Vector2(lo/(float)lon, la/(float)lat));
            }
        }
        int stride = lon + 1;
        for (int la = 0; la < lat; la++)
            for (int lo = 0; lo < lon; lo++)
            {
                int b = la*stride + lo;
                ts.Add(b); ts.Add(b+1); ts.Add(b+stride);
                ts.Add(b+1); ts.Add(b+stride+1); ts.Add(b+stride);
            }

        // 가닥 (각 가닥: 얇은 삼각기둥, 위쪽 좁고 아래 퍼짐)
        float sw = 0.004f, spread = R(0f) * 0.5f;
        for (int i = 0; i < tasselStrands; i++)
        {
            float ang = 2f * Mathf.PI * i / tasselStrands;
            float ca = Mathf.Cos(ang), sa = Mathf.Sin(ang);
            Vector3 outN  = new Vector3(ca, 0f, sa);
            Vector3 sideN = Vector3.Cross(outN, Vector3.up).normalized;

            Vector3 topL = Vector3.up*kOff*2f + outN*sw - sideN*sw*0.5f;
            Vector3 topR = Vector3.up*kOff*2f + outN*sw + sideN*sw*0.5f;
            Vector3 botL = Vector3.down*tasselLength + outN*spread - sideN*sw*0.1f;
            Vector3 botR = Vector3.down*tasselLength + outN*spread + sideN*sw*0.1f;

            int b = vs.Count;
            vs.Add(topL); vs.Add(topR); vs.Add(botL); vs.Add(botR);
            for(int k=0;k<4;k++){ns.Add(outN);us.Add(Vector2.zero);}
            ts.Add(b); ts.Add(b+2); ts.Add(b+3);
            ts.Add(b); ts.Add(b+3); ts.Add(b+1);
        }

        var m = new Mesh { name = "LanternTassel" };
        m.SetVertices(vs); m.SetNormals(ns); m.SetUVs(0, us); m.SetTriangles(ts, 0);
        m.RecalculateBounds();
        return m;
    }

    // ───────────────────────────────────────────────────────
    // 메시 에셋 저장 (컨텍스트 메뉴 "Bake Mesh Assets")
    // 씬 재로드 없이 메시가 유지되도록 .asset으로 저장
    // ───────────────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Bake Mesh Assets")]
    void BakeMeshAssets()
    {
        void Save(Mesh src, string name)
        {
            if (src == null) return;
            string path = "Assets/Mid/Meshes/" + name + ".asset";
            UnityEditor.AssetDatabase.DeleteAsset(path);
            UnityEditor.AssetDatabase.CreateAsset(Instantiate(src), path);
        }
        Save(_bodyMesh,   "Mesh_LanternBody");
        Save(_hookMesh,   "Mesh_LanternHook");
        Save(_stringMesh, "Mesh_LanternString");
        Save(_tasselMesh, "Mesh_LanternTassel");
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("[Mid_LanternBuilder] Meshes baked to Assets/Mid/Meshes/");
    }
#endif
}
