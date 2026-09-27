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
        // 按"损失生命的那个友方单位"分别累计（含自己与其他玩家）：
        // 原来只把总量的一半给了出牌者自己，队友挨了打却拿不到再生。
        // 用 List 而不是 Dictionary，保证顺序在两端一致（联机下不能依赖字典枚举顺序）。
        var losses = new List<(Creature Creature, decimal Lost)>();

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
                decimal lost = results.Sum(r => r.UnblockedDamage);
                if (lost > 0m)
                {
                    int idx = losses.FindIndex(x => x.Creature == target);
                    if (idx >= 0)
                    {
                        losses[idx] = (target, losses[idx].Lost + lost);
                    }
                    else
                    {
                        losses.Add((target, lost));
                    }
                }
            }
        }

        // 每个因此损失生命的友方单位，各自获得"自己损失量一半"的再生
        foreach ((Creature creature, decimal lost) in losses)
        {
            if (!creature.IsAlive)
            {
                continue;
            }
            decimal regen = Math.Max(1m, lost / 2m);
            await PowerCmd.Apply<RegenPower>(choiceContext, creature, regen, Owner.Creature, null, silent: true);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 6 → 8
    }
}
