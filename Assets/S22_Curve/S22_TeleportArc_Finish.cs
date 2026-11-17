using UnityEngine;
using UnityEngine.InputSystem;

// S22 실습 2: 제어점 3개로 De Casteljau를 계산해 텔레포트 호를 그림
// T를 누르고 있으면 호가 보이고, T를 누른 채 왼쪽 클릭하면 호의 끝으로 순간이동함
// 카메라를 내려다볼수록 가까이, 수평에 가까울수록 멀리 착지함
[RequireComponent(typeof(LineRenderer))]
public class S22_TeleportArc_Finish : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("순간이동할 씬 워커의 몸통")]
    public Transform walker;
    [Tooltip("씬 워커의 카메라 (눈 위치와 바라보는 방향)")]
    public Transform view;
    [Tooltip("바닥으로 인정할 레이어")]
    public LayerMask groundMask = ~0;

    [Header("호의 시작점 (손 위치)")]
    [Tooltip("카메라에서 손까지의 거리: x = 오른쪽, y = 위, z = 앞 (수평 기준)")]
    public Vector3 handOffset = new Vector3(0.25f, -0.35f, 0.3f);

    [Header("카메라 높이 각도 → 착지 거리")]
    [Tooltip("이 각도 이하로 내려다보면 가장 가까이 착지 (음수 = 아래)")]
    public float minPitch = -50f;
    [Tooltip("이 각도 이상으로 들면 가장 멀리 착지")]
    public float maxPitch = 10f;
    [Tooltip("가장 가까운 착지 거리 (수평)")]
    public float minDistance = 1.5f;
    [Tooltip("가장 먼 착지 거리 (수평)")]
    public float maxDistance = 10f;

    [Header("호의 모양")]
    [Tooltip("P1의 높이 = 착지 거리 × 이 비율")]
    public float arcHeightRatio = 0.3f;
    [Tooltip("호를 나누는 수 (점의 개수 = segments + 1)")]
    [Range(2, 50)] public int segments = 20;

    [Header("호의 모습")]
    public Color arcColor = new Color(0.3f, 0.9f, 1f);
    public float lineWidth = 0.04f;

    [Header("층 보기 (Gizmos)")]
    [Range(0f, 1f)] public float previewT = 0.5f;

    LineRenderer line;
    Vector3 p0, p1, p2;      // 제어점 3개
    bool hasTarget;          // 바닥을 찾았는가

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.enabled = false;

        // 재질이 비어 있으면 분홍색으로 보이므로, URP Unlit 재질을 만들어 넣음
        if (line.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.SetColor("_BaseColor", arcColor);   // URP Unlit의 색
            mat.color = arcColor;                    // Sprites/Default의 색
            line.sharedMaterial = mat;
        }

        line.startColor = arcColor;
        line.endColor = arcColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null || view == null)
            return;

        bool aiming = Keyboard.current.tKey.isPressed;

        if (aiming)
            hasTarget = FindControlPoints();

        line.enabled = aiming && hasTarget;
        if (line.enabled)
            DrawArc();

        if (line.enabled && Mouse.current.leftButton.wasPressedThisFrame)
            Teleport(p2);
    }

    // 제어점 P0, P1, P2를 정함. 바닥을 못 찾으면 false
    bool FindControlPoints()
    {
        // 바라보는 방향에서 위아래 성분을 뺀 수평 방향
        Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            return false;               // 바로 위나 아래를 보고 있으면 방향이 없음
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        // 카메라의 높이 각도 (수평 = 0°, 위 = +, 아래 = −)
        float pitch = Mathf.Asin(Mathf.Clamp(view.forward.y, -1f, 1f)) * Mathf.Rad2Deg;

        // 높이 각도를 착지 거리로 바꿈
        // InverseLerp: pitch가 minPitch와 maxPitch 사이의 어느 비율에 있는지 (0~1, 범위 밖은 0 또는 1)
        // Lerp: 그 비율만큼 minDistance에서 maxDistance 쪽으로 간 거리
        float k = Mathf.InverseLerp(minPitch, maxPitch, pitch);
        float distance = Mathf.Lerp(minDistance, maxDistance, k);

        // P0: 카메라에서 handOffset만큼 옮긴 손 위치
        p0 = view.position
           + right * handOffset.x
           + Vector3.up * handOffset.y
           + forward * handOffset.z;

        // P2: 앞쪽 distance 지점에서 아래로 Raycast를 쏴서 닿은 바닥
        Vector3 probe = p0 + forward * distance + Vector3.up * 0.5f;
        if (!Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 20f, groundMask))
            return false;
        p2 = hit.point;

        // P1: P0과 P2의 가운데에서 위로 (거리에 비례한 높이)
        p1 = (p0 + p2) * 0.5f + Vector3.up * (distance * arcHeightRatio);
        return true;
    }

    // t를 0부터 1까지 segments개로 나눠 곡선 위의 점을 구하고 Line Renderer로 이음
    void DrawArc()
    {
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            line.SetPosition(i, DeCasteljau(p0, p1, p2, t));
        }
    }

    // 제어점 3개의 De Casteljau
    // 첫째 차례: 선분 P0P1, 선분 P1P2에서 t 지점 찍기 → 점 2개
    // 둘째 차례: 그 두 점을 잇는 선분에서 t 지점 찍기 → 곡선 위의 점
    Vector3 DeCasteljau(Vector3 a0, Vector3 a1, Vector3 a2, float t)
    {
        Vector3 a = PointOnSegment(a0, a1, t);
        Vector3 b = PointOnSegment(a1, a2, t);
        return PointOnSegment(a, b, t);
    }

    // 선분 ab 위의 t 지점: P = (1 - t)·a + t·b
    Vector3 PointOnSegment(Vector3 a, Vector3 b, float t)
    {
        return (1 - t) * a + t * b;
    }

    // 씬 워커를 target으로 옮김
    void Teleport(Vector3 target)
    {
        Transform body = walker != null ? walker : transform;

        // Character Controller는 켜져 있는 동안 위치를 직접 바꾸면 무시될 수 있어서 잠깐 끔
        CharacterController cc = body.GetComponent<CharacterController>();
        Rigidbody rb = body.GetComponent<Rigidbody>();

        Vector3 newPos = target + Vector3.up * 0.05f;
        if (cc != null)
        {
            // 캡슐의 바닥이 target에 닿도록, 기준점과 캡슐 바닥의 높이 차이만큼 올림
            newPos.y -= cc.center.y - cc.height * 0.5f;
            cc.enabled = false;
            body.position = newPos;
            cc.enabled = true;
        }
        else if (rb != null)
        {
            rb.position = newPos;
            rb.linearVelocity = Vector3.zero;
        }
        else
        {
            body.position = newPos;
        }
    }

    // 층 보기: Gizmos로 제어점과 previewT에서의 첫째, 둘째 차례 점을 그림
    void OnDrawGizmos()
    {
        if (!hasTarget)
            return;

        // 제어점과 그 사이 선분
        Gizmos.color = Color.gray;
        Gizmos.DrawSphere(p0, 0.08f);
        Gizmos.DrawSphere(p1, 0.08f);
        Gizmos.DrawSphere(p2, 0.08f);
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p1, p2);

        // 첫째 차례
        Vector3 a = PointOnSegment(p0, p1, previewT);
        Vector3 b = PointOnSegment(p1, p2, previewT);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(a, 0.07f);
        Gizmos.DrawSphere(b, 0.07f);
        Gizmos.DrawLine(a, b);

        // 둘째 차례 = 곡선 위의 점
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(PointOnSegment(a, b, previewT), 0.1f);
    }
}