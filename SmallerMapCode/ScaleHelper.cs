using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace SmallerMap.SmallerMapCode;

public static class ScaleHelper
{
    public static MethodInfo? MapScalePropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveMapScale));
    public static MethodInfo? IconScalePropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveIconScale));
    public static MethodInfo? CharIconScalePropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveCharIconScale));
    public static MethodInfo? RoomOffsetYPropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveRoomOffsetY));
    public static MethodInfo? BossOffsetYPropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveBossOffsetY));
    public static MethodInfo? Boss2OffsetYPropertyGetter = AccessTools.PropertyGetter(typeof(ScaleHelper), nameof(EffectiveBoss2OffsetY));

    public static MethodInfo? ScrollPosYMethod = AccessTools.Method(typeof(ScaleHelper), nameof(GetEffectiveScrollPosY), [typeof(float)]);
    public static MethodInfo? MaxScrollPosYPropertySetter = AccessTools.PropertySetter(typeof(ScaleHelper), nameof(MaxScrollPosY));

    public static FieldInfo? Vector2YField = AccessTools.Field(typeof(Vector2), nameof(Vector2.Y));
    public static FieldInfo? TargetDragPosField = AccessTools.Field(typeof(NMapScreen), nameof(NMapScreen._targetDragPos));

    private static bool IsDisabled => Config.DisableMod || Config.DisableInMultiplayer && RunManager.Instance.IsInProgress && !RunManager.Instance.IsSinglePlayerOrFakeMultiplayer;

    private static float EffectiveMapScale => IsDisabled ? 1f : Config.MapScale;
    private static float EffectiveIconScale => IsDisabled ? 1f : Config.IconScale;
    private static float EffectiveCharIconScale => IsDisabled ? 1f : Config.CharIconScale;
    private static float EffectiveRoomOffsetY => IsDisabled ? 0f : Config.RoomOffsetY;
    private static float EffectiveBossOffsetY => IsDisabled ? 0f : Config.BossOffsetY;
    private static float EffectiveBoss2OffsetY => IsDisabled ? 0f : Config.Boss2OffsetY;
    private static float MaxScrollPosY { get; set; } = float.MinValue; // Stores the last used scroll position (usually from user input)

    /// <summary>
    /// Returns the Y position that the map should be scrolled to.
    /// </summary>
    /// <param name="preferredY">The value that the game calculated.</param>
    private static float GetEffectiveScrollPosY(float preferredY)
    {
        if (Config.DisableMod)
            return preferredY;

        if (Config.LockScrollPosition && MaxScrollPosY != float.MinValue)
            return MaxScrollPosY;

        return Mathf.Max(preferredY, MaxScrollPosY);
    }

    public static void ClearCachedFields()
    {
        MapScalePropertyGetter = null;
        IconScalePropertyGetter = null;
        CharIconScalePropertyGetter = null;
        RoomOffsetYPropertyGetter = null;
        BossOffsetYPropertyGetter = null;
        Boss2OffsetYPropertyGetter = null;

        ScrollPosYMethod = null;
        MaxScrollPosYPropertySetter = null;

        Vector2YField = null;
        TargetDragPosField = null;
    }

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
    /// <param name="method">The method call to push onto the stack.</param>
    /// <param name="opCode">The operation to perform.</param>
    public static void InsertCallInstruction(List<CodeInstruction> instructions, ref int index, MethodInfo method, OpCode opCode)
    {
        instructions.Insert(++index, new CodeInstruction(OpCodes.Call, method));
        instructions.Insert(++index, new CodeInstruction(opCode));
    }

    /// <summary>
    /// Multiplies all instances of <see cref="Vector2.One"/> by the value returned by <paramref name="method"/>.
    /// </summary>
    /// <param name="instructions">The list of instructions to modify.</param>
    /// <param name="method">The method call to push onto the stack.</param>
    /// <param name="numInstancesToEdit">How many instances to modify.</param>
    /// <returns></returns>
    public static IEnumerable<CodeInstruction> ScaleVector2Identities(IEnumerable<CodeInstruction> instructions, MethodInfo method, int numInstancesToEdit = -1)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (numInstancesToEdit == 0)
                break;

            if (codes[i].Calls(typeof(Vector2).GetMethod("get_One")))
            {
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, method)); // Push property getter
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, typeof(Vector2).GetMethod("op_Multiply", [typeof(Vector2), typeof(float)]))); // Vector2.One * scale

                numInstancesToEdit--;
            }
        }

        return codes;
    }

    public static IEnumerable<CodeInstruction> StoreTargetDragPosY(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> codes = [.. instructions];

        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].StoresField(TargetDragPosField))
            {
                // insert before this line
                i--;
                codes.Insert(++i, new CodeInstruction(OpCodes.Dup));
                codes.Insert(++i, new CodeInstruction(OpCodes.Ldfld, Vector2YField));
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, MaxScrollPosYPropertySetter));
                i++;
            }
        }

        return codes;
    }
}