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
/// <summary>重返初识 - 感染 18/24伤害+18/24格挡，消耗手�?弃牌堆，�?打击4防御放入手牌</summary>
[Pool(typeof(AmiyaCardPool))]

public partial class ReturnToBeginning : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar> { new DamageVar(18m, ValueProp.Move), new BlockVar(18m, ValueProp.Move) };
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { AmiyaKeywords.Infection };
    public ReturnToBeginning() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override async Task OnPlay(PlayerChoiceContext cc, CardPlay cp)
    {
        var target = cp.Target; if (target == null) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(cc);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Move, cp);
        // 消耗手牌中所有其他牌
        var handCards = PileType.Hand.GetPile(Owner).Cards.Where(c => c != this).ToList();
        foreach (var c in handCards) await CardCmd.Exhaust(cc, c);
        // 消耗弃牌堆所有牌
        var discardCards = PileType.Discard.GetPile(Owner).Cards.ToList();
        foreach (var c in discardCards) await CardCmd.Exhaust(cc, c);
        // �?打击4防御放入手牌
        for (int i = 0; i < 4; i++)
        {
            var strike = CombatState.CreateCard<Strike>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, Owner);
        }
        for (int i = 0; i < 4; i++)
        {
            var defend = CombatState.CreateCard<Defend>(Owner);
            await CardPileCmd.AddGeneratedCardToCombat(defend, PileType.Hand, Owner);
        }
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(6m); DynamicVars.Block.UpgradeValueBy(6m); }
}