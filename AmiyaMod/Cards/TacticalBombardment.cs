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

/// <summary>
/// 战术轰炸 - 罕见攻击�?
/// 对敌方全体造成7/9点伤�?
/// 若处于近卫形态，额外造成一�?
/// 若处于术士形态，给予1/2层易�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class TacticalBombardment : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(7m, ValueProp.Move)
    };

    public TacticalBombardment() 
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var formManager = Owner.Creature.GetPower<FormManagerPower>();
        int currentForm = formManager?.CurrentForm ?? 0;

        // 对所有敌人造成伤害
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(enemy)
                .Execute(choiceContext);
        }

        // 近卫形态：额外造成一�?
        if (currentForm == 0)
        {
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(enemy)
                    .Execute(choiceContext);
            }
        }

        // 术士形态：给予易伤
        if (currentForm == 1)
        {
            int vulnAmount = IsUpgraded ? 2 : 1;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, new[] { enemy }, vulnAmount, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}