using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 이 오브젝트를 클릭(터치)하면 이벤트를 발동한다. 클릭한 지점(월드 좌표)과 그 면이 향하는 방향(normal)을 함께 넘긴다.
/// 다른 물체에 가려진 상태에서는 반응하지 않는다.
/// 면에 맞춰 무언가를 붙이는 반응(예: 데칼 찍기)에 쓴다.
/// </summary>
public class Trigger_OnClickSurface : MonoBehaviour
{
    // 클릭한 지점과 그 면의 normal을 넘겨주는 이벤트
    public UnityEvent<Vector3, Vector3> OnClick;

    private Collider myCollider;   // 클릭 판정에 쓰는 이 오브젝트의 Collider

    private void Start()
    {
        myCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        // 포인터(마우스·터치·펜)가 없거나, 이번 프레임에 누르지 않았으면 처리하지 않음
        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        // 포인터의 화면 좌표에서 카메라가 보는 방향으로 광선을 만듦
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());

        // 광선이 처음 맞은 물체가 이 오브젝트일 때만 반응
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (hit.collider != myCollider) return;

        // hit.normal: 광선이 맞은 면이 향하는 방향 (면에 수직, 길이 1)
        OnClick?.Invoke(hit.point, hit.normal);
    }
}