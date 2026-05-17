using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

[Pool(typeof(AmiyaCardPool))]
public partial class EmotionAbsorption : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(7m, ValueProp.Move), new BlockVar(3m, ValueProp.Move) };
    public EmotionAbsorption() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);

        // 统计敌人debuff数量
        int debuffs = 0;
        foreach (var p in target.Powers)
            if (p.Type == PowerType.Debuff) debuffs++;

        if (debuffs > 0)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue * debuffs, ValueProp.Move, cp);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2m); DynamicVars.Block.UpgradeValueBy(1m); }
}