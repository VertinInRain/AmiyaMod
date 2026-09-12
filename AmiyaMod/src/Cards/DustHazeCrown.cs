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

/// <summary>手牌上限减少（独立状态图标）：层数 = 手牌上限减少值。尘霾之冠与祈愿都会叠加它。</summary>
public sealed class HandLimitReductionPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => Amiya.Art.PlaceholderArt.Power("no_draw_power");
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "手牌上限减少"),
        ("description", "手牌上限减少（减少值 = 层数）。")
    };
}

/// <summary>手牌上限规则：只要上限被削减，就检测手牌数并丢弃末尾牌直到等于削减后的上限。</summary>
public static class HandLimitHelper
{
    public static async Task DiscardDownToLimit(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature == null)
        {
            return;
        }
        var hand = PileType.Hand.GetPile(player);
        // 注意：CardPile.MaxCardsInHand 已经过 AmiyaMaxHandPatch 扣减（10 - 层数），不要再减一次！
        int limit = System.Math.Max(0, CardPile.MaxCardsInHand);
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
