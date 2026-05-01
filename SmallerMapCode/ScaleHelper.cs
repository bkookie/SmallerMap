using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace SmallerMap.SmallerMapCode;

public static class ScaleHelper
{
    public static readonly MethodInfo MapScaleProperty = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveMapScale));
    public static readonly MethodInfo IconScaleProperty = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveIconScale));
    public static readonly MethodInfo RoomOffsetYProperty = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveRoomOffsetY));
    public static readonly MethodInfo BossOffsetYProperty = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveBossOffsetY));
    public static readonly MethodInfo Boss2OffsetYProperty = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveBoss2OffsetY));

    private static bool IsMultiplayerAndDisabled => Config.DisableInMultiplayer && RunManager.Instance.IsInProgress && !RunManager.Instance.IsSinglePlayerOrFakeMultiplayer;

    private static float EffectiveMapScale => IsMultiplayerAndDisabled ? 1f : Config.MapScale;
    private static float EffectiveIconScale => IsMultiplayerAndDisabled ? 1f : Config.IconScale;
    private static float EffectiveRoomOffsetY => IsMultiplayerAndDisabled ? 0f : Config.RoomOffsetY;
    private static float EffectiveBossOffsetY => IsMultiplayerAndDisabled ? 0f : Config.BossOffsetY;
    private static float EffectiveBoss2OffsetY => IsMultiplayerAndDisabled ? 0f : Config.Boss2OffsetY;

    public const float Boss2OffsetY = -350f;

    /// <summary>
    /// Pushes a float constant onto the stack, then performs an operation on it.
    /// </summary>
    /// <param name="instructions">The list of instructions to modify.</param>
    /// <param name="index">The current instruction index. New instructions are inserted after, then increments the index.</param>
    /// <param name="value">The float to push onto the stack.</param>
    /// <param name="opCode">The operation to perform.</param>
    public static void InsertFloatInstruction(List<CodeInstruction> instructions, ref int index, float value, OpCode opCode)
    {
        instructions.Insert(++index, new CodeInstruction(OpCodes.Ldc_R4, value));
        instructions.Insert(++index, new CodeInstruction(opCode));
    }

    /// <summary>
    /// Pushes a static field onto the stack, then performs an operation on it.
    /// </summary>
    /// <param name="instructions">The list of instructions to modify.</param>
    /// <param name="index">The current instruction index. New instructions are inserted after, then increments the index.</param>
    /// <param name="staticMethod">The static method to push onto the stack.</param>
    /// <param name="opCode">The operation to perform.</param>
    public static void InsertCallInstruction(List<CodeInstruction> instructions, ref int index, MethodInfo staticMethod, OpCode opCode)
    {
        instructions.Insert(++index, new CodeInstruction(OpCodes.Call, staticMethod));
        instructions.Insert(++index, new CodeInstruction(opCode));
    }

    /// <summary>
    /// Multiplies all instances of <see cref="Vector2.One"/> by the value provided by <paramref name="staticMethod"/>.
    /// </summary>
    /// <param name="instructions">The list of instructions to modify.</param>
    /// <param name="staticMethod">The static method to push onto the stack.</param>
    /// <param name="numInstancesToEdit">How many instances to modify.</param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> ScaleVector2Identities(IEnumerable<CodeInstruction> instructions, MethodInfo staticMethod, int numInstancesToEdit = -1)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (numInstancesToEdit == 0)
                break;

            if (codes[i].Calls(typeof(Vector2).GetMethod("get_One")))
            {
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, staticMethod)); // Push property getter
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, typeof(Vector2).GetMethod("op_Multiply", [typeof(Vector2), typeof(float)]))); // Vector2.One * scale

                numInstancesToEdit--;
            }
        }

        return codes;
    }
}