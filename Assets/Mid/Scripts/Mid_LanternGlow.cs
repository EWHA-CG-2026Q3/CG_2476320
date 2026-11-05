using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// REQ_Lantern.md B3 + C6 — 중간(M10)에서 앞당겨 구현
// S10 uniform 애니메이션: Perlin 노이즈 한 값으로 초롱·실내광·창호지를 함께 흔들어
// "촛불처럼 숨 쉬는" 효과: uniform 하나로 여러 오브젝트 동시 제어 (C6 발표 포인트)
[ExecuteAlways]
public class Mid_LanternGlow : MonoBehaviour
{
    [Header("진폭·속도")]
    [Range(0f, 0.5f)] public float amplitude = 0.15f;  // ±15% (기본값, 사용자 확인)
    public float speed = 0.8f;
    public float seed = 0f;

    [Header("기준 세기")]
    public float baseGlowIntensity      = 0.9f;   // 초롱 _GlowIntensity (C1 기준)
    public float baseInnerLight         = 1.0f;   // InnerLight intensity
    public float baseInteriorLight      = 5.0f;   // Mid_InteriorLight intensity
    public float baseHanjiGlow          = 1.6f;   // 창호지 _GlowIntensity
    public float baseInteriorGlowStrength = 1.0f; // Mid_SnowCover _InteriorGlowStrength
    public float baseWindowSpot          = 10f;  // Mid_WindowSpot_* 기준 세기

    [Header("참조 (OnEnable에서 자동으로 채워짐)")]
    [SerializeField] Renderer lanternBody;
    [SerializeField] Light innerLight;
    [SerializeField] Light interiorLight;
    [SerializeField] Renderer[] hanjiRenderers;
    [SerializeField] Renderer[] interiorGlowRenderers;
    [SerializeField] Light[]    windowSpots;

    MaterialPropertyBlock _mpb;

    void OnEnable()
    {
        _mpb = new MaterialPropertyBlock();
        AutoFind();
    }

    // 씬 오브젝트를 이름으로 찾아 참조 채우기
    void AutoFind()
    {
        var bodyGO = GameObject.Find("Cheongsachorong/Pivot/Body");
        if (bodyGO) lanternBody = bodyGO.GetComponent<Renderer>();

        var innerGO = GameObject.Find("Cheongsachorong/Pivot/Body/InnerLight");
        if (innerGO) innerLight = innerGO.GetComponent<Light>();

        var intGO = GameObject.Find("Mid_InteriorLight");
        if (intGO) interiorLight = intGO.GetComponent<Light>();

        var hanjiRoot = GameObject.Find("HanjiPanel");
        if (hanjiRoot) hanjiRenderers = hanjiRoot.GetComponentsInChildren<Renderer>();

        // Mid_SnowCover 셰이더 + _UseInteriorGlow=1 인 렌더러만 수집
        var allR = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in allR)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && mat.shader != null
                    && mat.shader.name == "CG/Mid_SnowCover"
                    && mat.HasProperty("_UseInteriorGlow")
                    && mat.GetFloat("_UseInteriorGlow") > 0.5f)
                {
                    list.Add(r);
                    break;
                }
            }
        }
        interiorGlowRenderers = list.ToArray();

        // Mid_WindowSpot_* 창살 Spot 조명 수집
        string[] spotNames = { "Mid_WindowSpot_Back", "Mid_WindowSpot_Right", "Mid_WindowSpot_Left" };
        var spots = new System.Collections.Generic.List<Light>();
        foreach (var n in spotNames)
        {
            var go = GameObject.Find(n);
            if (go) spots.Add(go.GetComponent<Light>());
        }
        windowSpots = spots.ToArray();
    }

    void Update()
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        // 에디트 모드에서는 EditorApplication.timeSinceStartup 사용 (S10 uniform 수업 내용)
#if UNITY_EDITOR
        float t = Application.isPlaying ? Time.time : (float)EditorApplication.timeSinceStartup;
#else
        float t = Time.time;
#endif
        // Perlin 노이즈 → -1~1 → ±amplitude 흔들림
        float noise  = Mathf.PerlinNoise(t * speed, seed) * 2f - 1f;
        float factor = 1f + amplitude * noise;  // 예: amplitude=0.15 → 0.85~1.15

        // 초롱 _GlowIntensity (MaterialPropertyBlock: 머티리얼 에셋 수정 없이 인스턴스별 값)
        ApplyMPB(lanternBody, "_GlowIntensity", baseGlowIntensity * factor);
        ApplyMPBAll(hanjiRenderers, "_GlowIntensity", baseHanjiGlow * factor);
        ApplyMPBAll(interiorGlowRenderers, "_InteriorGlowStrength", baseInteriorGlowStrength * factor);

        // 조명 세기 직접 조정
        if (innerLight)    innerLight.intensity    = baseInnerLight    * factor;
        if (interiorLight) interiorLight.intensity  = baseInteriorLight * factor;
        if (windowSpots != null)
            foreach (var s in windowSpots) if (s) s.intensity = baseWindowSpot * factor;
    }

    void ApplyMPB(Renderer r, string prop, float value)
    {
        if (!r) return;
        r.GetPropertyBlock(_mpb);
        _mpb.SetFloat(prop, value);
        r.SetPropertyBlock(_mpb);
    }

    void ApplyMPBAll(Renderer[] rs, string prop, float value)
    {
        if (rs == null) return;
        foreach (var r in rs) ApplyMPB(r, prop, value);
    }
}
