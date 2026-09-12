using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>哀恸共情（普通，感染）：获得与已损失生命值等量的格挡（费用 1/0）。</summary>
public sealed class GriefEmpathy : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.Infection;

    public GriefEmpathy() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "哀恸共情"),
        ("description", "#获得与已损失生命值等量的格挡。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal loss = Owner!.Creature.MaxHp - Owner.Creature.CurrentHp;
        await CreatureCmd.GainBlock(Owner.Creature, loss, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 → 0
    }
}
