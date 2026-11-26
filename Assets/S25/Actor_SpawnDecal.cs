using UnityEngine;

/// <summary>
/// 받은 지점에 데칼 프리팹을 만들어, 그 면을 향하도록 세운다.
/// Trigger_OnClickSurface의 OnClick, Trigger_OnDragSurface의 OnDrag에 SpawnAt을 연결해 쓴다.
/// AlignToPath를 켜면, 이어 찍을 때 데칼의 위쪽(UV의 v 방향)을 진행 방향에 맞춘다 (물길이 흐르는 방향).
/// </summary>
public class Actor_SpawnDecal : MonoBehaviour
{
    [Tooltip("찍을 Decal Projector 프리팹 (Pivot은 (0, 0, 0)으로 둘 것)")]
    public GameObject DecalPrefab;

    [Tooltip("켜면 이어 찍을 때 데칼을 진행 방향에 맞춰 돌림")]
    public bool AlignToPath = false;

    private Vector3 lastPoint;     // 직전에 찍은 지점
    private bool hasLast;          // 직전에 찍은 지점이 있는지 (한 번의 드래그 안에서)

    // 지점(point)에, 면의 normal에 맞춰 데칼을 하나 찍음
    public void SpawnAt(Vector3 point, Vector3 normal)
    {
        // 데칼의 위쪽 방향 정하기
        // 기본: 월드 위쪽. 단, 바닥처럼 면이 위를 향하면 normal과 겹치므로 월드 앞쪽을 씀
        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;

        // AlignToPath가 켜져 있고 직전 지점이 있으면, 직전 지점에서 지금 지점으로 가는 방향을 위쪽으로 씀
        if (AlignToPath && hasLast)
        {
            up = point - lastPoint;
        }

        // Decal Projector는 자기 앞쪽(z축)으로 찍으므로, 앞쪽이 면 안쪽(−normal)을 향하게 세움
        Quaternion rotation = Quaternion.LookRotation(-normal, up);
        Instantiate(DecalPrefab, point, rotation);

        lastPoint = point;
        hasLast = true;
    }

    // 한 번의 드래그가 끝나면 직전 지점을 잊음 (다음 드래그는 새로 시작)
    public void EndPath()
    {
        hasLast = false;
    }
}