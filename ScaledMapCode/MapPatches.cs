using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace ScaledMap.ScaledMapCode;

file static class Scale
{
    public const float Map = 0.43f;
    public const float Icon = 0.55f;

    public static IEnumerable<CodeInstruction> ScaleVector2Identities(IEnumerable<CodeInstruction> instructions, float scale, int numInstancesToEdit = -1)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (numInstancesToEdit == 0)
                break;

            if (codes[i].Calls(typeof(Vector2).GetMethod("get_One")))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, scale));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, typeof(Vector2).GetMethod("op_Multiply", [typeof(Vector2), typeof(float)])));
                i += 2;

                numInstancesToEdit--;
            }
        }

        return codes;
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap), MethodType.Normal)]
public static class ScaleMapPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleMap(IEnumerable<CodeInstruction> instructions)
    {
        HashSet<float> constants = [];
        constants.Add(2325f);   // Map Height
        constants.Add(1050f);   // Map Width
        constants.Add(-500f);   // Offset X (centering)
        constants.Add(740f);    // Offset Y
        constants.Add(-21f);    // Jitter Lower X
        constants.Add(21f);     // Jitter Upper X
        constants.Add(-25f);    // Jitter Lower Y
        constants.Add(25f);     // Jitter Upper Y
        constants.Add(-1980f);  // Boss Point Y
        constants.Add(-2280f);  // Second Boss Point Y
        //constants.Add(-200f);   // Boss Point X (this and below dont need to be scaled)
        //constants.Add(-80f);    // Starting Point X
        //constants.Add(720f);    // Ancient Point Y
        //constants.Add(800f);    // Non-ancient Starting Point Y

        List<CodeInstruction> codes = [.. instructions];

        LocalBuilder? localBuilder = null;

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_R4)
            {
                float operand = (float)codes[i].operand;

                if (constants.Contains(operand))
                {
                    if (operand == 740f)
                    {
                        codes[i].operand = operand - 75f; // Shift all rooms up. Creates a little space between the Ancient and first row of rooms
                    }
                    else if (operand == -1980f)
                    {
                        codes[i].operand = operand * Scale.Map + 100f; // Shift Boss 1 down a little
                    }
                    else if (operand == -2280f)
                    {
                        codes[i].operand = operand * Scale.Map - 100f; // Shift Boss 2 up a little
                    }
                    else
                    {
                        codes[i].operand = operand * Scale.Map; // Scale normally
                    }
                }
            }
            else if (localBuilder == null && codes[i].Calls(typeof(NNormalMapPoint).GetMethod(nameof(NNormalMapPoint.Create), [typeof(MapPoint), typeof(NMapScreen), typeof(IRunState)])))
            {
                // Find the local builder that stores the NNormalMapPoint object (on the next line)
                if (codes[i + 1].opcode == OpCodes.Stloc_S && codes[i + 1].operand is LocalBuilder lb)
                {
                    localBuilder = lb;
                    i++;
                }
            }
            else if (localBuilder != null && codes[i].Calls(typeof(NNormalMapPoint).GetMethod(nameof(NNormalMapPoint.SetAngle)))) // Insert after this line
            {
                // Call NNormalMapPoint.RefreshState() for all points when creating the map, otherwise icons will not be scaled until exiting the current room.

                MethodInfo method = AccessTools.Method(typeof(NNormalMapPoint), nameof(NNormalMapPoint.RefreshState));

                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_S, localBuilder));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, method));
                i += 2;
            }
        }

        return codes;
    }
}

[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint._Process), MethodType.Normal)]
public static class MapPointScaleOscillatingIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapPointScaleOscillatingIcon(IEnumerable<CodeInstruction> instructions)
    {
        // Controls the min and max scale of the oscillating icons, as well as the max scale when hovering next room for selection (this has the white outline)

        // _iconContainer.Scale = Vector2.One * scale * (Mathf.Sin(_elapsedTime) * 0.25f + 1.2f);
        // _iconContainer.Scale = _iconContainer.Scale.Lerp(Vector2.One * scale, 0.5f);

        return Scale.ScaleVector2Identities(instructions, Scale.Icon);
    }
}

// Default state. All other scales are based on this scale.
[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.RefreshState), MethodType.Normal)]
public static class MapPointScaleStaticIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapPointScaleStaticIcon(IEnumerable<CodeInstruction> instructions)
    {
        return Scale.ScaleVector2Identities(instructions, Scale.Icon);
    }
}

// When the point is clicked (on mouse-up)
[HarmonyPatch(typeof(NNormalMapPoint), nameof(NNormalMapPoint.OnSelected), MethodType.Normal)]
public static class MapPointScaleSelectedIconPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapPointScaleSelectedIcon(IEnumerable<CodeInstruction> instructions)
    {
        return Scale.ScaleVector2Identities(instructions, Scale.Icon, numInstancesToEdit: 1); // Only scale the container, not the icons as well
    }
}

// Circles drawn around room when entering
[HarmonyPatch(typeof(NMapCircleVfx), nameof(NMapCircleVfx._Ready), MethodType.Normal)]
public static class MapPointScaleCircleVfxPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> MapPointScaleCircleVfx(IEnumerable<CodeInstruction> instructions)
    {
        return Scale.ScaleVector2Identities(instructions, Scale.Icon);
    }
}

// Mouse hover (relative scale, no need to change)
//[HarmonyPatch(typeof(NNormalMapPoint), "HoverScale", MethodType.Getter)]
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
//                    codes[i].operand = operand * Scale.Icon;
//                    break;
//                }
//            }
//        }
//        return codes;
//    }
//}

// Mouse click and hold (relative scale, no need to change)
//[HarmonyPatch(typeof(NNormalMapPoint), "DownScale", MethodType.Getter)]
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
//                    codes[i].operand = operand * 0.1f;
//                    break;
//                }
//            }
//        }
//        return codes;
//    }
//}

// When mouse leaves
//[HarmonyPatch(typeof(NNormalMapPoint), "AnimUnhover", MethodType.Normal)]
//public static class MapPointScaleUnhoverIconPatch
//{
//    [HarmonyTranspiler]
//    private static IEnumerable<CodeInstruction> MapPointScaleUnhoverIcon(IEnumerable<CodeInstruction> instructions)
//    {
//        return Scale.MultiplyVector2Identities(instructions, Scale.Icon);
//    }
//}