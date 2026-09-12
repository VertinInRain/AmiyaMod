using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>深埋地底（稀有）：每当你的打击命中敌人时，本回合减去该敌人 1/2 点力量。</summary>
public sealed class BuriedUnderground : BaseAmiyaCard
{
    public BuriedUnderground() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "深埋地底"),
        ("description", "#每当你的打击命中敌人时，本回合减去该敌人 {IfUpgraded:show:2|1} 点力量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BuriedUndergroundPower>(choiceContext, Owner!.Creature, IsUpgraded ? 2m : 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>深埋地底 Power：你的打击命中（每段）→ 该敌人本回合力量下降（官方 DarkShacklesPower 机制）。</summary>
public sealed class BuriedUndergroundPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => Amiya.Art.PlaceholderArt.Power("plating_power");
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "深埋地底"),
        ("description", "每当你的打击命中敌人时，本回合减去该敌人等量力量。")
    };

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        await base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
        if (dealer == Owner && cardSource?.Tags.Contains(CardTag.Strike) == true && target != Owner)
        {
            await PowerCmd.Apply<DarkShacklesPower>(choiceContext, target, Amount, Owner, null, silent: true);
        }
    }
}
