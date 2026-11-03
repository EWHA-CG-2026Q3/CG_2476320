using UnityEditor;
using UnityEngine;

public class M0_Setup
{
    public static void Execute()
    {
        // 폴더 구조 생성
        string[] folders = {
            "Assets/Mid",
            "Assets/Mid/Materials",
            "Assets/Mid/Shaders",
            "Assets/Mid/ShaderGraphs",
            "Assets/Mid/Scripts",
            "Assets/Mid/Prefabs",
            "Assets/Mid/Editor",
            "Assets/Mid/Textures"
        };

        foreach (var folder in folders)
        {
            var parts = folder.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string full = string.Join("/", parts, 0, i + 1);
                if (!AssetDatabase.IsValidFolder(full))
                {
                    AssetDatabase.CreateFolder(parent, parts[i]);
                    Debug.Log($"폴더 생성: {full}");
                }
                parent = full;
            }
        }

        // 에셋 복제 (AssetDatabase.CopyAsset 사용)
        var copies = new (string src, string dst)[]
        {
            ("Assets/S15/S15_Landscape.unity",                              "Assets/Mid/Mid_Winter.unity"),
            ("Assets/S13/S13_Pavilion_Finish/S13_VolumeProfile.asset",      "Assets/Mid/Mid_VolumeProfile.asset"),
            ("Assets/S15/Mat_S15_SG_Flow.mat",                              "Assets/Mid/Materials/Mat_Mid_Puddle.mat"),
            ("Assets/S13/Mat_S13_FireBowl.mat",                             "Assets/Mid/Materials/Mat_Mid_FireBowl.mat"),
            ("Assets/S13/Mat_S13_Smoke.mat",                                "Assets/Mid/Materials/Mat_Mid_Smoke.mat"),
        };

        foreach (var (src, dst) in copies)
        {
            if (!System.IO.File.Exists(Application.dataPath + dst.Substring("Assets".Length)))
            {
                bool ok = AssetDatabase.CopyAsset(src, dst);
                Debug.Log(ok ? $"복제 성공: {src} → {dst}" : $"복제 실패: {src}");
            }
            else
            {
                Debug.Log($"이미 존재: {dst}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("M0_Setup 완료");
    }
}
