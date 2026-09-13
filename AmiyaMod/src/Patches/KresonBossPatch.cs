using System;
using System.Reflection;
using Amiya.Boss;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace Amiya.Patches;

/// <summary>
/// 把克雷松强制替换成三层的 boss：
///   · 非进阶 10：整层的 boss 换成克雷松（只有一场 boss 战）；
///   · 进阶 10（三层有两个 boss）：第一个保持原版随机 boss，第二个固定为克雷松。
///
/// 两个补丁点都要，缺一不可：
///   1. RunManager.GenerateRooms —— 新局时 boss（与第二个 boss）都是在这里定的；
///      第二个 boss 的选择发生在 ActModel.GenerateRooms 之后，所以必须在 RunManager 这一层做。
///   2. ActModel.ValidateRoomsAfterLoad —— 读档不会重跑 GenerateRooms，boss 是从存档里读回来的，
///      想让老存档也换成克雷松，必须在这里再强制一次。
///
/// 注册方式与 AncientRelicPatch 一致：手工 Patch + 日志确认（本环境下特性式注册会被静默跳过）。
/// </summary>
internal static class KresonBossPatch
{
    /// <summary>
    /// 【测试开关】true = 把克雷松放进第一层的 boss（跑几步就能打，方便调试）；
    /// false = 正式行为，替换三层的 boss。测完记得改回 false。
    /// </summary>
    private const bool ReplaceFirstActForTesting = false;

    private static PropertyInfo? _stateProperty;

    public static void Apply(Harmony harmony)
    {
        try
        {
            MethodInfo? generateRooms = AccessTools.Method(typeof(RunManager), "GenerateRooms");
            if (generateRooms == null)
            {
                Log.Error("[Amiya] RunManager.GenerateRooms 反射解析失败，克雷松无法替换三层 boss");
            }
            else
            {
                harmony.Patch(generateRooms, postfix: new HarmonyMethod(typeof(KresonBossPatch), nameof(AfterGenerateRooms)));
                Log.Info("[Amiya] patched RunManager.GenerateRooms -> 克雷松替换三层 boss（进阶10 替换第二个）");
            }

            MethodInfo? validate = AccessTools.Method(typeof(ActModel), "ValidateRoomsAfterLoad");
            if (validate == null)
            {
                Log.Error("[Amiya] ActModel.ValidateRoomsAfterLoad 反射解析失败，读档时无法替换 boss");
            }
            else
            {
                harmony.Patch(validate, postfix: new HarmonyMethod(typeof(KresonBossPatch), nameof(AfterValidateRooms)));
                Log.Info("[Amiya] patched ActModel.ValidateRoomsAfterLoad -> 读档后仍为克雷松");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] patching kreson boss replacement failed: {ex}");
        }
    }

    /// <summary>新局：最后一个 act（三层）的 boss 换成克雷松。</summary>
    private static void AfterGenerateRooms(RunManager __instance)
    {
        try
        {
            _stateProperty ??= AccessTools.Property(typeof(RunManager), "State");
            if (_stateProperty?.GetValue(__instance) is not RunState state || state.Acts.Count == 0)
            {
                return;
            }
            ActModel act = state.Acts[ReplaceFirstActForTesting ? 0 : state.Acts.Count - 1];
            EncounterModel kreson = ModelDb.Encounter<KresonBossEncounter>();
            // 这里不用 AscensionHelper（它依赖 RunManager.IsInProgress，开局阶段可能还是 false），
            // 直接读 RunState 上的进阶等级。测试模式固定单 boss。
            bool doubleBoss = !ReplaceFirstActForTesting && state.AscensionLevel >= (int)AscensionLevel.DoubleBoss;
            if (doubleBoss)
            {
                act.SetSecondBossEncounter(kreson);
            }
            else
            {
                act.SetSecondBossEncounter(null);
                act.SetBossEncounter(kreson);
            }
            Log.Info($"[Amiya] kreson boss installed: act={act.Id.Entry} doubleBoss={doubleBoss} testFirstAct={ReplaceFirstActForTesting}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson boss replacement (new run) failed: {ex}");
        }
    }

    /// <summary>读档：三层的 boss 再强制一次（存档里层数结构不会变，用 HasSecondBoss 判断是否进阶10结构）。</summary>
    private static void AfterValidateRooms(ActModel __instance)
    {
        try
        {
            if (__instance.Index != (ReplaceFirstActForTesting ? 0 : ModelDb.ActsByIndex.Count - 1))
            {
                return;
            }
            EncounterModel kreson = ModelDb.Encounter<KresonBossEncounter>();
            if (__instance.HasSecondBoss)
            {
                if (!__instance.SecondBossEncounter!.Id.Equals(kreson.Id))
                {
                    __instance.SetSecondBossEncounter(kreson);
                    Log.Info("[Amiya] kreson boss installed on load (second boss)");
                }
            }
            else if (!__instance.BossEncounter.Id.Equals(kreson.Id))
            {
                __instance.SetBossEncounter(kreson);
                Log.Info("[Amiya] kreson boss installed on load (single boss)");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] kreson boss replacement (load) failed: {ex}");
        }
    }
}
