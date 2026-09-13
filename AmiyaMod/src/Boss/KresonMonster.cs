using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Art;
using BaseLib.Abstracts;
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
/// 血量 200（进阶 10 起 225）。开局自带三个状态：
///   【无敌】体力不会减少（可被【锚点】临时解除）、
///   【飘忽】每玩家每打出 4 张牌给其一张锚点、
///   【终点】8 个克雷松回合后自爆。
/// 意图为 1→2→3 无限循环（确定性，无随机分支）：
///   1. 24(进阶27) 点伤害 + 1 层易伤
///   2. 11×2(进阶13×2) 点伤害 + 给玩家手牌/抽牌堆/弃牌堆中随机 2 张攻击或技能牌附加 3 层【污染】
///   3. 获得 3 点力量，并获得 18 + 当前力量 的格挡
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
        ("moves.KRESON_VOLLEY_MOVE.title", "污染倾泻"),
        ("moves.KRESON_RALLY_MOVE.title", "无垠回响")
    };

    /// <summary>进阶 10（DoubleBoss）起血量 225，否则 200。</summary>
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 225, 200);

    public override int MaxInitialHp => MinInitialHp;

    /// <summary>图鉴里不显示：图鉴会给怪物套一个带 Marker2D 的布局场景，我们的贴图怪没有槽位。</summary>
    public override bool ShouldShowInCompendium => false;

    /// <summary>贴图版战斗立绘（运行时从 mods 目录读 PNG）。</summary>
    public override NCreatureVisuals? CreateCustomVisuals() => KresonVisuals.Build();

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 27, 24);

    private int VolleyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DoubleBoss, 13, 11);

    private const int VolleyHits = 2;

    private const int TaintStacks = 3;

    private const int TaintCards = 2;

    private const int RallyStrength = 3;

    private const int RallyBlockBase = 18;

    /// <summary>
    /// 开局给自身挂【无敌】【终点】，给每个玩家挂一份【飘忽】与污染来源。
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
        await PowerCmd.Apply<KresonInvinciblePower>(choiceContext, self, 1m, self, null, silent: true);
        await PowerCmd.Apply<KresonTerminusPower>(choiceContext, self, 1m, self, null, silent: true);
        foreach (Player player in Creature.CombatState.Players.ToList())
        {
            KresonFadePower fade = (KresonFadePower)ModelDb.Power<KresonFadePower>().ToMutable();
            fade.Target = player.Creature;
            await PowerCmd.Apply(choiceContext, fade, self, 1m, self, null, silent: true);
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
        var volley = new MoveState("KRESON_VOLLEY_MOVE", VolleyMove, new MultiAttackIntent(VolleyDamage, VolleyHits), new CardDebuffIntent());
        var rally = new MoveState("KRESON_RALLY_MOVE", RallyMove, new BuffIntent(), new DefendIntent());
        slash.FollowUpState = volley;
        volley.FollowUpState = rally;
        rally.FollowUpState = slash;
        return new MonsterMoveStateMachine(new MonsterState[] { slash, volley, rally }, slash);
    }

    /// <summary>意图1：重击 + 1 层易伤。</summary>
    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage).FromMonster(this).WithNoAttackerAnim().Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, 1m, Creature, null);
    }

    /// <summary>意图2：二连击 + 给随机两张攻击/技能牌附加 3 层污染。</summary>
    private async Task VolleyMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(VolleyDamage).WithHitCount(VolleyHits).FromMonster(this).WithNoAttackerAnim().Execute(null);
        await ApplyTaint(new ThrowingPlayerChoiceContext());
    }

    /// <summary>意图3：+3 力量，然后获得 18 + 当前力量的格挡。</summary>
    private async Task RallyMove(IReadOnlyList<Creature> targets)
    {
        var choiceContext = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Creature, RallyStrength, Creature, null);
        int block = RallyBlockBase + Creature.GetPowerAmount<StrengthPower>();
        await CreatureCmd.GainBlock(Creature, block, ValueProp.Move, null);
    }

    /// <summary>
    /// 给每个玩家在「手牌 + 抽牌堆 + 弃牌堆」里的随机 2 张攻击/技能牌附加 3 层污染，
    /// 优先挑还没被污染的牌。随机用引擎自己的 PickRandomTargets
    /// （走 RunState.Rng.CombatCardGeneration，多人两端一致）。
    /// </summary>
    private async Task ApplyTaint(PlayerChoiceContext choiceContext)
    {
        KresonTaint affliction = ModelDb.Affliction<KresonTaint>();
        foreach (Player player in Creature.CombatState.Players.ToList())
        {
            List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
                .Concat(PileType.Draw.GetPile(player).Cards)
                .Concat(PileType.Discard.GetPile(player).Cards)
                .Where(c => affliction.CanAfflict(c))
                .ToList();
            if (candidates.Count == 0)
            {
                continue;
            }
            List<CardModel> clean = candidates.Where(c => c.Affliction == null).ToList();
            List<CardModel> already = candidates.Where(c => c.Affliction is KresonTaint).ToList();
            var picks = affliction.PickRandomTargets(player.RunState.Rng, clean, TaintCards).ToList();
            if (picks.Count < TaintCards)
            {
                picks.AddRange(affliction.PickRandomTargets(player.RunState.Rng, already, TaintCards - picks.Count));
            }
            if (picks.Count == 0)
            {
                continue;
            }
            await CardCmd.AfflictAndPreview<KresonTaint>(picks, TaintStacks, CardPreviewStyle.HorizontalLayout);
        }
    }
}
