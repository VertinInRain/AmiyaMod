using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>剑不眠（罕见，领袖）：造成 10/13 点伤害。下一张领袖牌耗能变为 0（本场战斗内，Q7；官方 Veilpiercer 同款实现）。</summary>
public sealed class SwordNeverSleeps : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(10m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public SwordNeverSleeps() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "剑不眠"),
        ("description", "#造成 !D! 点伤害。下一张领袖牌耗能变为 0。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
        // 可见标识：施加"剑不眠"能力徽章（1 层），下张领袖牌免费
        await PowerCmd.Apply<SwordNeverSleepsPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 10 → 13
    }
}

/// <summary>
/// 剑不眠 Power（Counter=可见徽章标识）：下一张领袖牌 0 费。
/// 官方 VeilpiercerPower 同款：费用钩子无状态（领袖牌在手中→0费），
/// 实际打出领袖牌时 BeforeCardPlayed 减层（1→0 自动移除）。
/// </summary>
public sealed class SwordNeverSleepsPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/SwordNeverSleepsPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "剑不眠"),
        ("description", "下一张领袖牌可以免费打出。")
    };

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (card.Owner.Creature != Owner)
        {
            return false;
        }
        if (card is not BaseAmiyaCard amiyaCard || !amiyaCard.HasAmiyaTag(AmiyaTag.Leader))
        {
            return false;
        }
        if (card.Pile?.Type is not (PileType.Hand or PileType.Play))
        {
            return false;
        }
        modifiedCost = 0m;
        return true;
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature == Owner
            && cardPlay.Card is BaseAmiyaCard ac
            && ac.HasAmiyaTag(AmiyaTag.Leader)
            && cardPlay.Card.Pile?.Type is (PileType.Hand or PileType.Play))
        {
            await PowerCmd.Decrement(this);
        }
    }
}
