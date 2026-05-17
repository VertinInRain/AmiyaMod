using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>哀恸共�?- 感染 获得与已损失生命值等量的护盾</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class GriefEmpathy : BaseAmiyaCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
    public GriefEmpathy() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        decimal lostHp = Owner.Creature.MaxHp - Owner.Creature.CurrentHp;
        if (lostHp > 0)
            await CreatureCmd.GainBlock(Owner.Creature, lostHp, ValueProp.Move, cardPlay);
    }
    protected override void OnUpgrade() { UpgradeStarCostBy(-1); }
}