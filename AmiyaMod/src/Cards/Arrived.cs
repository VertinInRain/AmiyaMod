using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Amiya.Cards;

/// <summary>
/// 已至（稀有，魔王，3 费，升级前后均 3 费）：
/// 每次打出、消耗或丢弃一张牌（牌的效果结算完）后，若手牌数低于
/// min(阈值, 手牌上限)，抽牌补到该数量。阈值 = 5（升级后 6）。
/// </summary>
public sealed class Arrived : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public Arrived() : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "已至"),
        ("description", "#尽可能使手牌数量不低于 {IfUpgraded:show:6|5}。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal threshold = IsUpgraded ? 6m : 5m;
        await PowerCmd.Apply<ArrivedPower>(choiceContext, Owner!.Creature, threshold, Owner.Creature, null, silent: true);
        // 打出这张牌本身也会减少手牌，立即补抽一次
        await ArrivedPower.RefillHand(choiceContext, Owner, threshold);
    }

    protected override void OnUpgrade()
    {
        // 费用不变（升级前后均 3 费），阈值 5 → 6 由 IsUpgraded 决定
    }
}

/// <summary>已至 Power：打出/消耗/丢弃牌后，手牌不足 min(层数, 手牌上限) 时自动补抽到该数量。</summary>
public sealed class ArrivedPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/ArrivedPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "已至"),
        ("description", "每次打出、消耗或丢弃牌后，若手牌数低于（层数）与手牌上限的较小值，抽牌补到该数量。")
    };

    /// <summary>按阈值补抽：目标 = min(阈值, 手牌上限)，手牌不足则抽到目标。</summary>
    public static async Task RefillHand(PlayerChoiceContext choiceContext, Player player, decimal threshold)
    {
        if (player?.Creature == null)
        {
            return;
        }
        int target = System.Math.Min((int)threshold, System.Math.Max(0, CardPile.MaxCardsInHand));
        var hand = PileType.Hand.GetPile(player);
        int deficit = target - hand.Cards.Count;
        if (deficit > 0)
        {
            await CardPileCmd.Draw(choiceContext, deficit, player);
        }
    }

    private Task Refill(PlayerChoiceContext choiceContext, CardModel? card)
    {
        // 只在玩家回合内补抽（敌方回合/回合结束结算不触发）
        if (card == null || card.Owner?.Creature != Owner || Owner?.Player == null
            || Owner.CombatState?.CurrentSide != CombatSide.Player)
        {
            return Task.CompletedTask;
        }
        return RefillHand(choiceContext, Owner.Player, Amount);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        // 回合开始抽完 5 张后补抽到阈值（升级后 5 → 6 会多补 1 张）
        if (player == Owner?.Player)
        {
            await RefillHand(choiceContext, player, Amount);
        }
    }

    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayedLate(choiceContext, cardPlay);
        await Refill(choiceContext, cardPlay.Card);
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        await base.AfterCardDiscarded(choiceContext, card);
        await Refill(choiceContext, card);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        // 回合结束的虚无自动消耗不触发（那是收尾结算，不是牌效果）
        if (!causedByEthereal)
        {
            await Refill(choiceContext, card);
        }
    }
}
