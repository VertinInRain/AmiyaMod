using System;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 祈愿：让"耗能超出剩余能量"的牌可以打出（清掉 EnergyCostTooHigh 标记）。
/// 真正的能量支付仍走原逻辑（SpendEnergy 内部 LoseEnergy 会夹到 0，不会变负）。
/// </summary>
[HarmonyPatch(typeof(PlayerCombatState), "HasEnoughResourcesFor")]
internal static class AmiyaWishCanPlayPatch
{
    private static void Postfix(PlayerCombatState __instance, CardModel card, ref UnplayableReason reason, ref bool __result)
    {
        try
        {
            var fm = FormManagerPower.Current;
            if (fm?.Owner == null || !fm.Owner.HasPower<WishPower>())
            {
                return;
            }
            if (card.Owner?.PlayerCombatState != __instance)
            {
                return;
            }
            reason &= ~UnplayableReason.EnergyCostTooHigh;
            __result = reason == UnplayableReason.None;
        }
        catch (Exception)
        {
            // 可打性判定不允许抛异常
        }
    }
}

/// <summary>
/// 祈愿：支付资源前记录超支赤字（cost - 剩余能量）。
/// SpendResources 的 async 方法体在执行前，能量尚未扣除，正是记录时机。
/// </summary>
[HarmonyPatch(typeof(CardModel), "SpendResources")]
internal static class AmiyaWishDeficitPatch
{
    private static void Prefix(CardModel __instance)
    {
        try
        {
            if (__instance.Owner?.Creature is not { } creature || !creature.HasPower<WishPower>())
            {
                return;
            }
            if (__instance.EnergyCost.CostsX)
            {
                return; // X 费牌不受影响
            }
            int cost = Math.Max(0, __instance.EnergyCost.GetWithModifiers(CostModifiers.All));
            int energy = __instance.Owner.PlayerCombatState.Energy;
            int deficit = cost - energy;
            if (deficit > 0)
            {
                WishPower.PendingDeficit += deficit;
            }
        }
        catch (Exception)
        {
            // 不允许破坏支付流程
        }
    }
}
