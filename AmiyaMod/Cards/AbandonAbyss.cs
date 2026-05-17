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

/// <summary>背弃深渊 - 墓园 对所有敌人造成8点伤害3/4次 消耗</summary>
[Pool(typeof(AmiyaCardPool))]
public partial class AbandonAbyss : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new List<DynamicVar>
    {
        new DamageVar(8m, ValueProp.Move),
        new RepeatVar(3)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { AmiyaKeywords.Graveyard, CardKeyword.Exhaust };

    public AbandonAbyss() 
        : base(3, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = DynamicVars.Repeat.IntValue;
        var enemies = CombatState.HittableEnemies;

        for (int i = 0; i < hitCount; i++)
        {
            foreach (var enemy in enemies)
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
        DynamicVars.Repeat.UpgradeValueBy(1);
    }
}