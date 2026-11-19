using UnityEngine;

[ExecuteAlways] // Play 모드가 아니어도(에디터에서) 매 프레임 실행되게 함
[RequireComponent(typeof(MeshFilter))]
public class S23_BilinearPatch_Finish : MonoBehaviour
{
    [Header("네 모서리 (빈 GameObject)")]
    public Transform corner00; // u=0, v=0
    public Transform corner10; // u=1, v=0
    public Transform corner01; // u=0, v=1
    public Transform corner11; // u=1, v=1

    [Header("격자 칸 수 (u, v 방향 각각)")]
    [Range(1, 20)] public int uSegments = 6;
    [Range(1, 20)] public int vSegments = 6;

    Mesh mesh; // 이 오브젝트가 들고 있는 메시 — 한 번만 생성하고 내용만 매번 갈아 끼움

    void OnEnable()
    {
        // 오브젝트가 활성화될 때 Mesh를 하나 만들어 MeshFilter에 연결
        mesh = new Mesh();
        mesh.name = "BilinearPatch";
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void Update()
    {
        BuildMesh(); // 매 프레임 코너 위치를 다시 읽어 메시를 새로 계산

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // [ExecuteAlways]여도 에디터는 Update()를 매 프레임 자동으로 부르지 않음
            // → Scene 뷰를 강제로 다시 그리게 해서, 코너를 움직이면 바로 반영되게 함
            UnityEditor.SceneView.RepaintAll();
        }
#endif
    }

    void BuildMesh()
    {
        // 네 코너가 모두 연결되어 있지 않으면 계산하지 않음
        if (corner00 == null || corner10 == null || corner01 == null || corner11 == null) return;

        int uCount = uSegments + 1; // u 방향 "점" 개수 (칸 수 + 1)
        int vCount = vSegments + 1; // v 방향 "점" 개수 (칸 수 + 1)

        Vector3[] vertices = new Vector3[uCount * vCount]; // 격자 점들의 좌표
        Vector2[] uv = new Vector2[vertices.Length];       // 같은 점들의 텍스처 좌표

        // ── 1단계: 점 찍기 ──────────────────────────────
        for (int j = 0; j < vCount; j++)
        {
            float v = (float)j / vSegments; // 이 줄의 v 값 (0~1)
            for (int i = 0; i < uCount; i++)
            {
                float u = (float)i / uSegments; // 이 칸의 u 값 (0~1)
                int index = j * uCount + i;      // 2차원 격자 위치를 1차원 배열 자리로 변환

                // bilinear patch 공식: 네 코너를 u, v 비율로 섞은 위치 (월드 좌표)
                Vector3 worldPos =
                    (1 - u) * (1 - v) * corner00.position +
                    u * (1 - v) * corner10.position +
                    (1 - u) * v * corner01.position +
                    u * v * corner11.position;

                // mesh.vertices는 로컬 좌표라야 하므로 월드 → 로컬로 변환
                vertices[index] = transform.InverseTransformPoint(worldPos);

                // 같은 (u, v)를 텍스처 좌표로도 그대로 사용
                uv[index] = new Vector2(u, v);
            }
        }

        // ── 2단계: 점 잇기 (사각형 칸 → 삼각형 2개) ──────
        int[] triangles = new int[uSegments * vSegments * 6]; // 칸 수 × 삼각형 2개 × 꼭짓점 3개
        int t = 0; // triangles 배열에 채워 넣을 위치

        for (int j = 0; j < vSegments; j++)       // 칸 기준이라 vSegments
        {
            for (int i = 0; i < uSegments; i++)   // 칸 기준이라 uSegments
            {
                // 사각형 칸 하나는 이웃한 두 줄(j, j+1)과 이웃한 두 열(i, i+1)이 만나는 네 점을 모서리로 하는 작은 사각형
                int a = j * uCount + i; // 왼쪽 아래 점 (지금 칸의 기준이 되는 점: j줄, i열)
                int b = a + 1;          // a의 오른쪽 옆, 같은 줄의 다음 열(i+1) → 오른쪽 아래
                int c = a + uCount;     // a에서 한 줄 위(j+1), 같은 열(i) → 왼쪽 위 (한 줄 전체 길이 uCount만큼 건너뛰어야 다음 줄로 넘어감)
                int d = c + 1;          // c의 오른쪽 옆, 다음 줄의 다음 열(i+1) → 오른쪽 위

                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b; // 삼각형 1
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d; // 삼각형 2
            }
        }

        // ── 3단계: Mesh에 반영 ───────────────────────────
        mesh.Clear();               // 이전 모양의 데이터를 지움 (칸 수가 바뀌어도 안전하게)
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();  // 법선(빛 반사 방향) 자동 계산
        mesh.RecalculateBounds();   // 경계 상자 재계산
    }
}