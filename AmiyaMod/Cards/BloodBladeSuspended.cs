using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>血刃高�?- 墓园 32/42伤害，斩杀�?3/4最大生�?消�?/summary>
[Pool(typeof(AmiyaCardPool))]

public partial class BloodBladeSuspended : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(32m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Graveyard, CardKeyword.Exhaust };
    public BloodBladeSuspended() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
        if (target.IsDead)
            await CreatureCmd.GainMaxHp(Owner.Creature, IsUpgraded ? 4 : 3);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(10m); }
}