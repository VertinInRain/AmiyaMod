using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>预言显像（罕见）：每回合开始时，将一张存续先兆加入你的手牌（费用 1/0）。</summary>
public sealed class ProphecyImage : BaseAmiyaCard
{
    public ProphecyImage() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "预言显像"),
        ("description", "#每回合开始时，将一张绝望加入你的手牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ProphecyImagePower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>预言显像 Power。由 FormManagerPower 回合开始结算。</summary>
public sealed class ProphecyImagePower : CustomPowerModel
{
    public override string? CustomPackedIconPath => Amiya.Art.PlaceholderArt.Power("draw_cards_next_turn_power");
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "预言显像"),
        ("description", "每回合开始时，将一张绝望加入你的手牌。")
    };
}
