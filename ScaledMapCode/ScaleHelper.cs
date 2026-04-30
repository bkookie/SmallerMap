using System.Reflection.Emit;
using Godot;
using HarmonyLib;

namespace ScaledMap.ScaledMapCode;

public static class ScaleHelper
{
    public const float MapScale = 0.43f;
    public const float IconScale = 0.55f;

    public static IEnumerable<CodeInstruction> ScaleVector2Identities(IEnumerable<CodeInstruction> instructions, float scale, int numInstancesToEdit = -1)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (numInstancesToEdit == 0)
                break;

            if (codes[i].Calls(typeof(Vector2).GetMethod("get_One")))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldc_R4, scale)); // Push float constant
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, typeof(Vector2).GetMethod("op_Multiply", [typeof(Vector2), typeof(float)]))); // Vector2.One * scale
                i += 2;

                numInstancesToEdit--;
            }
        }

        return codes;
    }
}