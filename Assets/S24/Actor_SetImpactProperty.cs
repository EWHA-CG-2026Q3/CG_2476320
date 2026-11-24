using UnityEngine;

/// <summary>
/// 맞은 위치와 그 시각을 셰이더 프로퍼티로 전달한다.
/// Trigger_OnClickPoint(클릭)나 Trigger_OnCollisionPoint(충돌)의 이벤트에 SetImpact를 연결해 쓴다.
/// 셰이더는 이 두 값으로 맞은 지점에서 번지는 충격파 등을 그린다.
/// </summary>
public class Actor_SetImpactProperty : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private string positionProperty = "_ImpactPosition";
    [SerializeField] private string timeProperty = "_ImpactTime";

    private MaterialPropertyBlock block;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();

        // 시작하자마자 링이 보이지 않도록 "아주 오래전에 맞았다"로 설정
        targetRenderer.GetPropertyBlock(block);
        block.SetFloat(timeProperty, -1000f);
        targetRenderer.SetPropertyBlock(block);
    }

    // 맞은 위치와 현재 시각을 셰이더에 전달
    public void Actor_SetImpact(Vector3 worldPoint)
    {
        targetRenderer.GetPropertyBlock(block);
        block.SetVector(positionProperty, worldPoint);
        block.SetFloat(timeProperty, Time.timeSinceLevelLoad);
        targetRenderer.SetPropertyBlock(block);
    }
}