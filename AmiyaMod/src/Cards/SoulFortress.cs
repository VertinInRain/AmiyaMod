using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>灵魂堡垒（罕见）：成功形态转换后，获得 3/4 点格挡。</summary>
public sealed class SoulFortress : BaseAmiyaCard
{
    public SoulFortress() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "灵魂堡垒"),
        ("description", "#成功形态转换后，获得 {IfUpgraded:show:4|3} 点格挡。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SoulFortressPower>(choiceContext, Owner!.Creature, IsUpgraded ? 4m : 3m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>灵魂堡垒 Power（层数=格挡）。由 FormManagerPower.OnFormSwitched 结算。</summary>
public sealed class SoulFortressPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/SoulFortressPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "灵魂堡垒"),
        ("description", "成功形态转换后，获得等量格挡。")
    };
}
