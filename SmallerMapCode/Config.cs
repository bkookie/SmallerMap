using BaseLib.Config;

namespace SmallerMap.SmallerMapCode;

[ConfigHoverTipsByDefault]
internal class Config : SimpleModConfig
{
    public static bool DisableMod { get; set; } = false;
    public static bool DisableInMultiplayer { get; set; } = true;
    public static bool LockScrollPosition { get; set; } = false;

    [ConfigSlider(0.01, 1.00, 0.01, Format = "{0:0.00}")]
    public static float MapScale { get; set; } = 0.43f;

    [ConfigSlider(0.01, 1.00, 0.01, Format = "{0:0.00}")]
    public static float RoomIconScale { get; set; } = 0.55f;

    [ConfigSlider(0.01, 1.00, 0.01, Format = "{0:0.00}")]
    public static float CharIconScale { get; set; } = 0.55f;

    [ConfigSlider(-200, 200, 1)]
    public static float RoomOffsetY { get; set; } = -75f;

    [ConfigSlider(-200, 200, 1)]
    public static float BossOffsetY { get; set; } = 100f;


    private const float Boss2OffsetYFromBoss1 = -350f;
    [ConfigIgnore]
    public static float Boss2OffsetY => -1980f * MapScale + BossOffsetY + Boss2OffsetYFromBoss1;
}
