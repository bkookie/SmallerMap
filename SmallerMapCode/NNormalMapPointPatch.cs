using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace SmallerMap.SmallerMapCode;

[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Process), MethodType.Normal)]
public static class ScaleOscillatingIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleOscillatingIcon(IEnumerable<CodeInstruction> instructions)
    {
        // Controls the min and max scale of the oscillating icons, as well as the max scale when hovering next room for selection (this has the white outline)

        // _iconContainer.Scale = Vector2.One * scale * (Mathf.Sin(_elapsedTime) * 0.25f + 1.2f);
        // _iconContainer.Scale = _iconContainer.Scale.Lerp(Vector2.One * scale, 0.5f);

        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.IconScale);
    }
}

// Default state. All other scales are based on this scale.
[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.RefreshState), MethodType.Normal)]
public static class ScaleDefaultIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleDefaultIcon(IEnumerable<CodeInstruction> instructions)
    {
        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.IconScale);
    }
}

// When the point is clicked (on mouse-up)
[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.OnSelected), MethodType.Normal)]
public static class ScaleSelectedIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleSelectedIcon(IEnumerable<CodeInstruction> instructions)
    {
        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.IconScale, numInstancesToEdit: 1); // Only scale the icon container, not the icons as well
    }
}

// Mouse hover(relative scale, no need to change)
//[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.HoverScale), MethodType.Getter)]
//public static class MapPointScaleHoveredIconPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> MapPointScaleHoveredIcon(IEnumerable<CodeInstruction> instructions)
//    {
//        List<CodeInstruction> codes = [.. instructions];
//        for (int i = 0; i < codes.Count; i++)
//        {
//            if (codes[i].opcode == OpCodes.Ldc_R4)
//            {
//                float operand = (float)codes[i].operand;
//                if (codes[i].opcode == OpCodes.Ldc_R4 && operand == 1.45f)
//                {
//                    codes[i].operand = operand * ScaleHelper.IconScale;
//                    break;
//                }
//            }
//        }
//        return codes;
//    }
//}

// Mouse click and hold(relative scale, no need to change)
//[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.DownScale), MethodType.Getter)]
//public static class MapPointScaleDownIconPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> MapPointScaleDownIcon(IEnumerable<CodeInstruction> instructions)
//    {
//        List<CodeInstruction> codes = [.. instructions];
//        for (int i = 0; i < codes.Count; i++)
//        {
//            if (codes[i].opcode == OpCodes.Ldc_R4)
//            {
//                float operand = (float)codes[i].operand;
//                if (codes[i].opcode == OpCodes.Ldc_R4 && operand == 0.9f)
//                {
//                    codes[i].operand = operand * ScaleHelper.IconScale;
//                    break;
//                }
//            }
//        }
//        return codes;
//    }
//}

//When mouse leaves
//[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.AnimUnhover), MethodType.Normal)]
//public static class MapPointScaleUnhoverIconPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> MapPointScaleUnhoverIcon(IEnumerable<CodeInstruction> instructions)
//    {
//        return ScaleHelper.ScaleVector2Identities(instructions, ScaleHelper.IconScale);
//    }
//}