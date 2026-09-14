using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Art;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Boss;

/// <summary>
/// 无垠回荡克雷松（三层 boss）。
///
/// 血量 365。开局自带四个状态：
///   【无敌】体力不会减少（可被【锚点】临时解除）、
///   【飘忽】每玩家每打出 4 张牌给其一张锚点、
///   【育苗】每回合开始给玩家随机 3 张攻击/技能牌附加 4 层污染（持有【无垠花】时改为 2 层）、
///   【终点】8 个克雷松回合后自爆。
/// 意图为 1→2→3 无限循环（确定性，无随机分支）：
///   1. 27 点伤害 + 1 层易伤
///   2. 11×2 点伤害
///   3. 获得 3 点力量，并获得 15 + 2×当前力量 的格挡
/// </summary>
public sealed class KresonMonster : CustomMonsterModel, ILocalizationProvider
{
    /// <summary>
    /// 怪物文本（monsters 表）。CustomMonsterModel 本身不实现 ILocalizationProvider，
    /// 但这里手动实现后，BaseLib 的 ModelLocPatch 会按 entry 前缀写进 monsters 表，
    /// 于是不需要额外打包 localization/monsters.json（缺 key 会在悬停时抛 LocException）。
    /// </summary>
    public List<(string, string)>? Localization => new()
    {
        ("name", "无垠回荡克雷松"),
        ("moves.KRESON_SLASH_MOVE.title", "回荡之击"),
        ("moves.KRESON_VOLLEY_MOVE.title", "无垠连响"),
        ("moves.KRESON_RALLY_MOVE.title", "回响蓄势")
    };

    /// <summary>血量 365（不再随进阶变化）。</summary>
    public override int MinInitialHp => 365;

    public override int MaxInitialHp => MinInitialHp;

    /// <summary>图鉴里不显示：图鉴会给怪物套一个带 Marker2D 的布局场景，我们的贴图怪没有槽位。</summary>
    public override bool ShouldShowInCompendium => false;

    /// <summary>贴图版战斗立绘（运行时从 mods 目录读 PNG）。</summary>
    public override NCreatureVisuals? CreateCustomVisuals() => KresonVisuals.Build();

    /// <summary>
    /// 只暴露真实存在的资源给引擎预加载。
    /// 基类默认的 VisualsPath 是 res://scenes/creature_visuals/kreson_monster.tscn（不存在），
    /// 我们的立绘又是运行时按文件读的——若让引擎去预加载那条不存在的路径，
    /// 会抛 AssetLoadException 把战斗开始流程搞崩（与地图图标那次同一个坑）。
    /// </summary>
    public override IEnumerable<string> AssetPaths => base.AssetPaths.Where(p => ResourceLoader.Exists(p));

    private const int SlashDamage = 27;

    private const int VolleyDamage = 11;

    private const int VolleyHits = 2;

    private const int RallyStrength = 3;

    /// <summary>格挡 = 15 + 2 × 当前力量。</summary>
    private const int RallyBlockBase = 15;

    private const int RallyBlockPerStrength = 2;

    /// <summary>
    /// 开局给自身挂【无敌】【终点】，给每个玩家挂【飘忽】【育苗】与污染来源。
    /// 此时 CombatManager 尚未进入 InProgress，但力量施加本身是允许的（原版怪物同款写法）。
    /// </summary>
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        if (Creature?.CombatState == null)
        {
            return;
        }
        var choiceContext = new ThrowingPlayerChoiceContext();
        Creature self = Creature;
        // 施加顺序 = 状态栏显示顺序：无敌 → 飘忽 → 育苗 → 终点
        await PowerCmd.Apply<KresonInvinciblePower>(choiceContext, self, 1m, self, null, silent: true);
        foreach (Player player in Creature.CombatState.Players.ToList())
        {
            // 飘忽：每个玩家一份实例（Target = 该玩家，只有本人看得到自己的计数）
            KresonFadePower fade = (KresonFadePower)ModelDb.Power<KresonFadePower>().ToMutable();
            fade.Target = player.Creature;
            await PowerCmd.Apply(choiceContext, fade, self, KresonFadePower.BaseCardsLeft, self, null, silent: true);
        }
        await PowerCmd.Apply<KresonNursingPower>(choiceContext, self, 1m, self, null, silent: true);
        await PowerCmd.Apply<KresonTerminusPower>(choiceContext, self, KresonTerminusPower.BaseTurns, self, null, silent: true);
        // 污染来源：隐形状态，挂在每个玩家身上（不打乱克雷松自己的状态栏顺序）
        foreach (Player player in Creature.CombatState.Players.ToList())
        {
            await PowerCmd.Apply<KresonTaintSourcePower>(choiceContext, player.Creature, 1m, self, null, silent: true);
        }
        KresonVisuals.SetState(Creature, KresonVisuals.State.Invincible);
    }

    /// <summary>死亡时换成"死亡"贴图。</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature == Creature)
        {
            KresonVisuals.SetState(Creature, KresonVisuals.State.Dead);
        }
    }

    /// <summary>1 → 2 → 3 → 1 无限循环；MoveState.GetNextState 不看 rng，所以完全确定。</summary>
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var slash = new MoveState("KRESON_SLASH_MOVE", SlashMove, new SingleAttackIntent(SlashDamage), new DebuffIntent());
        var volley = new MoveState("KRESON_VOLLEY_MOVE", VolleyMove, new MultiAttackIntent(VolleyDamage, VolleyHits));
        var rally = new MoveState("KRESON_RALLY_MOVE", RallyMove, new BuffIntent(), new DefendIntent());
        slash.FollowUpState = volley;
        volley.FollowUpState = rally;
        rally.FollowUpState = slash;
        return new MonsterMoveStateMachine(new MonsterState[] { slash, volley, rally }, slash);
    }

    /// <summary>意图1：27 点伤害 + 1 层易伤。</summary>
    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithNoAttackerAnim().Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, 1m, Creature, null);
    }

    /// <summary>意图2：11×2 点伤害。</summary>
    private async Task VolleyMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(VolleyDamage).WithHitCount(VolleyHits).FromMonster(this).WithNoAttackerAnim().Execute(null);
    }

    /// <summary>意图3：+3 力量，然后获得 15 + 2×当前力量的格挡。</summary>
    private async Task RallyMove(IReadOnlyList<Creature> targets)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, RallyStrength, Creature, null);
        int block = RallyBlockBase + RallyBlockPerStrength * Creature.GetPowerAmount<StrengthPower>();
        await CreatureCmd.GainBlock(Creature, block, ValueProp.Move, null);
    }
}
