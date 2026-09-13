using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 存续先兆（魔王，消耗）——多级进化（最多升级 2 次）：
///   +0 绝望（普通）：0费 重放1，无效果；
///   +1 拯救（罕见）：0费 失去1点生命 重放2；
///   +2 停止（稀有）：0费 对随机场上单位施加一层易伤 重放6。
/// 标题/稀有度随升级等级变化（Title/Rarity 均为 virtual）；描述用字符串动态变量 EFFECT。
/// </summary>
public sealed class SurvivalOmen : BaseAmiyaCard
{
    public override int MaxUpgradeLevel => 2;

    protected override bool UsesLevelPortraits => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

    public override AmiyaTag AmiyaTags => AmiyaTag.DemonLord;

    public SurvivalOmen() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override string Title => CurrentUpgradeLevel switch
    {
        0 => "绝望",
        1 => "拯救",
        _ => "停止"
    };

    public override CardRarity Rarity => CurrentUpgradeLevel switch
    {
        0 => CardRarity.Common,
        1 => CardRarity.Uncommon,
        _ => CardRarity.Rare
    };

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new DynamicVar[]
            {
                new UpgradeLevelTextVar("EFFECT", card => card.CurrentUpgradeLevel switch
                {
                    0 => "",
                    1 => "随机场上单位失去 1 点生命。",
                    _ => "对随机场上单位施加一层易伤。"
                })
            };
        }
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "绝望"),
        ("description", "#{EFFECT}")
    };

    protected override IEnumerable<IHoverTip> ExtraHoverTips => new[]
    {
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        switch (CurrentUpgradeLevel)
        {
            case 0:
                break; // 绝望：仅重放1（魔王词条由 FormManagerPower 结算）
            case 1:
            {
                // 拯救：随机场上单位（含自己）失去 1 点生命（无视格挡、不吃力量）
                var candidates = Owner!.Creature.CombatState.Creatures.Where(c => c.IsAlive).ToList();
                if (candidates.Count > 0)
                {
                    // 多人安全：随机目标走联机同步的 CombatTargets 流
                    Creature target = candidates[Owner.RunState.Rng.CombatTargets.NextInt(candidates.Count)];
                    await CreatureCmd.Damage(choiceContext, target, 1m, ValueProp.Unblockable | ValueProp.Unpowered, Owner.Creature, null, null);
                }
                break;
            }
            default:
            {
                // 停止：随机对场上单位（含自己）施加 1 层易伤
                var targets = Owner!.Creature.CombatState.Creatures.Where(c => c.IsAlive).ToList();
                if (targets.Count > 0)
                {
                    Creature target = targets[Owner.RunState.Rng.CombatTargets.NextInt(targets.Count)];
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 1m, Owner.Creature, null, silent: true);
                }
                break;
            }
        }
    }

    public override void AfterCreated()
    {
        base.AfterCreated();
        BaseReplayCount = ReplayForLevel();
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        BaseReplayCount = ReplayForLevel();
    }

    protected override void OnUpgrade()
    {
        // 篝火升级后不会再走 AfterCreated/AfterCloned，重放数必须在这里显式重设
        BaseReplayCount = ReplayForLevel();
    }

    private int ReplayForLevel() => CurrentUpgradeLevel switch
    {
        0 => 1,
        1 => 2,
        _ => 6
    };
}
