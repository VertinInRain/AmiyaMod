using System;
using System.Linq;
using System.Reflection;
using Amiya.Cards;
using Amiya.Models;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Amiya.Patches;

/// <summary>
/// 先古遗物联动（仅阿米娅）：
///   古老牙齿（ArchaicTooth）→ 卡组中的思念变为先古牌万千愿景（保持升级状态）；
///   尘封魔典（DustyTome）→ 获得一张升级后的神圣复苏。
///
/// 实现方式：走官方接口 + 官方流程（都是被 await 的确定性流程，多人安全）：
///   思念实现 ITranscendenceCard（BaseLib 注册进 ArchaicTooth 的升华映射表）；
///   神圣复苏实现 ITomeCard（BaseLib 把尘封魔典的候选池锁定为它）。
/// 本补丁只保留"护栏"，不再自己执行异步效果——此前的 fire-and-forget 版本会在
/// 确定性流程之外改动牌组，多人下存在状态分歧风险：
///   · 非阿米娅 → 放行原版逻辑；
///   · 阿米娅 + 古老牙齿但牌组里没有思念 → 跳过原版（原版会对 null 调 Transform 抛异常）；
///   · 阿米娅 + 尘封魔典但遗物没有候选牌 → 跳过原版（避免空引用）。
/// </summary>
internal static class AncientRelicPatch
{
    /// <summary>
    /// 显式补丁注册（方法级 [HarmonyPatch] 特性在本环境 PatchAll 时被静默跳过，
    /// 诊断日志确认 ArchaicTooth/DustyTome 两个补丁 NOT ATTACHED，故改为手工 Patch）。
    /// </summary>
    public static void Apply(Harmony harmony)
    {
        PatchAfterObtained<ArchaicTooth>(harmony, nameof(ArchaicToothPrefix));
        PatchAfterObtained<DustyTome>(harmony, nameof(DustyTomePrefix));
    }

    private static void PatchAfterObtained<TRelic>(Harmony harmony, string prefixName) where TRelic : RelicModel
    {
        try
        {
            MethodInfo? original = AccessTools.Method(typeof(TRelic), "AfterObtained");
            if (original == null)
            {
                Log.Error($"[Amiya] {typeof(TRelic).Name}.AfterObtained 反射解析失败");
                return;
            }
            var prefix = new HarmonyMethod(typeof(AncientRelicPatch), prefixName);
            harmony.Patch(original, prefix: prefix);
            Log.Info($"[Amiya] patched {typeof(TRelic).Name}.AfterObtained -> {original.DeclaringType!.Name}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] patching {typeof(TRelic).Name}.AfterObtained failed: {ex}");
        }
    }

    private static bool ArchaicToothPrefix(ArchaicTooth __instance)
    {
        Player? player = __instance.Owner;
        if (player == null || player.Character is not AmiyaCharacter)
        {
            return true;
        }
        if (player.Deck.Cards.Any(c => c is AmiyaFormSwitch))
        {
            return true; // 官方流程（经 ITranscendenceCard 映射）完成 思念 → 万千愿景
        }
        Log.Info("[Amiya] archaic tooth: 牌组中没有思念，跳过原版流程");
        return false;
    }

    private static bool DustyTomePrefix(DustyTome __instance)
    {
        Player? player = __instance.Owner;
        if (player == null || player.Character is not AmiyaCharacter)
        {
            return true;
        }
        if (__instance.AncientCard == null)
        {
            Log.Info("[Amiya] dusty tome: 遗物没有候选远古牌，跳过原版流程");
            return false;
        }
        return true; // 官方流程发放"升级后的候选牌"（ITomeCard 已锁定为神圣复苏）
    }
}
