using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Afflictions;
using Amiya.Cards;
using Amiya.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

public enum AmiyaForm
{
    Guard = 1,   // 近卫
    Caster = 2,  // 术士
    Medic = 3,   // 医疗
    DemonLord = 4 // 魔王
}

/// <summary>
/// 形态机 + 词条引擎（施加在玩家生物上的 Power）。
/// 形态循环/苍白赐福/领袖/感染/魔王之唤/魔王形态(黑冠·燃烬·裁决)/下回合力量池。
/// 禁止自行 new（类型已被 ModelDb 注册）。
/// </summary>
public sealed class FormManagerPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/FormManagerPower.png";
    public static FormManagerPower? Current { get; private set; }

    /// <summary>本场战斗阿米娅死亡时的形态（结算画面死亡立绘用；战斗开始时由 Harness 重置）。</summary>
    public static AmiyaForm? AmiyaDeathForm { get; set; }

    public AmiyaForm Form { get; private set; } = AmiyaForm.Guard;

    /// <summary>进入战斗时的生命值快照（神圣复苏"恢复到战斗开始状态"用）。</summary>
    public int CombatStartHp { get; private set; }

    public int FormSwitchTotal { get; private set; }

    public int TurnCount { get; private set; }

    /// <summary>本回合已打出的牌数（领袖奇偶判定用）。</summary>
    public int TurnCardPlayCount { get; private set; }

    /// <summary>本回合已打出的攻击牌数（洪流不息：打出前计数，Q27 不含自身）。</summary>
    public int TurnAttackPlayedCount { get; private set; }

    /// <summary>本场战斗"成功完全格挡"次数（神魂坚韧之歌）。</summary>
    public int FullyBlockedCount { get; private set; }

    /// <summary>本回合授予的临时力量（燎原/盛怒旋风）：回合结束全额移除（当回合生效，官方 SETUP_STRIKE 语义）。</summary>
    public decimal TempStrengthThisTurn { get; private set; }

    /// <summary>授予临时力量：本回合 +amount 力量，回合结束由 AfterSideTurnEnd 全额移除。</summary>
    public async Task GrantTempStrength(PlayerChoiceContext choiceContext, decimal amount)
    {
        if (amount <= 0)
        {
            return;
        }
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, amount, Owner, null, silent: true);
        TempStrengthThisTurn += amount;
        Log.Info($"[Amiya] temp strength +{amount} (turn total={TempStrengthThisTurn})");
    }

    /// <summary>魔王黑雾：战斗开始时给所有魔王牌挂上特效（视觉层复用官方 smog 覆盖层）。</summary>
    public async Task ApplyDemonFogToAll(Player? player)
    {
        if (player?.PlayerCombatState == null)
        {
            return;
        }
        foreach (var card in player.PlayerCombatState.AllCards.ToList())
        {
            if (card is BaseAmiyaCard ac && ac.HasAmiyaTag(AmiyaTag.DemonLord) && card.Affliction == null)
            {
                await CardCmd.Afflict<AmiyaDemonLordAffliction>(card, 1m);
            }
        }
    }

    /// <summary>
    /// 手牌词条特效（每张牌一张特效槽）：
    ///   领袖生效（下一张=奇数张）→ 白光优先（双词条牌先清黑雾再挂白光）；
    ///   否则 → 领袖牌无特效；魔王牌挂黑雾。
    /// </summary>
    public async Task RefreshLeaderGlow(Player player)
    {
        bool leaderActive = TurnCardPlayCount % 2 == 0;
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToList())
        {
            if (card is not BaseAmiyaCard ac)
            {
                continue;
            }
            bool isLeader = ac.HasAmiyaTag(AmiyaTag.Leader);
            bool isDemon = ac.HasAmiyaTag(AmiyaTag.DemonLord);
            if (isLeader && leaderActive)
            {
                // 领袖生效条件更苛刻：白光优先于黑雾
                if (card.Affliction is AmiyaDemonLordAffliction)
                {
                    CardCmd.ClearAffliction(card);
                }
                if (card.Affliction == null)
                {
                    await CardCmd.Afflict<AmiyaLeaderGlowAffliction>(card, 1m);
                }
            }
            else
            {
                if (card.Affliction is AmiyaLeaderGlowAffliction)
                {
                    CardCmd.ClearAffliction(card);
                }
                if (isDemon && card.Affliction == null)
                {
                    await CardCmd.Afflict<AmiyaDemonLordAffliction>(card, 1m);
                }
            }
        }
    }

    /// <summary>下回合格挡池（今时明日的偏差）。</summary>
    public int PendingBlockNextTurn { get; private set; }

    /// <summary>外部追加下回合格挡（盛怒旋风等手动多段攻击牌的适配入口）。</summary>
    public void AddPendingBlockNextTurn(int amount)
    {
        PendingBlockNextTurn += amount;
    }

    /// <summary>本回合打击/防御免费（风暴瞭望），回合开始重置。</summary>
    public bool StrikesDefendsFreeThisTurn { get; set; }

    /// <summary>相变不息：本回合每次获得格挡（卡牌来源）触发形态转换。</summary>
    public bool PhaseChangeActive { get; set; }

    /// <summary>最近一次可用的 PlayerChoiceContext（供无上下文的钩子触发形态转换）。</summary>
    private PlayerChoiceContext? _lastCtx;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "阿米娅形态"),
        ("description", "当前形态由角标数字表示：1=近卫，2=术士，3=医疗，4=魔王。\n近卫：进入时获得3点格挡与3点活力。\n术士：进入时给予全体敌人1层虚弱。\n医疗：进入时抽1张牌。")
    };

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Current = this;
        Form = AmiyaForm.Guard;
        CombatStartHp = Owner?.CurrentHp ?? 0;
        FormSwitchTotal = 0;
        TurnCardPlayCount = 0;
        TurnAttackPlayedCount = 0;
        FullyBlockedCount = 0;
        TempStrengthThisTurn = 0;
        PendingBlockNextTurn = 0;
        StrikesDefendsFreeThisTurn = false;
        PhaseChangeActive = false;
        _lastCtx = null;
        TurnCount = 1; // 施加发生在第1回合中途，钩子从第2回合开始（保证"除第一回合外"不错位）
        Log.Info($"[Amiya] FormManagerPower applied (form={Form})");
        _ = ApplyDemonFogToAll(Owner?.Player);
        if (Owner?.Player != null)
        {
            _ = RefreshLeaderGlow(Owner.Player);
        }
        return Task.CompletedTask;
    }

    // —— 形态 ——

    /// <summary>
    /// 统一入口：近卫→术士→医疗 循环并结算入形态效果。
    /// 燃烬存在时（魔王形态）改为"玩家自选一张手牌消耗"，不再切换形态。
    /// 返回 Task 供调用方等待：燃烬的选牌 UI 必须在动作上下文存活期间完成。
    /// </summary>
    public async Task Trigger(PlayerChoiceContext choiceContext)
    {
        _lastCtx = choiceContext;
        if (Owner.HasPower<EmberPower>())
        {
            await EmberConsume(choiceContext);
            return;
        }

        Form = (AmiyaForm)(((int)Form % 3) + 1);
        FormSwitchTotal++;
        Log.Info($"[Amiya] Form switch -> {Form} (total={FormSwitchTotal})");
        _ = OnFormSwitched(choiceContext);
        _ = SyncAmountAndEffects(choiceContext);
    }

    /// <summary>形态切换成功后联动各能力牌（燎原/灵魂堡垒/蔓延/铁卫）。</summary>
    private async Task OnFormSwitched(PlayerChoiceContext choiceContext)
    {
        if (Owner.HasPower<WildfirePower>())
        {
            await GrantTempStrength(choiceContext, Owner.GetPowerAmount<WildfirePower>());
        }
        if (Owner.HasPower<SoulFortressPower>())
        {
            await CreatureCmd.GainBlock(Owner, Owner.GetPowerAmount<SoulFortressPower>(), ValueProp.Move, null);
        }
        if (Owner.HasPower<SpreadPower>() && Owner.Player != null)
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
        }
        if (Form == AmiyaForm.Guard && Owner.HasPower<IronGuardPower>())
        {
            await PowerCmd.Apply<PlatingPower>(choiceContext, Owner, Owner.GetPowerAmount<IronGuardPower>(), Owner, null, silent: true);
        }

        // 奔夜：每次成功形态转换，把位于抽牌堆/弃牌堆的奔夜移入手牌
        if (Owner.Player != null)
        {
            foreach (var pileType in new[] { PileType.Draw, PileType.Discard })
            {
                foreach (var card in pileType.GetPile(Owner.Player).Cards.ToList())
                {
                    if (card is RunningNight)
                    {
                        await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom);
                        Log.Info("[Amiya] running night: returned to hand");
                    }
                }
            }
        }
    }

    private async Task EmberConsume(PlayerChoiceContext choiceContext)
    {
        try
        {
            // 燃烬（Q4 已裁）：玩家自选一张手牌消耗；空手则无事发生
            if (Owner.Player == null || PileType.Hand.GetPile(Owner.Player).Cards.Count == 0)
            {
                Log.Info("[Amiya] ember: no cards in hand, skip");
                return;
            }
            var selected = await CardSelectCmd.FromHand(
                choiceContext, Owner.Player,
                new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1, 1),
                _ => true, this);
            var list = selected.ToList();
            Log.Info($"[Amiya] ember: selection returned {list.Count} card(s)");
            foreach (var card in list)
            {
                await CardCmd.Exhaust(choiceContext, card);
                Log.Info($"[Amiya] ember: exhausted {card.Id.Entry}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] ember consume failed: {ex}");
        }
    }

    private async Task SyncAmountAndEffects(PlayerChoiceContext choiceContext)
    {
        try
        {
            int delta = (int)Form - Amount;
            if (delta != 0)
            {
                await PowerCmd.ModifyAmount(choiceContext, this, delta, Owner, null, silent: true);
            }
            await ApplyEntryEffects(choiceContext);
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] form entry effects error ({Form}): {ex}");
        }
    }

    private async Task ApplyEntryEffects(PlayerChoiceContext choiceContext)
    {
        switch (Form)
        {
            case AmiyaForm.Guard:
                await CreatureCmd.GainBlock(Owner, 3m, ValueProp.Move, null);
                await PowerCmd.Apply<VigorPower>(choiceContext, Owner, 3m, Owner, null, silent: true);
                break;
            case AmiyaForm.Caster:
                await PowerCmd.Apply<WeakPower>(choiceContext, Owner.CombatState.Enemies, 1m, Owner, null, silent: true);
                break;
            case AmiyaForm.Medic:
                if (Owner.Player != null)
                {
                    await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
                }
                break;
        }
    }

    // —— 词条引擎 ——

    /// <summary>领袖奇偶计数：每张牌打出前 +1；并在此消费一次性免费标记（费用钩子须保持无状态，否则 UI 费用颜色查询会误吞标记）。</summary>
    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner?.Player != null && cardPlay.Card?.Owner == Owner.Player)
        {
            TurnCardPlayCount++;
            if (cardPlay.Card.Type == CardType.Attack)
            {
                TurnAttackPlayedCount++;
            }
            Log.Info($"[Amiya] card play #{TurnCardPlayCount}: {cardPlay.Card.Id.Entry}");

            // 领袖白光随奇偶实时刷新
            await RefreshLeaderGlow(Owner.Player);
        }
    }

    /// <summary>领袖：本回合第奇数张打出的牌 → 放入抽牌堆（随机洗入）；带消耗词条则照常消耗。</summary>
    public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
    {
        if (card is BaseAmiyaCard amiyaCard
            && amiyaCard.HasAmiyaTag(AmiyaTag.Leader)
            && !card.Keywords.Contains(CardKeyword.Exhaust)
            && TurnCardPlayCount % 2 == 0 // 位置钩子先于本牌计数
            && Owner?.Player != null)
        {
            Log.Info($"[Amiya] leader effect: {card.Id.Entry} -> draw pile (play #{TurnCardPlayCount + 1})");
            return new CardLocation(Owner.Player, PileType.Draw, CardPilePosition.Random);
        }
        return cardLocation;
    }

    /// <summary>魔王：打出魔王牌 → 魔王之唤 +1（懒加载）；裁决联动；≥7 层进入魔王形态。</summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        _lastCtx = choiceContext;

        if (Owner?.Player == null || cardPlay.Card?.Owner != Owner.Player)
        {
            return;
        }

        // 领袖白光：打出后奇偶变化，刷新剩余领袖牌
        await RefreshLeaderGlow(Owner.Player);

        // 疗养特供卡：每打出一张感染牌获得 1 能量
        if (cardPlay.Card is BaseAmiyaCard taggedPlay && taggedPlay.HasAmiyaTag(AmiyaTag.Infection) && Owner.HasPower<NursingCardPower>())
        {
            await PlayerCmd.GainEnergy(1m, Owner.Player);
        }

        // 今时明日的偏差：每打出一张攻击牌，下回合格挡池 +6/8
        if (cardPlay.Card.Type == CardType.Attack && Owner.HasPower<TodayTomorrowDeviationPower>())
        {
            PendingBlockNextTurn += Owner.GetPowerAmount<TodayTomorrowDeviationPower>();
        }

        if (cardPlay.Card is not BaseAmiyaCard amiyaCard
            || !amiyaCard.HasAmiyaTag(AmiyaTag.DemonLord))
        {
            return;
        }

        await GainDemonLordCall(choiceContext, cardPlay.Card, cardPlay);
    }

    /// <summary>魔王之唤 +1（懒加载施加）+ 裁决 + 阈值进入魔王形态。终焉之影等外部来源复用。</summary>
    public async Task GainDemonLordCall(PlayerChoiceContext choiceContext, CardModel? cardSource = null, CardPlay? cardPlay = null)
    {
        DemonLordCallPower? call = Owner.GetPower<DemonLordCallPower>();
        if (call == null)
        {
            call = await PowerCmd.Apply<DemonLordCallPower>(choiceContext, Owner, 1m, Owner, null, silent: true);
            Log.Info("[Amiya] demon lord call +1 (total=1, lazy applied)");
        }
        else
        {
            await PowerCmd.ModifyAmount(choiceContext, call, 1m, Owner, null, silent: true);
            Log.Info($"[Amiya] demon lord call +1 (total={call.Amount})");
        }

        if (Owner.HasPower<JudgmentPower>())
        {
            // dealer=null：裁决为纯伤害，不吃力量/易伤等任何加成（Unpowered 固定值，卡面写 9 就是 9）
            await CreatureCmd.Damage(choiceContext, Owner.CombatState.Enemies, 9m, ValueProp.Move | ValueProp.Unpowered, null, null, null);
            // 官方模式：裸 CreatureCmd.Damage 不走攻击管线，须手动触发胜负判定（击杀最后敌人立即结束战斗）
            await CombatManager.Instance.CheckWinCondition();
            Log.Info("[Amiya] judgment: 9 damage to all enemies");
        }

        if (Owner.HasPower<DemonLordVesselPower>())
        {
            // 对所有敌人造成当前魔王之唤层数的伤害（固定值，不吃力量/易伤）。
            // FromCard 先行设置 attacker，否则 AttackCommand 抛 "We require an attacker..."。
            var vesselAttack = DamageCmd.Attack(call.Amount);
            if (cardSource != null)
            {
                vesselAttack = vesselAttack.FromCard(cardSource, cardPlay);
            }
            await vesselAttack.Unpowered().TargetingAllOpponents(Owner.CombatState).Execute(choiceContext);
        }
        if (Owner.HasPower<DemonLordBannerPower>() && Owner.Player != null)
        {
            await CardPileCmd.Draw(choiceContext, Owner.GetPowerAmount<DemonLordBannerPower>(), Owner.Player);
        }

        if (call.Amount >= 7 && Form != AmiyaForm.DemonLord)
        {
            await EnterDemonLord(choiceContext);
        }
    }

    private async Task EnterDemonLord(PlayerChoiceContext choiceContext)
    {
        Form = AmiyaForm.DemonLord;
        Log.Info("[Amiya] entering DEMON LORD form: granting 黑冠/燃烬/裁决");
        await PowerCmd.Apply<BlackCrownPower>(choiceContext, Owner, 1m, Owner, null, silent: true);
        await PowerCmd.Apply<EmberPower>(choiceContext, Owner, 1m, Owner, null, silent: true);
        await PowerCmd.Apply<JudgmentPower>(choiceContext, Owner, 1m, Owner, null, silent: true);
        int delta = 4 - Amount;
        if (delta != 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, delta, Owner, null, silent: true);
        }
    }

    /// <summary>
    /// 费用修正（无状态纯查询——UI 费用颜色也会高频调用此钩子，禁止在此消费标记）：
    /// 兔兔之踢（4-形态转换次数，下限0）；虚空残片（回合首牌，标记在 BeforeCardPlayed 消费）；
    /// 得见光芒（打击/防御 0 费）；风暴瞭望（打击/防御免费回合）；剑不眠（下一张领袖牌 0 费）。
    /// </summary>
    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (Owner?.Player == null || card.Owner != Owner.Player)
        {
            return false;
        }
        if (Owner.GetPower<VoidFragmentPower>() is { } vf && vf.PlaysThisTurn < 1)
        {
            // 虚空残片免费优先级最高：回合首牌（含兔兔之踢）直接 0 费，避免兔兔之踢分支抢先按自身折扣结算
            modifiedCost = 0m;
            return true;
        }
        if (card is RabbitKick)
        {
            modifiedCost = Math.Max(0m, 4m - FormSwitchTotal);
            Log.Info($"[Amiya] RabbitKick cost: original={originalCost}, switches={FormSwitchTotal}, modified={modifiedCost}");
            return true;
        }
        if (StrikesDefendsFreeThisTurn
            && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend)))
        {
            modifiedCost = 0m;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 相变不息：本回合每次获得格挡（卡牌来源）→ 触发形态转换（Q41：每次获得格挡命令一次）。
    /// 我是你的了：格挡全额转化为等量再生（AfterBlockGained 每次真实获得仅触发一次，预览/计算钩子不会重复叠加）。
    /// </summary>
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        await base.AfterBlockGained(creature, amount, props, cardSource);
        if (creature != Owner || amount <= 0 || _lastCtx == null)
        {
            return;
        }

        if (Owner.HasPower<IAmYoursPower>())
        {
            await CreatureCmd.LoseBlock(_lastCtx, Owner, amount, Owner);
            await PowerCmd.Apply<RegenPower>(_lastCtx, Owner, amount, Owner, null, silent: true);
            Log.Info($"[Amiya] i am yours: {amount} block -> {amount} regen");
        }

        if (PhaseChangeActive && cardSource != null && !Owner.HasPower<EmberPower>())
        {
            await Trigger(_lastCtx);
        }
    }

    /// <summary>完全格挡计数（神魂坚韧之歌）：敌人对你造成的伤害被完全格挡时 +1。</summary>
    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && result.WasFullyBlocked)
        {
            FullyBlockedCount++;
            Log.Info($"[Amiya] fully blocked +1 (total={FullyBlockedCount})");
        }
        return Task.CompletedTask;
    }

    /// <summary>湍流：每次抽到打击/防御时，抽一张牌。</summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        if (Owner.HasPower<TurbulencePower>()
            && Owner.Player != null
            && (card.Tags.Contains(CardTag.Strike) || card.Tags.Contains(CardTag.Defend)))
        {
            await CardPileCmd.Draw(choiceContext, 1m, Owner.Player);
        }
        // 抽到领袖牌：白光状态可能变化
        if (card is BaseAmiyaCard ac && ac.HasAmiyaTag(AmiyaTag.Leader) && Owner.Player != null)
        {
            await RefreshLeaderGlow(Owner.Player);
        }
    }

    // —— 回合流程 ——

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        TurnCount++;
        TurnCardPlayCount = 0;
        TurnAttackPlayedCount = 0;
        StrikesDefendsFreeThisTurn = false;
        PhaseChangeActive = false;
        _lastCtx = choiceContext;
        _lastCtx = choiceContext;

        // 领袖白光：回合开始（第1张=奇数）刷新手牌
        await RefreshLeaderGlow(player);

        // 今时明日的偏差：下回合格挡池结算
        if (PendingBlockNextTurn > 0)
        {
            await CreatureCmd.GainBlock(Owner, PendingBlockNextTurn, ValueProp.Move, null);
            PendingBlockNextTurn = 0;
        }

        // 预言显像：每回合开始将「层数」张存续先兆加入手牌（可叠加，Q45）
        if (Owner.GetPower<ProphecyImagePower>() is { } prophecy && prophecy.Amount > 0)
        {
            for (int i = 0; i < (int)prophecy.Amount; i++)
            {
                // 战斗内生成卡必须走 CombatState.CreateCard（内部注册进 _allCards），
                // RunState.CreateCard 的卡进战斗牌堆后 IsInCombat=true 但未注册 → 回合结束冲手牌时抛 "must be added to a CombatState"
                var omen = player.Creature.CombatState.CreateCard<SurvivalOmen>(player);
                await CardPileCmd.Add(omen, PileType.Hand, CardPilePosition.Bottom);
                if (omen.Affliction == null)
                {
                    await CardCmd.Afflict<AmiyaDemonLordAffliction>(omen, 1m);
                }
            }
        }

        Log.Info($"[Amiya] turn start TurnCount={TurnCount}, relics={player.Relics.Count}, paleTriggers={PaleRelicHelper.TriggerCount(player)}");

        // 求而不得之物：回合开始时若在消耗堆中，免费自动打出
        foreach (var card in PileType.Exhaust.GetPile(player).Cards.ToList())
        {
            if (card is UnattainableDesire or MyHeartFollows)
            {
                await CardCmd.AutoPlay(choiceContext, card, null);
                Log.Info($"[Amiya] auto-play from exhaust: {card.Id.Entry}");
            }
        }

        // 感染：回合结束时处理（AfterSideTurnEnd）
        // 苍白赐福/苍白花冠：除第一回合外，每回合开始触发形态转换（赐福 1 次 / 花冠 2 次；魔王形态下触发燃烬消耗）
        int paleTriggers = PaleRelicHelper.TriggerCount(player);
        if (TurnCount > 1)
        {
            for (int i = 0; i < paleTriggers; i++)
            {
                await Trigger(choiceContext);
            }
        }
    }

    /// <summary>回合结束时：弃牌堆中带感染词条的牌变形为 打击/防御（移除全部词条、保留升级态）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side != CombatSide.Player || Owner?.Player == null || !participants.Contains(Owner))
        {
            return;
        }

        // 临时力量（燎原/盛怒旋风）：回合结束全额移除
        if (TempStrengthThisTurn > 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, -TempStrengthThisTurn, Owner, null, silent: true);
            Log.Info($"[Amiya] temp strength -{TempStrengthThisTurn} removed at turn end");
            TempStrengthThisTurn = 0;
        }

        await ProcessInfection(choiceContext, Owner.Player);
    }

    /// <summary>阿米娅被击杀：记录死亡时的形态（结算画面展示对应形态死亡立绘）。</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature == Owner)
        {
            AmiyaDeathForm = Form;
            Log.Info($"[Amiya] death in form: {Form}");
        }
    }

    private async Task ProcessInfection(PlayerChoiceContext choiceContext, Player player)
    {
        try
        {
            var discard = PileType.Discard.GetPile(player);
            foreach (var card in discard.Cards.ToList())
            {
                if (card is not BaseAmiyaCard amiyaCard || !amiyaCard.HasAmiyaTag(AmiyaTag.Infection))
                {
                    continue;
                }

                Log.Info($"[Amiya] infection: transforming {card.Id.Entry} ...");
                CardModel target = card.Type == CardType.Attack
                    ? player.Creature.CombatState.CreateCard<AmiyaStrike>(player)
                    : player.Creature.CombatState.CreateCard<AmiyaDefend>(player);

                if (card.IsUpgraded)
                {
                    CardCmd.Upgrade(target);
                }
                await CardCmd.Transform(card, target);
                Log.Info($"[Amiya] infection: {card.Id.Entry} -> {target.Id.Entry}{(card.IsUpgraded ? "+" : "")} done");
                // 拒绝哀悼由 AmiyaTransformNotifyPatch 统一结算（变形补丁），此处不再重复触发
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Amiya] infection processing error: {ex}");
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Current = null;
        return Task.CompletedTask;
    }
}
