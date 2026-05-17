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
/// 风暴突击 - 普通攻击牌
/// 感染 造成8点伤害，�?/3张牌
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class StormAssault : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(8m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { AmiyaKeywords.Infection };

    public StormAssault() 
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .Execute(choiceContext);

        // �?张牌（升级后3张）
        int drawCount = IsUpgraded ? 3 : 2;
        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
    }

    protected override void OnUpgrade()
    {
        // 伤害不变，抽�?1（在OnPlay中处理）
    }
}