using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>燎原（罕见）：每次触发形态转换时，获得 1/2 点临时力量。</summary>
public sealed class Wildfire : BaseAmiyaCard
{
    public Wildfire() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "燎原"),
        ("description", "#每次触发形态转换时，获得 {IfUpgraded:show:2|1} 点临时力量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WildfirePower>(choiceContext, Owner!.Creature, IsUpgraded ? 2m : 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>燎原 Power（层数=每次转换获得的临时力量）。由 FormManagerPower.OnFormSwitched 结算。</summary>
public sealed class WildfirePower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/WildfirePower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "燎原"),
        ("description", "每次触发形态转换时，获得等量临时力量。")
    };
}
