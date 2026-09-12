using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>湍流（罕见）：每次抽到打击、防御时，抽一张牌（费用 2/1）。</summary>
public sealed class Turbulence : BaseAmiyaCard
{
    public Turbulence() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "湍流"),
        ("description", "#每次抽到打击、防御时，抽一张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TurbulencePower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}

/// <summary>湍流 Power。由 FormManagerPower.AfterCardDrawn 结算。</summary>
public sealed class TurbulencePower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/TurbulencePower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "湍流"),
        ("description", "每次抽到打击、防御时，抽一张牌。")
    };
}
