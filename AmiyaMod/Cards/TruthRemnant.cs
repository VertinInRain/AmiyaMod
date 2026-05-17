using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 真理残余 - 普通攻击牌
/// 造成4/5伤害，获得与所造成的伤害相等的格挡，将一张此牌的复制放入弃牌�?
/// 参�? Anger (复制品加入弃牌堆), Fisticuffs (伤害转格�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class TruthRemnant : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(4m, ValueProp.Move)
    };

    public TruthRemnant() 
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 造成伤害并获得等量格挡（伤害�?格挡值）
        var dmgValue = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(dmgValue)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);

        await CreatureCmd.GainBlock(Owner.Creature, dmgValue, ValueProp.Move, cardPlay);

        // 将一张复制品加入弃牌�?
        var copy = CombatState.CreateCard<TruthRemnant>(Owner);
        if (IsUpgraded)
            copy.UpgradeInternal();
        await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Bottom, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
    }
}