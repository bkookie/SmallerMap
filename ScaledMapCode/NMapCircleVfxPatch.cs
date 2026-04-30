using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ScaledMap.ScaledMapCode;

// Circles drawn around room when entering
[HarmonyPatch(typeof(NMapCircleVfx), nameof(NMapCircleVfx._Ready), MethodType.Normal)]
public static class ScaleCircleVfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleCircleVfx(IEnumerable<CodeInstruction> instructions)
    {
        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.IconScale);
    }
}