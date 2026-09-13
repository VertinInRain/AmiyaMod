using System;
using System.Collections.Generic;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Amiya.Patches;

/// <summary>
/// 手牌上限减少：尘霾之冠 / 祈愿 叠加到 HandLimitReductionPower 层数。
/// 游戏的手牌上限是静态属性 CardPile.MaxCardsInHand => 10，没有 per-player 钩子，
/// 只能补丁静态 getter。
///
/// 多人安全 + 按玩家分别计算：静态 getter 本身拿不到"哪个玩家"，
/// 因此由抽牌前缀（AmiyaDrawOverflowPatch）在抽牌期间登记"当前抽牌玩家"，
/// 期间按该玩家自己的层数扣减；其余时刻一律返回基准值（10）。
/// 这样每个玩家按自己的上限抽牌，且两端计算结果一致，不会产生同步分歧。
/// </summary>
[HarmonyPatch(typeof(CardPile), "MaxCardsInHand", MethodType.Getter)]
internal static class AmiyaMaxHandPatch
{
    /// <summary>手牌上限基准值（游戏常量 10；静态初始化时无上下文，即原值）。</summary>
    public static readonly int BaseLimit = CardPile.MaxCardsInHand;

    // 抽牌可能嵌套（抽牌触发的钩子再次抽牌），用栈保存上下文
    private static readonly List<Player?> _drawStack = new();

    public static void PushDrawContext(Player player) => _drawStack.Add(player);

    public static void PopDrawContext()
    {
        if (_drawStack.Count > 0)
        {
            _drawStack.RemoveAt(_drawStack.Count - 1);
        }
    }

    public static Player? CurrentDrawPlayer => _drawStack.Count > 0 ? _drawStack[_drawStack.Count - 1] : null;

    private static void Postfix(ref int __result)
    {
        try
        {
            if (CurrentDrawPlayer is { } player)
            {
                // 净削减 = 削减（尘霾之冠/祈愿）− 增加（痛悼无垠）；负值表示上限提高
                int net = HandLimitHelper.NetReductionFor(player);
                if (net != 0)
                {
                    __result = Math.Max(0, __result - net);
                }
            }
        }
        catch (Exception)
        {
            // 手牌上限计算不允许抛异常
        }
    }
}
