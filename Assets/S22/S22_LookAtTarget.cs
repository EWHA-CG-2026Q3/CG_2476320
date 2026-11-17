using UnityEngine;

// S22 실습 1: 카메라가 경로를 따라 움직이는 동안 한 물체를 계속 바라보게 함
// Camera_Path에 Spline Animate와 함께 붙임 (Spline Animate의 Align To는 None)
public class S22_LookAtTarget : MonoBehaviour
{
    [Tooltip("카메라가 계속 바라볼 물체")]
    public Transform target;

    // LateUpdate는 모든 Update가 끝난 뒤에 호출됨
    // Spline Animate가 이번 프레임의 위치를 옮긴 뒤에 방향을 정하기 위해 LateUpdate를 씀
    void LateUpdate()
    {
        if (target == null)
            return;

        // 카메라의 앞쪽(Z축)이 target을 향하도록 회전함
        // 위쪽은 월드의 위(Y축)를 기준으로 해서 카메라가 옆으로 기울지 않음
        transform.LookAt(target, Vector3.up);
    }
}