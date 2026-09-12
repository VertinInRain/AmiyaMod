using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Amiya.Cards;

/// <summary>魔王的祭器（罕见）：每次打出带有魔王词条的牌时，对所有敌人造成当前魔王之唤层数的伤害（费用 1/0）。</summary>
public sealed class DemonLordVessel : BaseAmiyaCard
{
    public DemonLordVessel() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "魔王的祭器"),
        ("description", "#每次打出带有魔王词条的牌时，对所有敌人造成当前魔王之唤层数的伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<DemonLordVesselPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}

/// <summary>魔王的祭器 Power。由 FormManagerPower.GainDemonLordCall 结算（伤害=当前魔王之唤层数）。</summary>
public sealed class DemonLordVesselPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/DemonLordVesselPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "魔王的祭器"),
        ("description", "每次打出带有魔王词条的牌时，对所有敌人造成当前魔王之唤层数的伤害。")
    };
}
