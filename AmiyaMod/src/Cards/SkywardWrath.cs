using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 盛怒倾天（罕见，2 费）：对场上随机单位造成 6 点伤害 7 次（升级后 8 点）。
/// 友方单位因此损失生命时，获得损失量一半的再生。
/// 随机目标走联机同步的 CombatTargets 随机流（多人一致）。
/// </summary>
public sealed class SkywardWrath : BaseAmiyaCard
{
    private const int HitCount = 7;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(6m, ValueProp.Move) };

    public SkywardWrath() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "盛怒倾天"),
        ("description", $"#对场上随机单位造成 !D! 点伤害 {HitCount} 次。友方单位因此失去生命时，获得失去量一半的再生。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
        {
            return;
        }
        decimal perHit = DynamicVars.Damage.BaseValue;
        decimal friendlyLoss = 0m;

        for (int i = 0; i < HitCount; i++)
        {
            // 每次命中前重新快照：中途死亡的单位不再进入随机池
            List<Creature> alive = Owner.Creature.CombatState.Creatures.Where(c => c.IsAlive).ToList();
            if (alive.Count == 0)
            {
                break;
            }
            Creature target = alive[Owner.RunState.Rng.CombatTargets.NextInt(alive.Count)];
            IEnumerable<DamageResult> results = await CreatureCmd.Damage(
                choiceContext, target, perHit, ValueProp.Move, Owner.Creature, this, cardPlay);

            if (target.Side == CombatSide.Player)
            {
                friendlyLoss += results.Sum(r => r.UnblockedDamage);
            }
        }

        // 友方（含自己与其他玩家）因此损失的生命 → 一半转化为再生
        if (friendlyLoss > 0)
        {
            decimal regen = Math.Max(1m, friendlyLoss / 2m);
            await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, regen, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 6 → 8
    }
}
