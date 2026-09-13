using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 战术轰炸（罕见）：对敌方全体造成 7/10 点伤害。
/// 近卫形态：额外再造成一次；术士形态：给予全体敌人 2 层易伤；
/// 魔王形态：额外再造成一次并给予全体敌人 2 层易伤。
/// </summary>
public sealed class TacticalBombardment : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(7m, ValueProp.Move) };

    public TacticalBombardment() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "战术轰炸"),
        ("description", "#对敌方全体造成 !D! 点伤害。若处于近卫形态，额外造成一次；若处于术士形态，给予全体敌人 2 层易伤；若处于魔王形态，额外造成一次并给予全体敌人 2 层易伤。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);

        if (Owner?.Creature == null)
        {
            return;
        }

        var form = FormManagerPower.Of(Owner)?.Form ?? AmiyaForm.Guard;
        switch (form)
        {
            case AmiyaForm.Guard:
                // 近卫：再打一次全体伤害
                await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
                break;
            case AmiyaForm.Caster:
                // 术士：全体敌人 2 层易伤
                await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature.CombatState.Enemies, 2m, Owner.Creature, null, silent: true);
                break;
            case AmiyaForm.DemonLord:
                // 魔王：再打一次全体伤害 + 全体敌人 2 层易伤
                await CommonActions.CardAttack(this, cardPlay).Execute(choiceContext);
                await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature.CombatState.Enemies, 2m, Owner.Creature, null, silent: true);
                break;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 7 → 10
    }
}
