using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 决意 - 初始攻击�?
/// 造成5/7点伤害，本场战斗每触发过一次形态转换额外命中一�?
/// </summary>
[Pool(typeof(AmiyaCardPool))]

public partial class Resolve : BaseAmiyaCard
{
    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(5m, ValueProp.Move)
    };

    public Resolve() 
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        // 计算额外命中次数 = 形态转换总次�?
        int extraHits = 0;
        var formManager = Owner.Creature.GetPower<FormManagerPower>();
        if (formManager != null)
        {
            extraHits = formManager.TotalFormSwitches;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(target)
            .WithHitCount(1 + extraHits)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}