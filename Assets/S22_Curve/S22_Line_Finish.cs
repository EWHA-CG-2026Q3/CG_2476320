using UnityEngine;

// S22 실습 2: 두 물체 A, B 사이의 선분에서 A로부터 전체의 t만큼 간 자리를 구해 표시 물체를 놓음
// A와 B를 잇는 선은 t 지점이 어디 있는지 보여 주는 안내선임
// 편집 모드에서도 작동함: Scene 뷰에서 A, B를 옮기거나 Marker T를 바꾸면 바로 반영됨
[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class S22_Line_Finish : MonoBehaviour
{
    [Header("연결")]
    [Tooltip("선분의 시작 A")]
    public Transform pointA;
    [Tooltip("선분의 끝 B")]
    public Transform pointB;
    [Tooltip("선분 위의 t 지점에 놓을 물체")]
    public Transform marker;

    [Header("선분 위의 t 지점")]
    [Tooltip("A에서 B까지 전체를 1로 볼 때, A로부터 간 비율")]
    [Range(0f, 1f)] public float t = 0.25f;

    [Header("안내선의 모습")]
    public Color lineColor = new Color(0.3f, 0.9f, 1f);
    public float lineWidth = 0.03f;

    LineRenderer line;

    void OnEnable()
    {
        SetupLine();
    }

    void Update()
    {
        if (pointA == null || pointB == null)
            return;
        if (line == null)
            SetupLine();

        Vector3 a = pointA.position;
        Vector3 b = pointB.position;

        // 안내선: 점 목록에 a와 b 두 점을 넣어 직선으로 이음
        line.positionCount = 2;
        line.SetPosition(0, a);
        line.SetPosition(1, b);

        // 선분 ab 위의 t 지점 p를 구하고, Marker를 p에 놓음
        Vector3 p = PointOnSegment(a, b, t);
        if (marker != null)
            marker.position = p;
    }

    // 선분 ab 위의 t 지점 p: p = (1 - t) * a + t * b
    Vector3 PointOnSegment(Vector3 a, Vector3 b, float t)
    {
        Vector3 p = (1 - t) * a + t * b;
        return p;
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
}