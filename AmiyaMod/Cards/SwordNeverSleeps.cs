using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;
/// <summary>剑不�?- 领袖 10/13伤害，下一张领袖牌耗能�?</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SwordNeverSleeps : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(10m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
    public SwordNeverSleeps() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
        await PowerCmd.Apply<SwordNeverSleepsPower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}