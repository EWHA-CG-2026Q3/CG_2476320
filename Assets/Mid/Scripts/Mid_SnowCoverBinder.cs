using UnityEngine;

// Pavilion_Roof의 bounds를 읽어 _CoverMin/_CoverMax를 자동 갱신
// C2: 실내 중심·바닥·천장 Y도 자동으로 계산해서 넣어줌
// 정자를 옮겨도 마스크가 따라감 (uniform 수업 내용)
[ExecuteAlways]
public class Mid_SnowCoverBinder : MonoBehaviour
{
    public Renderer   roofRenderer;
    public Renderer   pavilionBodyRenderer; // 정자 본체 (바닥 Y 계산용)
    public Material[] targetMaterials;

    void OnEnable()    => Apply();
    void OnValidate()  => Apply();
#if UNITY_EDITOR
    void Update()      { if (!Application.isPlaying) Apply(); }
#endif
    void FixedUpdate() => Apply();

    void Apply()
    {
        if (roofRenderer == null) return;

        // 지붕 영역 마스크 (M6)
        Bounds roof = roofRenderer.bounds;
        Vector4 mn  = new Vector4(roof.min.x, roof.min.z, 0, 0);
        Vector4 mx  = new Vector4(roof.max.x, roof.max.z, 0, 0);

        // 실내광 좌표 (C2): 지붕·정자 bounds에서 자동 계산
        float floorY  = pavilionBodyRenderer != null
                        ? pavilionBodyRenderer.bounds.min.y + 0.3f
                        : 0.3f;
        float ceilY   = roof.min.y;
        // 실내 중심: 지붕 XZ 중심, 높이 중간
        Vector4 center = new Vector4(
            roof.center.x,
            (floorY + ceilY) * 0.5f,
            roof.center.z,
            0f
        );

        foreach (var m in targetMaterials)
        {
            if (m == null) continue;
            m.SetVector("_CoverMin", mn);
            m.SetVector("_CoverMax", mx);
            m.SetVector("_InteriorCenter", center);
            m.SetFloat("_InteriorFloorY", floorY);
            m.SetFloat("_InteriorCeilY", ceilY);
        }
    }
}
