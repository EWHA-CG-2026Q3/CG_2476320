using UnityEditor;
using UnityEngine;

// 메뉴: Tools/Mid/Generate Snow Noise
// 이음새 없는(tileable) fBm 노이즈 2장 생성
//   T_Mid_SnowDetail.png  — 고주파, 눈 바닥 디테일용
//   T_Mid_SnowHeight.png  — 저주파, F1 높이맵용 (Linear)
public class Mid_NoiseTextureGenerator
{
    const int Size = 512;
    const string DetailPath = "Assets/Mid/Textures/T_Mid_SnowDetail.png";
    const string HeightPath = "Assets/Mid/Textures/T_Mid_SnowHeight.png";

    [MenuItem("Tools/Mid/Generate Snow Noise")]
    static void Generate()
    {
        SavePng(MakeNoise(octaves: 5, baseFreq: 4f), DetailPath, linear: false);
        SavePng(MakeNoise(octaves: 3, baseFreq: 1.5f), HeightPath, linear: true);
        AssetDatabase.Refresh();
        SetImport(DetailPath, linear: false);
        SetImport(HeightPath, linear: true);
        Debug.Log("Snow noise 생성 완료");
    }

    // 4방향 블렌딩으로 이음새 없는 fBm 생성
    static Texture2D MakeNoise(int octaves, float baseFreq)
    {
        var tex = new Texture2D(Size, Size, TextureFormat.R8, false);
        float[] data = new float[Size * Size];
        float min = float.MaxValue, max = float.MinValue;

        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float v = 0, amp = 1, freq = baseFreq, total = 0;
            for (int o = 0; o < octaves; o++)
            {
                v += Tileable(x / (float)Size * freq, y / (float)Size * freq, freq) * amp;
                total += amp;
                amp *= 0.5f;
                freq *= 2f;
            }
            v /= total;
            data[y * Size + x] = v;
            if (v < min) min = v;
            if (v > max) max = v;
        }

        // 0~1 정규화
        Color[] cols = new Color[Size * Size];
        for (int i = 0; i < data.Length; i++)
        {
            float n = (data[i] - min) / (max - min);
            cols[i] = new Color(n, n, n, 1);
        }
        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    // 4코너 샘플 블렌딩 — Perlin이 정수 경계에서 이어지는 성질 이용 (S18처럼 Repeat 래핑)
    static float Tileable(float x, float y, float w)
    {
        float fx = x % 1f, fy = y % 1f;
        return Mathf.PerlinNoise(x,   y  ) * (1-fx) * (1-fy)
             + Mathf.PerlinNoise(x-w, y  ) * fx     * (1-fy)
             + Mathf.PerlinNoise(x,   y-w) * (1-fx) * fy
             + Mathf.PerlinNoise(x-w, y-w) * fx     * fy;
    }

    static void SavePng(Texture2D tex, string path, bool linear)
    {
        var bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(
            Application.dataPath + path.Substring("Assets".Length), bytes);
    }

    static void SetImport(string path, bool linear)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        if (imp == null) return;
        imp.wrapMode   = TextureWrapMode.Repeat;
        imp.filterMode = FilterMode.Bilinear;
        imp.sRGBTexture = !linear;
        imp.SaveAndReimport();
    }
}
