using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>魔王的旗帜（罕见）：每打出一张带有魔王词条的牌，抽 1/2 张牌。</summary>
public sealed class DemonLordBanner : BaseAmiyaCard
{
    public DemonLordBanner() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "魔王的旗帜"),
        ("description", "#每打出一张带有魔王词条的牌，抽 {IfUpgraded:show:2|1} 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DemonLordBannerPower>(choiceContext, Owner!.Creature, IsUpgraded ? 2m : 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>魔王的旗帜 Power（层数=抽牌数）。由 FormManagerPower.GainDemonLordCall 结算。</summary>
public sealed class DemonLordBannerPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/DemonLordBannerPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "魔王的旗帜"),
        ("description", "每打出一张带有魔王词条的牌，抽等量牌。")
    };
}
