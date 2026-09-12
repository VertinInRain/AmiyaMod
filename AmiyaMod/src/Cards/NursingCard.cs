using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>疗养特供卡（罕见）：每打出一张感染牌，获得 1 点能量（费用 2/1）。</summary>
public sealed class NursingCard : BaseAmiyaCard
{
    public NursingCard() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "疗养特供卡"),
        ("description", "#每打出一张感染牌，获得 1 点能量。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NursingCardPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
    }
}

/// <summary>疗养特供卡 Power。由 FormManagerPower.AfterCardPlayed 结算。</summary>
public sealed class NursingCardPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => Amiya.Art.PlaceholderArt.Power("ritual_power");
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "疗养特供卡"),
        ("description", "每打出一张感染牌，获得 1 点能量。")
    };
}
