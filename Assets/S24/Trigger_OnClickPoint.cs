using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 이 오브젝트를 마우스로 클릭하거나 터치하면 이벤트를 발동한다. 클릭한 지점(월드 좌표)을 함께 넘긴다.
/// 다른 물체에 가려진 상태에서는 반응하지 않는다.
/// 클릭 지점이 필요한 반응(예: 실드 충격파)과 필요 없는 반응(예: 애니메이션 재생, VFX 실행)
/// 모두 OnClick에 연결해 쓴다.
/// </summary>
public class Trigger_OnClickPoint : MonoBehaviour
{
    // 클릭한 지점(월드 좌표)을 넘겨주는 이벤트
    public UnityEvent<Vector3> OnClick;

    private Collider myCollider;   // 클릭 판정에 쓰는 이 오브젝트의 Collider

    private void Start()
    {
        myCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        // 포인터(마우스·터치·펜)가 없거나, 이번 프레임에 누르지 않았으면 처리하지 않음 (New Input System)
        // 마우스는 왼쪽 버튼, 터치는 손가락을 댄 순간이 press에 해당함
        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        // 포인터의 화면 좌표에서 카메라가 보는 방향으로 광선을 만듦
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());

        // 광선이 처음 맞은 물체를 찾음. 아무것도 맞지 않았으면 처리하지 않음
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

        // 처음 맞은 물체가 이 오브젝트일 때만 반응 (앞에 다른 물체가 있으면 반응하지 않음)
        if (hit.collider != myCollider) return;

        OnClick?.Invoke(hit.point);
    }
}