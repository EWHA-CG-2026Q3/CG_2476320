// DiamondMesh_Faceted.cs
// 씬: S19의 모든 씬 / 오브젝트: 면을 나눈 다이아몬드
// S19: DiamondMesh_Rewinded와 같은 정점, 같은 삼각형에서 출발해
//      면마다 정점을 따로 두어(6 → 24) normal과 UV 좌표를 면마다 갖게 한 버전.
// 이 스크립트는 메시를 만들고 보관하는 역할만 함 — 이동/회전/스케일 계산은 별도 스크립트가 담당.
// 머티리얼이 비어 있거나 분홍색(셰이더 오류)이면 URP Lit 머티리얼을 자동으로 붙임.
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class S19_DiamondMesh_Faceted : MonoBehaviour
{
    [Header("다이아몬드 원본 정점 (0=아래 꼭짓점, 1~4=중간 사각형, 5=위 꼭짓점)")]
    [SerializeField]
    Vector3[] baseVertices = new Vector3[]
    {
        new Vector3(0.5f, 0f,   0.5f), // 0 — 아래 꼭짓점
        new Vector3(0f,   0.5f, 0f),   // 1 — 중간 사각형
        new Vector3(1f,   0.5f, 0f),   // 2
        new Vector3(1f,   0.5f, 1f),   // 3
        new Vector3(0f,   0.5f, 1f),   // 4
        new Vector3(0.5f, 1f,   0.5f), // 5 — 위 꼭짓점
    };

    // DiamondMesh_Rewinded와 같은 삼각형 (면마다 정점 번호 3개)
    // 바깥에서 봤을 때 시계 방향 = 앞면 (Unity 규칙)
    static readonly int[] triangles = new int[]
    {
        0,1,2, 0,2,3, 0,3,4, 0,4,1,   // 아래 사각뿔 4면
        5,2,1, 5,3,2, 5,4,3, 5,1,4,   // 위 사각뿔 4면
    };

    Mesh mesh;

    public Vector3[] BaseVertices => baseVertices;

    void OnEnable()
    {
        EnsureLitMaterial();

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        SetVertices(baseVertices);
    }

    public void SetVertices(Vector3[] v)
    {
        // 여기부터 DiamondMesh_Rewinded와 다름

        // 정점: 삼각형 배열의 번호를 그 정점으로 바꿔 차례로 적음 (면마다 3개, 모두 24개)
        //       0,1,2 → v[0], v[1], v[2]
        Vector3[] facetedVertices =
        {
            v[0], v[1], v[2],   v[0], v[2], v[3],   v[0], v[3], v[4],   v[0], v[4], v[1],   // 아래 4면
            v[5], v[2], v[1],   v[5], v[3], v[2],   v[5], v[4], v[3],   v[5], v[1], v[4],   // 위 4면
        };

        // 삼각형: 정점을 면마다 따로 적었으므로, 적은 순서대로
        int[] facetedTriangles =
        {
             0,  1,  2,    3,  4,  5,    6,  7,  8,    9, 10, 11,   // 아래 4면
            12, 13, 14,   15, 16, 17,   18, 19, 20,   21, 22, 23,   // 위 4면
        };

        // UV 좌표: 면 하나씩 텍스처에 대응시킴
        Vector2 uvBottomCenter = new Vector2(0.5f, 0);   // 텍스처의 아래 가운데를 가리키는 UV 좌표
        Vector2 uvTopCenter = new Vector2(0.5f, 1);   // 텍스처의 위 가운데를 가리키는 UV 좌표
        Vector2 uvTopLeft = new Vector2(0, 1);      // 텍스처의 왼쪽 위를 가리키는 UV 좌표
        Vector2 uvTopRight = new Vector2(1, 1);      // 텍스처의 오른쪽 위를 가리키는 UV 좌표
        Vector2 uvBottomLeft = new Vector2(0, 0);      // 텍스처의 왼쪽 아래를 가리키는 UV 좌표
        Vector2 uvBottomRight = new Vector2(1, 0);      // 텍스처의 오른쪽 아래를 가리키는 UV 좌표

        // facetedVertices와 같은 순서로 하나씩 짝지음 (면마다 그림 한 장을 삼각형 모양으로)
        Vector2[] uv =
        {
            // 아래 4면: (아래 꼭짓점, 왼쪽, 오른쪽) → (아래 가운데, 왼쪽 위, 오른쪽 위)
            uvBottomCenter, uvTopLeft, uvTopRight,
            uvBottomCenter, uvTopLeft, uvTopRight,
            uvBottomCenter, uvTopLeft, uvTopRight,
            uvBottomCenter, uvTopLeft, uvTopRight,

            // 위 4면: (위 꼭짓점, 오른쪽, 왼쪽) → (위 가운데, 오른쪽 아래, 왼쪽 아래)
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
            uvTopCenter, uvBottomRight, uvBottomLeft,
        };

        // 여기까지

        mesh.Clear();
        mesh.vertices = facetedVertices;     // 다름: 6개 대신 24개
        mesh.triangles = facetedTriangles;   // 다름: 적은 순서대로
        mesh.uv = uv;                        // 다름: UV 좌표 추가
        mesh.RecalculateNormals();           // 같은 줄이지만, 이제 면마다 normal이 따로 계산됨
        mesh.RecalculateBounds();
    }

    // 머티리얼이 없거나 셰이더가 깨져 있으면(분홍색) Lit 머티리얼을 새로 만들어 붙임
    void EnsureLitMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        Material current = meshRenderer.sharedMaterial;

        bool needsMaterial = current == null
                          || current.shader == null
                          || !current.shader.isSupported
                          || current.shader.name == "Hidden/InternalErrorShader";
        if (!needsMaterial) return;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");   // URP가 아닌 프로젝트용 대비
        if (lit == null) return;

        meshRenderer.sharedMaterial = new Material(lit) { name = "Diamond_Lit (자동 생성)" };
    }
}