using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 이 오브젝트 위를 누른 채 끌면, 일정한 간격(Spacing)마다 이벤트를 발동한다.
/// 끈 경로 위의 지점(월드 좌표)과 그 면의 normal을 넘기고, 손을 떼면 OnDragEnd를 발동한다.
/// 경로를 따라 무언가를 이어 찍는 반응(예: 물길 그리기)에 쓴다.
/// </summary>
public class Trigger_OnDragSurface : MonoBehaviour
{
    [Tooltip("이벤트를 발동하는 간격 (월드 단위 거리)")]
    public float Spacing = 0.15f;

    // 경로 위의 지점과 그 면의 normal을 넘겨주는 이벤트
    public UnityEvent<Vector3, Vector3> OnDrag;
    // 손을 떼어 한 번의 드래그가 끝났을 때 발동하는 이벤트
    public UnityEvent OnDragEnd;

    private Collider myCollider;   // 판정에 쓰는 이 오브젝트의 Collider
    private Vector3 lastPoint;     // 마지막으로 이벤트를 발동한 지점
    private bool dragging;         // 지금 드래그 중인지

    private void Start()
    {
        myCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (Pointer.current == null) return;

        // 손을 떼면 드래그를 끝냄
        if (Pointer.current.press.wasReleasedThisFrame && dragging)
        {
            dragging = false;
            OnDragEnd?.Invoke();
            return;
        }

        // 누르고 있지 않으면 처리하지 않음
        if (!Pointer.current.press.isPressed) return;

        // 포인터 위치에서 광선을 쏴, 처음 맞은 물체가 이 오브젝트일 때만 처리
        Ray ray = Camera.main.ScreenPointToRay(Pointer.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (hit.collider != myCollider) return;

        // 드래그를 막 시작했거나, 마지막 지점에서 Spacing 이상 움직였을 때만 발동
        if (!dragging || Vector3.Distance(hit.point, lastPoint) >= Spacing)
        {
            dragging = true;
            lastPoint = hit.point;
            OnDrag?.Invoke(hit.point, hit.normal);
        }
    }
}