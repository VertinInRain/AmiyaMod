using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>长枪夜火（稀有，领袖）：消耗手牌中所有非攻击牌，每消耗一张对随机敌人造成 9 点伤害。升级后获得保留。</summary>
public sealed class LongSpearNightFire : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(9m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public LongSpearNightFire() : base(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "长枪夜火"),
        ("description", "#消耗手牌中所有非攻击牌，每消耗一张对随机敌人造成 !D! 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var nonAttacks = PileType.Hand.GetPile(Owner!).Cards.Where(c => c.Type != CardType.Attack).ToList();
        foreach (var card in nonAttacks)
        {
            await CardCmd.Exhaust(choiceContext, card);
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this, cardPlay)
                .TargetingRandomOpponents(CombatState)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 伤害不变（9），升级获得保留
        AddKeyword(CardKeyword.Retain);
    }
}
