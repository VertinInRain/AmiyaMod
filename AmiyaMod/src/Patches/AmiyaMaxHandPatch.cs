using System;
using System.Linq;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Amiya.Patches;

/// <summary>
/// 手牌上限减少：尘霾之冠 / 祈愿 叠加到 HandLimitReductionPower 层数。
/// 游戏的手牌上限是静态属性 CardPile.MaxCardsInHand => 10，没有 per-player 钩子，
/// 只能补丁静态 getter：阿米娅拥有该力量时按层数扣减（最多减到 0）。
///
/// 多人安全：不能只按"本地玩家"的层数算（两端会各按自己玩家扣减 → 抽牌数分歧掉线），
/// 改为取所有玩家中该状态的最大层数——两端遍历到的玩家集合一致，结果必然一致。
/// </summary>
[HarmonyPatch(typeof(CardPile), "MaxCardsInHand", MethodType.Getter)]
internal static class AmiyaMaxHandPatch
{
    private static void Postfix(ref int __result)
    {
        try
        {
            var state = FormManagerPower.Current?.Owner?.CombatState;
            if (state == null)
            {
                return;
            }
            int reduction = state.Players
                .Select(p => p.Creature.GetPower<HandLimitReductionPower>())
                .Where(r => r != null && r.Amount > 0)
                .Select(r => (int)r!.Amount)
                .DefaultIfEmpty(0)
                .Max();
            if (reduction > 0)
            {
                __result = Math.Max(0, __result - reduction);
            }
        }
        catch (Exception)
        {
            // 手牌上限计算不允许抛异常
        }
    }
}
