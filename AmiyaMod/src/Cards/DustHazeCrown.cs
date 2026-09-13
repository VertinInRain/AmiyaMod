using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>尘霾之冠（稀有）：手牌上限 -2。每回合开始时，获得 1 点能量（升级后 2）。</summary>
public sealed class DustHazeCrown : BaseAmiyaCard
{
    public DustHazeCrown() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "尘霾之冠"),
        ("description", "#手牌上限减少2。每回合开始时，获得 -1-+2+ 点能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DustHazeCrownPower>(choiceContext, Owner!.Creature, IsUpgraded ? 2m : 1m, Owner.Creature, null, silent: true);
        await PowerCmd.Apply<HandLimitReductionPower>(choiceContext, Owner!.Creature, 2m, Owner.Creature, null, silent: true);
        await HandLimitHelper.DiscardDownToLimit(choiceContext, Owner!);
    }

    protected override void OnUpgrade()
    {
        // 能量 1 → 2（由 IsUpgraded 判定）
    }
}

/// <summary>尘霾之冠 Power：每回合开始时获得（层数）点能量。</summary>
public sealed class DustHazeCrownPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/DustHazeCrownPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "尘霾之冠"),
        ("description", "每回合开始时，获得能量（数量 = 层数）。")
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        if (player == Owner?.Player && Amount > 0)
        {
            await PlayerCmd.GainEnergy(Amount, player);
        }
    }
}

/// <summary>
/// 手牌上限变化（独立状态图标）：层数 = 手牌上限的 <b>净减少值</b>。
/// 尘霾之冠、祈愿把它加正（上限降低）；痛悼无垠把它减 2（上限提高，层数可为负）。
/// 层数正好为 0 时引擎会移除它（ShouldRemoveDueToAmount），即上限回到基准值。
/// </summary>
public sealed class HandLimitReductionPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/HandLimitReductionPower.png";
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 允许层数为负。游戏里"力量/敏捷"等可负状态走的就是这条路：
    /// 层数为负时角标直接显示负数（NPower 用 DisplayAmount.ToString() 填角标），
    /// 且 PowerCmd.ModifyAmount 只在层数正好为 0 时才移除状态。
    /// </summary>
    public override bool AllowNegative => true;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "手牌上限减少"),
        ("description", "手牌上限的变化（正数 = 减少，负数 = 提高）。")
    };
}

/// <summary>手牌上限规则：只要上限被削减/增加，就检测手牌数并丢弃末尾牌直到等于调整后的上限。</summary>
public static class HandLimitHelper
{
    /// <summary>
    /// 该玩家手牌上限的净减少层数：正数 = 上限降低，负数 = 上限提高，0 = 基准值。
    /// 只读该玩家自己身上的状态（多人下各算各的）。
    /// </summary>
    public static int NetReductionFor(Player? player)
        => player?.Creature?.GetPower<HandLimitReductionPower>() is { } p ? p.Amount : 0;

    /// <summary>该玩家手牌上限的减少量（0 或正数）。</summary>
    public static int ReductionFor(Player? player) => System.Math.Max(0, NetReductionFor(player));

    /// <summary>该玩家手牌上限的提高量（0 或正数，来自痛悼无垠）。</summary>
    public static int IncreaseFor(Player? player) => System.Math.Max(0, -NetReductionFor(player));

    /// <summary>该玩家自己的手牌上限（基准 10 − 净减少）。多人下各算各的。</summary>
    public static int LimitFor(Player? player)
        => System.Math.Max(0, Amiya.Patches.AmiyaMaxHandPatch.BaseLimit - NetReductionFor(player));

    public static async Task DiscardDownToLimit(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature == null)
        {
            return;
        }
        var hand = PileType.Hand.GetPile(player);
        // 按该玩家自己的上限计算（不用全局静态值，避免把别人的削减算到自己头上）
        int limit = LimitFor(player);
        while (hand.Cards.Count > limit)
        {
            var last = hand.Cards.LastOrDefault();
            if (last == null)
            {
                break;
            }
            await CardPileCmd.Add(last, PileType.Discard, CardPilePosition.Bottom);
        }
    }
}
