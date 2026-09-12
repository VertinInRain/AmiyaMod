using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amiya.Cards;
using Amiya.Models;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Amiya.Patches;

/// <summary>
/// 先古遗物联动（仅阿米娅）：
///   古老牙齿（ArchaicTooth）→ 将卡组中的思念变化为先古牌万千愿景（保持升级状态）；
///   尘封魔典（DustyTome）→ 获得一张升级后的神圣复苏。
///
/// 双保险机制：
///   1) 官方接口：思念实现 ITranscendenceCard（BaseLib 会把 思念→万千愿景 注册进
///      ArchaicTooth 的升华映射表）；神圣复苏实现 ITomeCard（BaseLib 会把尘封魔典的
///      候选卡池限定为神圣复苏，事件展示与发放都固定为它）。
///   2) 本文件的手工 Harmony 前缀：覆盖两件遗物的 AfterObtained，直接执行阿米娅版效果，
///      防止原版流程找不到对应初始牌而空引用/随机发错卡。
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
        var player = __instance.Owner;
        Log.Info($"[Amiya] archaic tooth AfterObtained fired: owner={player != null}, isAmiya={player?.Character is AmiyaCharacter}");
        if (player == null || player.Character is not AmiyaCharacter)
        {
            return true; // 非阿米娅：原版流程
        }
        _ = HandleArchaicTooth(player);
        return false;
    }

    private static bool DustyTomePrefix(DustyTome __instance)
    {
        var player = __instance.Owner;
        Log.Info($"[Amiya] dusty tome AfterObtained fired: owner={player != null}, isAmiya={player?.Character is AmiyaCharacter}");
        if (player == null || player.Character is not AmiyaCharacter)
        {
            return true; // 非阿米娅：原版流程
        }
        _ = HandleDustyTome(player);
        return false;
    }

    private static async Task HandleArchaicTooth(Player player)
    {
        try
        {
            var card = player.Deck.Cards.FirstOrDefault(c => c is AmiyaFormSwitch);
            if (card == null)
            {
                Log.Info("[Amiya] archaic tooth: 牌组中没有思念，跳过");
                return;
            }
            bool upgraded = card.IsUpgraded;
            var ancient = card.Owner.RunState.CreateCard(ModelDb.Card<ThousandVisions>(), card.Owner);
            if (upgraded)
            {
                CardCmd.Upgrade(ancient);
            }
            await CardCmd.Transform(card, ancient);
            Log.Info($"[Amiya] archaic tooth: 思念 -> 万千愿景{(upgraded ? "+" : "")}");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] archaic tooth effect failed: {ex}");
        }
    }

    private static async Task HandleDustyTome(Player player)
    {
        try
        {
            var card = player.RunState.CreateCard(ModelDb.Card<SeeLight>(), player);
            CardCmd.Upgrade(card);
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
            Log.Info("[Amiya] dusty tome: 获得升级后的神圣复苏");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] dusty tome effect failed: {ex}");
        }
    }
}
