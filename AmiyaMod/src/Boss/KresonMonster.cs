using System;
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
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
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
///   1. 回荡之击：27 点伤害 + 1 层易伤
///   2. 无垠连响：按轮次递增段数 —— 第一轮 12 点 ×2 次、第二轮 8 点 ×3 次、第三轮 6 点 ×4 次
///      （三档的总量都是 24 点，但段数越多、克雷松的【力量】加成吃到的次数越多）
///   3. 回响蓄势：获得 3 点力量，并获得 15 + 2×当前力量 的格挡
/// 因为【终点】在第 8 个克雷松回合结束就引爆，无垠连响最多只会用到第三档。
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

    /// <summary>无垠连响每轮的"单段伤害"（三档总量都是 24，段数递增）。</summary>
    private static readonly int[] VolleyDamageByRound = { 12, 8, 6 };

    /// <summary>无垠连响每轮的段数。</summary>
    private static readonly int[] VolleyHitsByRound = { 2, 3, 4 };

    private const int RallyStrength = 3;

    /// <summary>格挡 = 15 + 2 × 当前力量。</summary>
    private const int RallyBlockBase = 15;

    private const int RallyBlockPerStrength = 2;

    private int _volleyRound;

    /// <summary>
    /// 无垠连响已经打到了第几轮（0/1/2 → 12×2 / 8×3 / 6×4）。
    /// 存档属性；两端都在同一段确定性流程里 +1（打完无垠连响后），所以不会分歧。
    /// 注意这里直接写字段、不调 AssertMutable()：这个计数器是在克雷松自己的行动中途写的，
    /// 万一拿到不可变实例也只是数值不推进，绝不能让断言把 Boss 的回合打断。
    /// </summary>
    [SavedProperty]
    public int VolleyRound
    {
        get => _volleyRound;
        set => _volleyRound = value;
    }

    private static int VolleyDamageFor(int round)
        => VolleyDamageByRound[Math.Clamp(round, 0, VolleyDamageByRound.Length - 1)];

    private static int VolleyHitsFor(int round)
        => VolleyHitsByRound[Math.Clamp(round, 0, VolleyHitsByRound.Length - 1)];

    /// <summary>
    /// 无垠连响的意图：伤害与段数都按"当前轮次"动态取值（两个委托是引擎渲染意图时现调的，
    /// 所以玩家看到的永远是这一轮的真实数值，而不是建状态机时定死的常量）。
    /// </summary>
    private sealed class VolleyIntent : MultiAttackIntent
    {
        public VolleyIntent(Func<int> damage, Func<int> hits)
            : base(0, hits)
        {
            DamageCalc = () => damage();
        }
    }

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
        var volley = new MoveState(
            "KRESON_VOLLEY_MOVE",
            VolleyMove,
            new VolleyIntent(() => VolleyDamageFor(VolleyRound), () => VolleyHitsFor(VolleyRound)));
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

    /// <summary>意图2：无垠连响 —— 第一轮 12×2 / 第二轮 8×3 / 第三轮 6×4，打完推进轮次。</summary>
    private async Task VolleyMove(IReadOnlyList<Creature> targets)
    {
        int round = VolleyRound;
        await DamageCmd.Attack(VolleyDamageFor(round))
            .WithHitCount(VolleyHitsFor(round))
            .FromMonster(this)
            .WithNoAttackerAnim()
            .Execute(null);
        // 打完再推进：本轮意图（含悬停里的数字）用的必须是本轮这一档
        VolleyRound = Math.Min(round + 1, VolleyDamageByRound.Length - 1);
        Log.Info($"[Amiya] 无垠连响：第 {round + 1} 轮 {VolleyDamageFor(round)}×{VolleyHitsFor(round)} 已结算，下一轮 = {VolleyRound + 1}");
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
