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
/// 盛怒旋�?- 罕见攻击�?
/// X�?对所有敌人造成3点伤害，获得3/5点临时力量，重放X�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class RageWhirlwind : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(3m, ValueProp.Move),
        new BlockVar(3m, ValueProp.Move) // 用CalculatedBlock存临时力量�?
    };

    public RageWhirlwind() 
        : base(-1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies) // -1 = X cost
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = cardPlay.Resources.EnergyValue; // X = 消耗的能量
        
        // 获得临时力量
        decimal strGain = 3m;
        await PowerCmd.Apply<StrengthPower>(choiceContext, new[] { Owner.Creature }, strGain * x, Owner.Creature, this);

        // 对所有敌人造成伤害 X �?
        for (int i = 0; i < x; i++)
        {
            foreach (var enemy in CombatState.HittableEnemies)
            {
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                    .FromCard(this)
                    .Targeting(enemy)
                    .Execute(choiceContext);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}