using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>一断皆断（普通）：对敌方全体造成 6/9 点伤害；若敌人意图不是攻击，则对其造成两次伤害。</summary>
public sealed class OneStrikeEndsAll : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move) };

    public OneStrikeEndsAll() : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "一断皆断"),
        ("description", "#对敌方全体造成 !D! 点伤害。若敌人意图不是攻击，则对其造成两次伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
        {
            return;
        }
        // 快照遍历：攻击可能击杀敌人导致 Enemies 集合被修改（此前 foreach 活集合抛 Collection was modified）
        foreach (var enemy in Owner.Creature.CombatState.Enemies.ToList())
        {
            bool intendsToAttack = enemy.Monster != null && enemy.Monster.IntendsToAttack;
            int hits = intendsToAttack ? 1 : 2;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(hits)
                .FromCard(this, cardPlay)
                .Targeting(enemy)
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
