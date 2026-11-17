using UnityEngine;

// S22 실습 3: 제어점 목록(A, 안내 점들, B)으로 De Casteljau를 계산해 곡선을 그림
// 안내 점을 목록에 더 넣으면, 점이 하나 남을 때까지 줄이는 횟수가 하나씩 늘어남
// 편집 모드에서도 작동함: Scene 뷰에서 점을 옮기면 곡선이 바로 따라옴
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class S22_Curve_Finish : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("제어점: 첫 칸과 마지막 칸은 양 끝(A, B), 그 사이 칸은 안내 점(G)")]
    public Transform[] points;
    [Tooltip("곡선 위의 t 지점 P에 놓을 물체")]
    public Transform marker;

    [Header("곡선 위의 t 지점")]
    [Range(0f, 1f)] public float t = 0.5f;

    [Header("곡선 그리기")]
    [Tooltip("t를 0부터 1까지 몇 번으로 나눌지 (점의 개수 = segments + 1)")]
    [Range(1, 100)] public int segments = 32;
    public Color lineColor = new Color(0.3f, 0.9f, 1f);
    public float lineWidth = 0.03f;

    [Header("차례 보기 (Gizmos)")]
    public bool showSteps = true;
    [Tooltip("차례 보기 선의 굵기 (픽셀)")]
    [Range(1f, 10f)] public float stepLineWidth = 4f;
    [Tooltip("차례 보기 점의 크기 (화면 기준)")]
    [Range(0.02f, 0.2f)] public float stepPointSize = 0.06f;

    LineRenderer line;

    void OnEnable()
    {
        SetupLine();
    }

    void Update()
    {
        if (line == null)
            SetupLine();

        if (!ReadPoints(out Vector3[] ctrl))
        {
            line.positionCount = 0;
            return;
        }

        // 곡선: t를 0부터 1까지 segments번으로 나눠 곡선 위의 점을 구하고 이음
        line.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float ti = (float)i / segments;            // i번째 점의 t
            line.SetPosition(i, DeCasteljau(ctrl, ti));
        }

        // 곡선 위의 t 지점 p를 구하고, Marker를 p에 놓음
        Vector3 p = DeCasteljau(ctrl, t);
        if (marker != null)
            marker.position = p;
    }

    // De Casteljau: 이웃한 점을 잇는 선분의 t 지점 찍기를, 점이 하나 남을 때까지 되풀이함
    Vector3 DeCasteljau(Vector3[] ctrl, float t)
    {
        Vector3[] q = (Vector3[])ctrl.Clone();          // 계산용 복사본 (제어점 자체는 바꾸지 않음)

        // 한 차례마다 점이 하나 줄어듦: count개 → count - 1개
        for (int count = q.Length; count > 1; count--)
        {
            // 이웃한 두 점 q[i], q[i + 1]을 잇는 선분의 t 지점을 q[i] 자리에 넣음
            for (int i = 0; i < count - 1; i++)
                q[i] = PointOnSegment(q[i], q[i + 1], t);
        }

        return q[0];                                    // 하나 남은 점 = 곡선 위의 점
    }

    // 선분 ab의 t 지점 p: a에서 b까지의 벡터의 t만큼을 a에 더함
    Vector3 PointOnSegment(Vector3 a, Vector3 b, float t)
    {
        Vector3 p = a + t * (b - a);
        return p;
    }

    // 목록의 물체 위치를 읽음. 점이 2개보다 적거나 빈 칸이 있으면 false
    bool ReadPoints(out Vector3[] ctrl)
    {
        ctrl = null;
        if (points == null || points.Length < 2)
            return false;

        ctrl = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null)
                return false;
            ctrl[i] = points[i].position;
        }
        return true;
    }

    // Line Renderer 준비: 재질이 비어 있으면 분홍색으로 보이므로 URP Unlit 재질을 만들어 넣음
    void SetupLine()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;

        if (line.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;   // 씬 파일에 저장하지 않음 (열 때마다 다시 만듦)
            mat.SetColor("_BaseColor", lineColor);
            mat.color = lineColor;
            line.sharedMaterial = mat;
        }

        line.startColor = lineColor;
        line.endColor = lineColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
    }

#if UNITY_EDITOR
    // 차례 보기: 제어점(회색)과, t에서 차례마다 찍은 점과 선분을 그림. 마지막 점 P는 빨간색
    // 차례를 보여 주는 표시용 코드이며, 읽지 않아도 됨 (Unity 에디터에서만 작동)
    void OnDrawGizmos()
    {
        if (!showSteps || !ReadPoints(out Vector3[] ctrl))
            return;

        // 다른 물체에 가려지지 않고 늘 맨 앞에 그림
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        Vector3[] q = (Vector3[])ctrl.Clone();
        DrawStep(q, q.Length, Color.gray, 1f);

        Color[] colors =
        {
            new Color(0.17f, 0.54f, 0.24f),   // 첫째 차례: 초록 (Q)
            new Color(0.44f, 0.28f, 0.91f),   // 둘째 차례: 보라 (R)
            new Color(1f, 0.6f, 0.1f),        // 셋째 차례: 주황
            Color.cyan                        // 넷째 차례: 하늘색
        };

        int step = 0;
        for (int count = q.Length; count > 1; count--)
        {
            for (int i = 0; i < count - 1; i++)
                q[i] = PointOnSegment(q[i], q[i + 1], t);

            bool last = (count - 1 == 1);
            Color c = last ? Color.red : colors[step % colors.Length];
            DrawStep(q, count - 1, c, last ? 1.6f : 1f);
            step++;
        }
    }

    // 점 n개와, 그 점들을 차례로 이은 선분을 그림
    void DrawStep(Vector3[] q, int n, Color c, float sizeScale)
    {
        UnityEditor.Handles.color = c;

        if (n >= 2)
        {
            Vector3[] pts = new Vector3[n];
            System.Array.Copy(q, pts, n);
            UnityEditor.Handles.DrawAAPolyLine(stepLineWidth, pts);   // 굵은 선
        }

        for (int i = 0; i < n; i++)
        {
            // 화면에서 늘 같은 크기로 보이도록, 카메라와의 거리에 맞춰 크기를 정함
            float size = UnityEditor.HandleUtility.GetHandleSize(q[i]) * stepPointSize * sizeScale;
            UnityEditor.Handles.SphereHandleCap(0, q[i], Quaternion.identity, size, EventType.Repaint);
        }
    }
#endif
}