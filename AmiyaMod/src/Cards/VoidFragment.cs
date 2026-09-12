using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>虚空残片（罕见，虚无）：每回合第 1 张牌可以免费打出（费用 2/1）。</summary>
public sealed class VoidFragment : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Ethereal };

    public VoidFragment() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "虚空残片"),
        ("description", "#每回合第 1 张牌可以免费打出。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VoidFragmentPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1（虚无保留）
    }
}

/// <summary>
/// 虚空残片 Power：每回合第 1 张牌免费。官方 VoidFormPower 同款——
/// 计数挂在能力自身上（能力存在时才计，打出前的不算），回合开始归零；
/// 费用由 FormManagerPower.TryModifyEnergyCostInCombat 无状态查询 PlaysThisTurn &lt; 1。
/// </summary>
public sealed class VoidFragmentPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/VoidFragmentPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    /// <summary>本回合已打出的牌数（不计自动打出、按系列计）。</summary>
    public int PlaysThisTurn { get; set; }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "虚空残片"),
        ("description", "每回合第 1 张牌可以免费打出。")
    };

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card?.Owner?.Creature == Owner && !cardPlay.IsAutoPlay && cardPlay.IsLastInSeries)
        {
            PlaysThisTurn++;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner?.Player)
        {
            PlaysThisTurn = 0;
        }
        return Task.CompletedTask;
    }
}
