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
/// 苦难摇篮 - 罕见攻击�?
/// 固有 对所有敌人造成8点伤害并给予它们两层虚弱 消�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class SufferingCradle : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(8m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Innate, CardKeyword.Exhaust };

    public SufferingCradle() 
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
            await PowerCmd.Apply<WeakPower>(choiceContext, new[] { enemy }, 2m, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 伤害不变，效果不�?
    }
}