using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace SmallerMap.SmallerMapCode;

// These are fore single player map markers

[HarmonyPatch(typeof(NMapMarker), nameof(NMapMarker._Ready), MethodType.Normal)]
public static class MapMarkerPositionPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapMarkerPosition(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [.. instructions];
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == -35f)
            {
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, ScaleHelper.CharIconScalePropertyGetter!));
                codes.Insert(++i, new CodeInstruction(OpCodes.Mul));
            }
        }
        return codes;
    }
}

[HarmonyPatch(typeof(NMapMarker), nameof(NMapMarker.SetMapPoint), MethodType.Normal)]
public static class MapMarkerScaleSetPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapMarkerScaleSet(IEnumerable<CodeInstruction> instructions)
    {
        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.CharIconScalePropertyGetter!);
    }
}

[HarmonyPatch(typeof(NMapMarker), nameof(NMapMarker.HideMapPoint), MethodType.Normal)]
public static class MapMarkerScaleHidePatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapMarkerScaleHide(IEnumerable<CodeInstruction> instructions)
    {
        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.CharIconScalePropertyGetter!);
    }
}