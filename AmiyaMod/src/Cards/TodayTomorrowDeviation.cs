using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>今时明日的偏差（罕见）：每当你打出一张攻击牌，就在下回合获得 5/7 点格挡。</summary>
public sealed class TodayTomorrowDeviation : BaseAmiyaCard
{
    public TodayTomorrowDeviation() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "今时明日的偏差"),
        ("description", "#每当你打出一张攻击牌，就在下回合获得 {IfUpgraded:show:7|5} 点格挡。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<TodayTomorrowDeviationPower>(choiceContext, Owner!.Creature, IsUpgraded ? 7m : 5m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>今时明日的偏差 Power（层数=每张攻击牌的下回合格挡）。由 FormManagerPower 结算。</summary>
public sealed class TodayTomorrowDeviationPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/TodayTomorrowDeviationPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "今时明日的偏差"),
        ("description", "每当你打出一张攻击牌，就在下回合获得等量格挡。")
    };
}
