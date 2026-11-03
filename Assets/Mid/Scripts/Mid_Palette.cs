using UnityEngine;

// M4 컬러 팔레트 — The Snow Girl (2017), Mixtape Club
// F-Phase 스크립트(불빛 일렁임 등)도 이 값을 참조한다
public static class Mid_Palette
{
    public static Color Snow      => Hex("#6DA7E6"); // 눈
    public static Color Wall      => Hex("#386CA9"); // 벽 (빛 받는 면)
    public static Color WoodShade => Hex("#154685"); // 그늘진 나무
    public static Color Roof      => Hex("#030509"); // 지붕
    public static Color Tree      => Hex("#141F43"); // 나무
    public static Color Window    => Hex("#FEDCBF"); // 창문 불빛 (씬에서 유일한 따뜻한 색)

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
