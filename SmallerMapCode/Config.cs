using BaseLib.Config;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace SmallerMap.SmallerMapCode;

[ConfigHoverTipsByDefault]
internal class Config : SimpleModConfig
{
    [ConfigSection("ScalingSettings")]
    [ConfigSlider(0.01, 1.00, 0.01, Format = "{0:0.00}")]
    public static float MapScale { get; set; } = 0.43f;

    [ConfigSlider(0.01, 1.00, 0.01, Format = "{0:0.00}")]
    public static float IconScale { get; set; } = 0.55f;

    [ConfigSlider(-200, 200, 1)]
    public static float RoomOffsetY { get; set; } = -75f;

    [ConfigSlider(-200, 200, 1)]
    public static float BossOffsetY { get; set; } = 100f;

    [ConfigVisibleIf(nameof(Ascension10Unlocked))]
    [ConfigSlider(-200, 200, 1)]
    public static float Boss2OffsetY { get; set; } = -100f;

    private static bool Ascension10Unlocked()
    {
        foreach (CharacterModel c in SaveManager.Instance.GenerateUnlockStateFromProgress().Characters)
        {
            if (SaveManager.Instance.Progress.GetOrCreateCharacterStats(c.Id).MaxAscension == 10)
            {
                return true;
            }
        }

        return false;
    }
}
