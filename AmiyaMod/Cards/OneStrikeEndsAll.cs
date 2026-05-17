using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 一断皆�?- 普通攻击牌
/// 对敌方全体造成6/9点伤害，若敌人意图不是攻击，造成的伤害翻�?
/// 参�? GoForTheEyes (判断敌人意图)
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class OneStrikeEndsAll : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(6m, ValueProp.Move)
    };

    public OneStrikeEndsAll() 
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = CombatState.HittableEnemies;
        foreach (var enemy in enemies)
        {
            decimal dmg = DynamicVars.Damage.BaseValue;
            
            // 若敌人意图不是攻击，伤害翻�?
            if (enemy.Monster != null && !enemy.Monster.IntendsToAttack)
            {
                dmg *= 2m;
            }

            await DamageCmd.Attack(dmg)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}