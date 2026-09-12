using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
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

/// <summary>
/// 平凡亦是喜乐（罕见，0 费）：可多次升级（最多 +10，标题自动显示 平凡亦是喜乐+1/2/.../10）。
/// 升级 k 次后打出：所有打击、防御牌数值增加 1+2k 点；抽 1+2k 张牌；
/// 并获得"溢流消耗"状态：每次抽牌时，超出手牌上限的牌会被直接消耗。
/// </summary>
public sealed class OrdinaryJoy : BaseAmiyaCard
{
    public override int MaxUpgradeLevel => 10;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            var vars = new List<DynamicVar>();
            vars.Add(new UpgradeLevelVar("V", card => 1m + 2m * card.CurrentUpgradeLevel));
            vars.Add(new UpgradeLevelVar("W", card => 1m + 2m * card.CurrentUpgradeLevel));
            return vars;
        }
    }

    public OrdinaryJoy() : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "平凡亦是喜乐"),
        // 文本顺序按作者要求：数值增加 → 溢流消耗 → 抽牌
        ("description", "#所有打击、防御牌数值增加 !V! 点。\n每次抽牌时，超出手牌上限的牌会被直接消耗。\n抽 !W! 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal amount = 1m + 2m * CurrentUpgradeLevel;
        // 先获得状态，再抽牌：本次抽牌的溢出也会被消耗
        await PowerCmd.Apply<OrdinaryJoyPower>(choiceContext, Owner!.Creature, amount, Owner.Creature, null, silent: true);
        await PowerCmd.Apply<DrawOverflowExhaustPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
        await CardPileCmd.Draw(choiceContext, amount, Owner!);
    }

    protected override void OnUpgrade()
    {
        // 升级不改变费用（始终 0 费）；数值随 CurrentUpgradeLevel 自动提升
    }
}

/// <summary>平凡亦是喜乐 Power：打击伤害 +层数、防御格挡 +层数（层数 = 打出时 1+2k，可叠加）。</summary>
public sealed class OrdinaryJoyPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/OrdinaryJoyPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "平凡亦是喜乐"),
        ("description", "所有打击、防御牌数值增加（增加量 = 层数）。")
    };

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (dealer == Owner && cardSource?.Tags.Contains(CardTag.Strike) == true)
        {
            return Amount;
        }
        return 0m;
    }

    public override decimal ModifyBlockAdditive(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == Owner && cardSource?.Tags.Contains(CardTag.Defend) == true)
        {
            return Amount;
        }
        return 0m;
    }
}

/// <summary>溢流消耗（状态）：每次抽牌时，超出手牌上限的牌会被直接消耗（由 AmiyaDrawOverflowPatch 实现）。</summary>
public sealed class DrawOverflowExhaustPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/DrawOverflowExhaustPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "溢流消耗"),
        ("description", "每次抽牌时，超出手牌上限的牌会被直接消耗。")
    };
}
