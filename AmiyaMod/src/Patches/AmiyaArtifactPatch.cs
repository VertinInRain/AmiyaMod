using System;
using System.Reflection;
using Amiya.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Amiya.Patches;

/// <summary>
/// 人工制品（ArtifactPower）只应拦下"负面"的状态层数变化。
///
/// 问题：手牌上限状态（HandLimitReductionPower）允许负层数来表示"上限提高"，
/// 而引擎的 PowerModel.GetTypeForAmount 会把「Counter 且 AllowNegative 且层数为负」
/// 一律归类成 PowerType.Debuff（"负力量"这类状态走的就是这条通用规则），
/// 于是痛悼无垠的上限 +2 会被人工制品当成负面状态吞掉，还会白扣一层人工制品。
///
/// 处理：只针对这一个状态、且只在"层数减少"（= 上限提高，对玩家是收益）时放行；
/// 其余情况（尘霾之冠/祈愿的正面削减）保持原版行为。
///
/// 注册方式与 AncientRelicPatch 一致：手工 Patch + 日志确认（本环境下特性式注册会被静默跳过）。
/// </summary>
internal static class AmiyaArtifactPatch
{
    public static void Apply(Harmony harmony)
    {
        try
        {
            MethodInfo? original = AccessTools.Method(typeof(ArtifactPower), "TryModifyPowerAmountReceived");
            if (original == null)
            {
                Log.Error("[Amiya] ArtifactPower.TryModifyPowerAmountReceived 反射解析失败，手牌上限提高可能被人工制品拦截");
                return;
            }
            harmony.Patch(original, prefix: new HarmonyMethod(typeof(AmiyaArtifactPatch), nameof(Prefix)));
            Log.Info("[Amiya] patched ArtifactPower.TryModifyPowerAmountReceived -> 手牌上限提高不被人工制品拦截");
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] patching ArtifactPower failed: {ex}");
        }
    }

    // ReSharper disable once InconsistentNaming
    private static bool Prefix(PowerModel canonicalPower, decimal amount, ref decimal modifiedAmount, ref bool __result)
    {
        if (canonicalPower is not HandLimitReductionPower || amount >= 0m)
        {
            return true; // 交给原版逻辑
        }

        // 手牌上限提高：原样放行，并声明"未被本次拦截"（避免人工制品消耗层数）
        modifiedAmount = amount;
        __result = false;
        return false;
    }
}
