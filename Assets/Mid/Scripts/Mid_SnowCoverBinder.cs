using UnityEngine;

// Pavilion_Roof의 bounds를 읽어 _CoverMin/_CoverMax를 자동 갱신
// 정자를 옮겨도 지붕 영역 마스크가 따라감 (uniform 수업 내용)
[ExecuteAlways]
public class Mid_SnowCoverBinder : MonoBehaviour
{
    public Renderer    roofRenderer;
    public Material[]  targetMaterials;

    void OnEnable()    => Apply();
    void OnValidate()  => Apply();
#if UNITY_EDITOR
    void Update()      { if (!Application.isPlaying) Apply(); }
#endif
    void FixedUpdate() => Apply();

    void Apply()
    {
        if (roofRenderer == null) return;
        Bounds b   = roofRenderer.bounds;
        Vector4 mn = new Vector4(b.min.x, b.min.z, 0, 0);
        Vector4 mx = new Vector4(b.max.x, b.max.z, 0, 0);
        foreach (var m in targetMaterials)
        {
            if (m == null) continue;
            m.SetVector("_CoverMin", mn);
            m.SetVector("_CoverMax", mx);
        }
    }
}
