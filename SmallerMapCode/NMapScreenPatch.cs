using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace SmallerMap.SmallerMapCode;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap), MethodType.Normal)]
public static class ScaleMapPatch
{
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScaleMap(IEnumerable<CodeInstruction> instructions)
    {
        HashSet<float> constants = [];
        constants.Add(2325f);   // Map Height
        constants.Add(1050f);   // Map Width
        constants.Add(-500f);   // Room Offset X (centering)
        constants.Add(740f);    // Room Offset Y
        constants.Add(-21f);    // Jitter Lower X
        constants.Add(21f);     // Jitter Upper X
        constants.Add(-25f);    // Jitter Lower Y
        constants.Add(25f);     // Jitter Upper Y
        constants.Add(-1980f);  // Boss Point Y
        constants.Add(-2280f);  // Boss 2 Point Y
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
                    if (operand == 740f) // All rooms. By default, shifts all rooms up. Creates a little space between the Ancient and first row of rooms
                    {
                        ScaleHelper.InsertCallInstruction(codes, ref i, ScaleHelper.RoomOffsetYProperty!, OpCodes.Add);
                    }
                    else if (operand == -1980f) // Boss 1. By default, shifts Boss 1 down a little
                    {
                        // -1980 * MapScale + BossOffsetY

                        ScaleHelper.InsertCallInstruction(codes, ref i, ScaleHelper.MapScaleProperty!, OpCodes.Mul);
                        ScaleHelper.InsertCallInstruction(codes, ref i, ScaleHelper.BossOffsetYProperty!, OpCodes.Add);
                    }
                    else if (operand == -2280f) // Boss 2. Relies on Boss 1 appearing first. By default, places this a set distance above Boss 1
                    {                        
                        codes[i] = new CodeInstruction(OpCodes.Call, ScaleHelper.Boss2OffsetYProperty!); // Replace the constant with property getter.
                    }
                    else // Scale normally
                    {
                        ScaleHelper.InsertCallInstruction(codes, ref i, ScaleHelper.MapScaleProperty!, OpCodes.Mul);
                    }
                }
            }
            else if (localBuilder == null && codes[i].Calls(typeof(NNormalMapPoint).GetMethod(nameof(NNormalMapPoint.Create), [typeof(MapPoint), typeof(NMapScreen), typeof(IRunState)])))
            {
                // Find the local builder that stores the NNormalMapPoint object (gets stored on the following instruction)
                if (codes[i + 1].opcode == OpCodes.Stloc_S && codes[i + 1].operand is LocalBuilder lb)
                {
                    localBuilder = lb;
                    i++;
                }
            }
            else if (localBuilder != null && codes[i].Calls(typeof(NNormalMapPoint).GetMethod(nameof(NNormalMapPoint.SetAngle)))) // Insert after this instruction
            {
                // Calls NNormalMapPoint.RefreshState() for all points when creating the map (inside the loop), otherwise icons will not be scaled until exiting the current room.

                MethodInfo method = AccessTools.Method(typeof(NNormalMapPoint), nameof(NNormalMapPoint.RefreshState));

                codes.Insert(++i, new CodeInstruction(OpCodes.Ldloc_S, localBuilder));
                codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, method));
            }
        }

        return codes;
    }
}