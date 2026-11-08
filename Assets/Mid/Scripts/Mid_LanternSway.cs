using UnityEngine;

// Mid_LanternSway: 청사초롱 진자 흔들림 (회전 변환 + Hierarchy)
// 수업 근거: S08~S09 회전 변환, S10~S12 계층 구조와 행렬 합성
[ExecuteAlways]
public class Mid_LanternSway : MonoBehaviour
{
    [Header("진자")]
    [Tooltip("줄 길이 L (m) — Mid_LanternBuilder.stringLength 와 맞출 것")]
    public float stringLength = 0.35f;
    [Tooltip("최대 흔들림 각도 A (도)")]
    public float amplitude = 25f;
    [Tooltip("초기 위상 φ — 초롱마다 다르게")]
    public float phase = 0f;

    [Header("바람 노이즈")]
    public float windSpeed = 0.3f;
    [Tooltip("바람에 의한 진폭 변조폭 (도)")]
    public float windAmplitude = 0.8f;
    [Tooltip("Perlin 시드 — 초롱마다 다르게")]
    public float seed = 0f;

    [Header("술(Tassel) 지연")]
    [Tooltip("배율 k")]
    public float tasselK = 1.3f;
    [Tooltip("지연 Δ (초)")]
    public float tasselDelay = 0.25f;

    Transform _tasselPivot;

    void OnEnable() => FindTassel();

#if UNITY_EDITOR
    // Edit 모드에서 SceneView 연속 갱신 → [ExecuteAlways] Update가 매 프레임 호출됨
    void OnDrawGizmos() => UnityEditor.SceneView.RepaintAll();
#endif

    void FindTassel()
    {
        var body = transform.Find("Body");
        if (body != null) _tasselPivot = body.Find("TasselPivot");
    }

    // θ(t) = A·sin(ωt + φ) + 바람 노이즈
    // ω = √(g / L): 물리 진자 각주파수 (줄이 길수록 천천히 흔들림)
    float ComputeTheta(float t)
    {
        float omega = Mathf.Sqrt(9.81f / Mathf.Max(stringLength, 0.01f));
        float wind = (Mathf.PerlinNoise(t * windSpeed, seed) - 0.5f) * windAmplitude;
        return amplitude * Mathf.Sin(omega * t + phase) + wind;
    }

    void Update()
    {
#if UNITY_EDITOR
        float t = Application.isPlaying
            ? Time.time
            : (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
        float t = Time.time;
#endif

        float theta = ComputeTheta(t);

        // 회전 변환 + Hierarchy:
        // Pivot.localRotation 하나만 설정하면 자식(줄·몸통·술) 전체가 행렬 합성으로 따라옴
        transform.localRotation = Quaternion.AngleAxis(theta, Vector3.right);

        // 술의 2차 흔들림: θ_tassel(t) = k · θ(t − Δ)
        // 부모(Pivot) 회전 위에 자식(TasselPivot) 회전이 합성되는 예시 (행렬 합성)
        if (_tasselPivot == null) FindTassel();
        if (_tasselPivot != null)
        {
            float delayed = ComputeTheta(t - tasselDelay);
            _tasselPivot.localRotation = Quaternion.AngleAxis(tasselK * delayed, Vector3.right);
        }
    }
}
