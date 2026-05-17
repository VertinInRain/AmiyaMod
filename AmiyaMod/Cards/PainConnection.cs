using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 痛觉相连 - 罕见攻击�?
/// 去除自己和目标敌人所有格挡，给予双方�?层易伤，造成20/28点伤�?消�?
/// 参�? Expose (去除格挡)
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class PainConnection : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(20m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Exhaust };

    public PainConnection() 
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 去除自己的格�?
        await CreatureCmd.LoseBlock(Owner.Creature, Owner.Creature.Block);
        // 去除目标的格�?
        await CreatureCmd.LoseBlock(target, target.Block);

        // 给双方各1层易�?
        await PowerCmd.Apply<VulnerablePower>(choiceContext, new[] { Owner.Creature }, 1m, Owner.Creature, this);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, new[] { target }, 1m, Owner.Creature, this);

        // 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(8m);
    }
}