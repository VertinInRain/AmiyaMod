using System;
using System.Collections.Generic;
using System.Linq;
using Amiya.Cards;
using Amiya.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Patches;

/// <summary>
/// 手牌上限：尘霾之冠 / 祈愿 / 痛悼无垠 全部叠在同一个状态 HandLimitReductionPower 的层数上
/// （正数 = 上限降低，负数 = 上限提高）。
/// 游戏的手牌上限是静态属性 <c>CardPile.MaxCardsInHand => 10</c>，没有 per-player 钩子。
///
/// 【多人同步的关键】静态 getter 拿不到"哪个玩家"，最早的实现是用一个静态"当前抽牌玩家"栈
/// 在 DrawInternal 前后 push/pop：但 DrawInternal 内部**每个 await 之后都会重新读一次上限**，
/// 而两次抽牌交错时（另一边先结束、先出栈）栈顶就变成了另一个玩家 —— 两端交错顺序不同，
/// 读到的上限就不同，抽出的张数也不同，于是报状态分歧（实测：朋友那一侧上限 8，
/// 主机这边却按 10 抽，手牌 10 vs 8、抽牌堆 8 vs 10；若手牌已满还会一边洗牌一边不洗牌）。
///
/// 现在的做法是「上下文无关」的，两端一定算出同样的结果：
///   1. 本 getter 只把上限**抬高**到本场战斗里所有玩家上限的最大值（CeilingInCombat）——
///      只依赖同步状态，用来给"上限提高"（痛悼无垠）留出空间；
///   2. 按各自上限抽牌由 AmiyaDrawLimitPatch 在 DrawInternal 入口按 player 参数裁剪张数
///      （player 是参数，天然属于某个人，和时序无关）。
/// </summary>
[HarmonyPatch(typeof(CardPile), "MaxCardsInHand", MethodType.Getter)]
internal static class AmiyaMaxHandPatch
{
    /// <summary>
    /// 手牌上限基准值。原版就是常量 <c>CardPile.MaxCardsInHand => 10</c>（反编译确认）。
    /// 这里不读那个 getter 自己——否则会踩到"静态初始化里访问被补丁的 getter"的递归。
    /// </summary>
    public const int BaseLimit = 10;

    private static int _lastLoggedCeiling = -1;

    private static void Postfix(ref int __result)
    {
        try
        {
            int ceiling = CeilingInCombat;
            if (ceiling > __result)
            {
                __result = ceiling;
            }
        }
        catch (Exception)
        {
            // 手牌上限计算不允许抛异常
        }
    }

    /// <summary>
    /// 本场战斗中所有玩家手牌上限的最大值（= 基准 10 + 最大的"上限提高"量）。
    /// 只读同步状态，不含任何"当前谁在抽牌"的上下文，因此两端结果必然一致。
    /// 没人提高上限时就是 10，此时引擎侧的行为与原版完全一致。
    /// </summary>
    public static int CeilingInCombat
    {
        get
        {
            int max = BaseLimit;
            CombatState? state = CombatManager.Instance?.DebugOnlyGetState();
            if (state != null)
            {
                foreach (Player p in state.Players)
                {
                    int limit = HandLimitHelper.LimitFor(p);
                    if (limit > max)
                    {
                        max = limit;
                    }
                }
            }
            if (max != _lastLoggedCeiling)
            {
                _lastLoggedCeiling = max;
                Log.Info($"[Amiya] 手牌上限天花板 = {max}（战斗中所有玩家上限的最大值）");
            }
            return max;
        }
    }
}

/// <summary>
/// 按玩家自己的上限裁剪"这次要抽几张"。
/// 放在 DrawInternal 的入口：这里 player 是参数，谁抽牌就按谁的上限算，
/// 和两端的事件交错顺序无关，因此不会再产生同步分歧。
/// </summary>
[HarmonyPatch(typeof(CardPileCmd), "DrawInternal")]
internal static class AmiyaDrawLimitPatch
{
    private static void Prefix(Player player, ref decimal count)
    {
        try
        {
            if (player?.Creature == null || count <= 0m)
            {
                return;
            }
            int limit = HandLimitHelper.LimitFor(player);
            int ceiling = AmiyaMaxHandPatch.CeilingInCombat;
            if (limit >= ceiling)
            {
                // 该玩家的上限就是引擎自己用的上限（没被削减）→ 交给引擎，
                // 保持原版行为完全不变（包括"手牌已满"的提示气泡）。
                return;
            }
            int hand = PileType.Hand.GetPile(player).Cards.Count;
            decimal room = Math.Max(0, limit - hand);
            if (count > room)
            {
                Log.Info($"[Amiya] 手牌上限：{player.NetId} 上限={limit} 手牌={hand}，本次抽牌 {count} → {room}");
                count = room;
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] draw limit patch failed: " + ex);
        }
    }
}

/// <summary>
/// 抽牌之外的"直接塞进手牌"（各种生成/回收卡牌的效果）也要按**各自**的手牌上限处理：
/// 引擎自己的判定用的是全局上限（= 本场最高值），所以当某个玩家自己的上限更低时，
/// 一张效果牌会绕过他自己的上限挤进手牌。这里在 Add 的入口把目标牌堆改成弃牌堆，
/// 和原版"手牌已满时改入弃牌堆"的处理一致。
///
/// 只在"该玩家自己的上限低于引擎上限"时才动手（即有人吃了手牌上限削减/别人提高了上限），
/// 其它情况一律交给引擎，原版行为（含"手牌已满"提示）不受影响。
/// </summary>
internal static class AmiyaHandFullRedirect
{
    internal static void Redirect(Player? owner, ref PileType pileType)
    {
        try
        {
            if (pileType != PileType.Hand || owner?.Creature == null)
            {
                return;
            }
            int limit = HandLimitHelper.LimitFor(owner);
            if (limit >= AmiyaMaxHandPatch.CeilingInCombat)
            {
                return; // 引擎的判定就是对的
            }
            int hand = PileType.Hand.GetPile(owner).Cards.Count;
            if (hand >= limit)
            {
                Log.Info($"[Amiya] 手牌上限：{owner.NetId} 手牌={hand} 已达自己的上限 {limit}，改入弃牌堆");
                pileType = PileType.Discard;
            }
        }
        catch (Exception ex)
        {
            Log.Error("[Amiya] hand full redirect failed: " + ex);
        }
    }
}

/// <summary>Add(CardModel, PileType, ...)：单张卡直接进手牌时按牌主自己的上限判断。</summary>
[HarmonyPatch(typeof(CardPileCmd), "Add", new Type[] { typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
internal static class AmiyaHandFullRedirectSinglePatch
{
    private static void Prefix(CardModel card, ref PileType newPileType)
        => AmiyaHandFullRedirect.Redirect(card?.Owner, ref newPileType);
}

/// <summary>Add(IEnumerable&lt;CardModel&gt;, PileType, ...)：多张卡同一个主人，按第一张的主人判断。</summary>
[HarmonyPatch(typeof(CardPileCmd), "Add", new Type[] { typeof(IEnumerable<CardModel>), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
internal static class AmiyaHandFullRedirectManyPatch
{
    private static void Prefix(IEnumerable<CardModel> cards, ref PileType newPileType)
        => AmiyaHandFullRedirect.Redirect(cards?.FirstOrDefault()?.Owner, ref newPileType);
}
