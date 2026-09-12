using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>苦难摇篮（罕见，固有，消耗）：对所有敌人造成 8 点伤害并给予它们两层虚弱。</summary>
public sealed class SufferingCradle : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(8m, ValueProp.Move) };

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust, CardKeyword.Innate };

    public SufferingCradle() : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "苦难摇篮"),
        ("description", "#对所有敌人造成 !D! 点伤害并给予 {IfUpgraded:show:1 层虚弱两次|2 层虚弱}。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        if (IsUpgraded)
        {
            // 升级后分两次各给 1 层虚弱：更快消耗敌人的人工制品层数
            for (int i = 0; i < 2; i++)
            {
                await PowerCmd.Apply<WeakPower>(choiceContext, Owner!.Creature.CombatState.Enemies, 1m, Owner.Creature, null, silent: true);
            }
        }
        else
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, Owner!.Creature.CombatState.Enemies, 2m, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 8 → 12
    }
}
