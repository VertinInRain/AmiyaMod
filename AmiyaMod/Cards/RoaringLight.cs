using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class RoaringLight : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(7m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
    public RoaringLight() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        int hits = IsUpgraded ? 9 : 7;
        for (int i = 0; i < hits; i++)
        {
            // 每击失去1点临时力量（参考Mangle → ManglePower → TemporaryStrengthPower）
            await PowerCmd.Apply<ManglePower>(cc, new[] { Owner.Creature }, 1m, Owner.Creature, this);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); }
}