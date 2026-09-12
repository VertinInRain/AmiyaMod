using System;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Amiya.Patches;

/// <summary>
/// 手牌上限减少：尘霾之冠 / 祈愿 叠加到 HandLimitReductionPower 层数。
/// 游戏的手牌上限是静态属性 CardPile.MaxCardsInHand => 10，没有 per-player 钩子，
/// 只能补丁静态 getter：阿米娅拥有该力量时按层数扣减（最多减到 0）。
/// </summary>
[HarmonyPatch(typeof(CardPile), "MaxCardsInHand", MethodType.Getter)]
internal static class AmiyaMaxHandPatch
{
    private static void Postfix(ref int __result)
    {
        try
        {
            var fm = FormManagerPower.Current;
            if (fm?.Owner != null && fm.Owner.GetPower<HandLimitReductionPower>() is { } reduction && reduction.Amount > 0)
            {
                __result = Math.Max(0, __result - (int)reduction.Amount);
            }
        }
        catch (Exception)
        {
            // 手牌上限计算不允许抛异常
        }
    }
}
