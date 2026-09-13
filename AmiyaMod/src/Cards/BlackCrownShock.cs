using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>黑冠震荡（稀有，魔王，领袖）：若处于魔王形态，对所有敌人造成 20/24 点伤害。</summary>
public sealed class BlackCrownShock : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(20m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord | AmiyaTag.Leader;

    public BlackCrownShock() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "黑冠震荡"),
        ("description", "#若处于魔王形态，对所有敌人造成 !D! 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (FormManagerPower.Of(Owner)?.Form != AmiyaForm.DemonLord)
        {
            return;
        }
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
