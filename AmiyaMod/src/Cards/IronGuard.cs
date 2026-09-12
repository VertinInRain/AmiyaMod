using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>铁卫（罕见）：每次进入近卫形态时，获得 3/4 层覆甲。</summary>
public sealed class IronGuard : BaseAmiyaCard
{
    public IronGuard() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "铁卫"),
        ("description", "#每次进入近卫形态时，获得 {IfUpgraded:show:4|3} 层覆甲。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IronGuardPower>(choiceContext, Owner!.Creature, IsUpgraded ? 4m : 3m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>铁卫 Power（层数=覆甲）。由 FormManagerPower.OnFormSwitched（进入近卫）结算。</summary>
public sealed class IronGuardPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/IronGuardPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "铁卫"),
        ("description", "每次进入近卫形态时，获得等量覆甲。")
    };
}
