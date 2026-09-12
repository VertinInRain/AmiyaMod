using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>蔓延（稀有）：每当你触发形态转换时，抽 1 张牌（费用 1/0）。</summary>
public sealed class Spread : BaseAmiyaCard
{
    public Spread() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "蔓延"),
        ("description", "#每当你触发形态转换时，抽 1 张牌。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SpreadPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>蔓延 Power。由 FormManagerPower.OnFormSwitched 结算。</summary>
public sealed class SpreadPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/SpreadPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "蔓延"),
        ("description", "每当你触发形态转换时，抽 1 张牌。")
    };
}
