using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>待调弦的怒火（稀有，领袖）：对所有敌人造成 12/16 点伤害；若未斩杀敌人，受到 3/2 点伤害（Q9）。</summary>
public sealed class UntunedRage : BaseAmiyaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(12m, ValueProp.Move) };

    public override AmiyaTag AmiyaTags => AmiyaTag.Leader;

    public UntunedRage() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "待调弦的怒火"),
        ("description", "#对所有敌人造成 !D! 点伤害。若未斩杀敌人，受到 {IfUpgraded:show:2|3} 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var enemies = Owner!.Creature.CombatState.Enemies;
        int aliveBefore = enemies.Count(e => e.IsAlive);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        int aliveAfter = enemies.Count(e => e.IsAlive);
        if (aliveAfter == aliveBefore)
        {
            // 未斩杀：自伤 3/2（可被格挡，Q5）
            decimal selfDamage = IsUpgraded ? 2m : 3m;
            await CreatureCmd.Damage(choiceContext, Owner.Creature, selfDamage, ValueProp.Move, Owner.Creature, null, null);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4m); // 12 → 16
    }
}
