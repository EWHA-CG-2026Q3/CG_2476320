using UnityEngine;

// M4 컬러 팔레트 — The Snow Girl (2017), Mixtape Club
// F-Phase 스크립트(불빛 일렁임 등)도 이 값을 참조한다
public static class Mid_Palette
{
    // ── M4 기본 팔레트 (파란 계열) ──────────────────────────
    public static Color Snow      => Hex("#6DA7E6"); // 눈
    public static Color Wall      => Hex("#386CA9"); // 벽 (빛 받는 면)
    public static Color WoodShade => Hex("#154685"); // 그늘진 나무
    public static Color Roof      => Hex("#030509"); // 지붕
    public static Color Tree      => Hex("#141F43"); // 나무
    public static Color Window    => Hex("#FEDCBF"); // 창문 불빛

    // ── 밤 12색 팔레트 (C0: Reference/palette_night12.png) ──
    // 파란 계열 9색
    public static Color NightSnowLit    => Hex("#8FB0D8"); // 빛 받는 눈
    public static Color NightSnow       => Hex("#7FA0C9"); // 기본 눈
    public static Color NightSnowShade1 => Hex("#748FBB");
    public static Color NightSnowShade2 => Hex("#6987B4");
    public static Color NightWall       => Hex("#5D7BA6"); // 빛 받는 나무·벽
    public static Color NightRoof       => Hex("#33496E"); // 지붕
    public static Color NightWoodShade  => Hex("#2A3854"); // 그늘진 나무
    public static Color NightDark       => Hex("#1C2A45"); // 어두운 부분
    public static Color NightDarkest    => Hex("#131C30"); // 가장 어두운 곳
    // 따뜻한 색 3개 — 씬에서 이 3색만 따뜻하게, 정자 내부와 그 빛이 닿는 곳에만
    public static Color WarmShade       => Hex("#6E2F2E"); // 따뜻한 그늘 (실내 구석, 청사 띠 주변)
    public static Color WarmMid         => Hex("#A05A35"); // 실내광 받은 나무
    public static Color WarmLight       => Hex("#FFB257"); // 창호지 발광, 실내 광원

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
