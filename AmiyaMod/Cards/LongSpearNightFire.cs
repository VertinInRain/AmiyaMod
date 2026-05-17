using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 长枪夜火 - 稀有攻击牌
/// 领袖 消耗手牌中所有非攻击牌，每消耗一张，对随机敌人造成9/12点伤�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class LongSpearNightFire : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(9m, ValueProp.Move)
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Leader };
    public LongSpearNightFire() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handPile = PileType.Hand.GetPile(Owner);
        var nonAttackCards = handPile.Cards.Where(c => c.Type != CardType.Attack && c != this).ToList();
        int count = nonAttackCards.Count;
        foreach (var card in nonAttackCards)
            await CardCmd.Exhaust(choiceContext, card);

        var enemies = CombatState.HittableEnemies.ToList();
        var rng = new System.Random();
        for (int i = 0; i < count && enemies.Count > 0; i++)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
                .Targeting(enemies[rng.Next(enemies.Count)]).Execute(choiceContext);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(3m); }
}