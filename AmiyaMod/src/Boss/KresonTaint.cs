using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Amiya.Boss;

/// <summary>
/// 克雷松专用的「污染」。
///
/// 为什么不用原版 Tainted：原版 <c>Tainted.CanAfflictCardType</c> 写死只允许【技能牌】，
/// 对攻击牌 <c>CardCmd.Afflict</c> 会静默返回 null（不报错、也不生效）。
/// 克雷松的意图2 要求「攻击或技能牌」都能被污染，所以这里自建一个规则相同的词条：
///   · 可叠加（2 层 = 同一个词条实例 Amount = 2）
///   · 卡面覆盖层复用官方 tainted（见 AmiyaAfflictionOverlayPatch）
///   · 打出后获得等量【污染】（TaintedPower），本回合受到的攻击伤害增加 —— 由 KresonTaintSourcePower 实现
/// </summary>
public sealed class KresonTaint : AfflictionModel, ILocalizationProvider
{
    public override bool IsStackable => true;

    public override bool HasExtraCardText => true;

    /// <summary>与原版不同之处：攻击牌与技能牌都可以被污染。</summary>
    public override bool CanAfflictCardType(CardType cardType)
        => cardType is CardType.Attack or CardType.Skill;

    public List<(string, string)>? Localization => new()
    {
        ("title", "污染"),
        ("description", "打出带污染的牌后，获得等量【污染】：本回合受到的攻击伤害增加。"),
        ("extraCardText", "打出后获得 {Amount} 层污染。")
    };
}

/// <summary>
/// 污染来源（挂在每个玩家身上的隐形状态）：打出带【污染】的牌时，获得等量层数的污染。
/// 原版对应实现是 VitalSparkPower（寄生棱晶），但它会在开局污染所有技能牌，
/// 克雷松只要「被点名的牌才触发」，所以只保留 AfterCardPlayed 这一半逻辑。
/// </summary>
public sealed class KresonTaintSourcePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    /// <summary>纯机制状态，不显示图标（机制说明写在【污染】的词条文本里）。</summary>
    protected override bool IsVisibleInternal => false;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null || cardPlay.Card.Owner != Owner.Player)
        {
            return;
        }
        if (cardPlay.Card.Affliction is KresonTaint taint && taint.Amount > 0)
        {
            await PowerCmd.Apply<TaintedPower>(choiceContext, Owner, taint.Amount, null, null);
        }
    }
}

/// <summary>
/// 【育苗】：每回合开始时，为玩家牌组中随机 2 张攻击或技能牌附加 2 层污染（优先选择当前无污染的）。
/// 挂在每个玩家身上（InstanceType None，一人一份），自己回合开始时结算自己那份。
///
/// 说明：「牌组」在战斗中以战斗牌堆的形式存在（PlayerCombatState.AllCards = 抽牌堆+手牌+弃牌堆+消耗堆+出牌区），
/// 附加的污染是战斗内状态（游戏存档里不保存词条），所以实际是对这套战斗卡牌生效。
/// </summary>
public sealed class KresonNursingPower : CustomPowerModel
{
    /// <summary>每次点名的牌数。</summary>
    public const int CardsPerTurn = 2;

    /// <summary>每张牌附加的污染层数。</summary>
    public const int StacksPerCard = 2;

    public override string? CustomPackedIconPath => "res://Amiya/images/powers/KresonNursingPower.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "育苗"),
        ("description", "每回合开始时，为玩家牌组中随机 2 张攻击或技能牌附加 2 层【污染】（优先选择当前无污染的）。")
    };

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player == null || player != Owner.Player)
        {
            return;
        }
        await KresonTaintHelper.ApplyToPlayer(choiceContext, player, CardsPerTurn, StacksPerCard);
    }
}

/// <summary>污染施加的公共逻辑（育苗 / 其它来源共用）。</summary>
public static class KresonTaintHelper
{
    /// <summary>
    /// 给该玩家在 手牌 + 抽牌堆 + 弃牌堆 里的随机 N 张攻击/技能牌各附加 amount 层污染，
    /// 优先挑还没被污染的牌；随机走引擎自己的 PickRandomTargets
    /// （RunState.Rng.CombatCardGeneration，多人两端一致）。
    /// </summary>
    public static async Task ApplyToPlayer(PlayerChoiceContext choiceContext, Player player, int cards, int amount)
    {
        KresonTaint affliction = ModelDb.Affliction<KresonTaint>();
        List<CardModel> candidates = PileType.Hand.GetPile(player).Cards
            .Concat(PileType.Draw.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Where(c => affliction.CanAfflict(c))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }
        List<CardModel> clean = candidates.Where(c => c.Affliction == null).ToList();
        List<CardModel> already = candidates.Where(c => c.Affliction is KresonTaint).ToList();
        var picks = affliction.PickRandomTargets(player.RunState.Rng, clean, cards).ToList();
        if (picks.Count < cards)
        {
            picks.AddRange(affliction.PickRandomTargets(player.RunState.Rng, already, cards - picks.Count));
        }
        if (picks.Count == 0)
        {
            return;
        }
        await CardCmd.AfflictAndPreview<KresonTaint>(picks, amount, CardPreviewStyle.HorizontalLayout);
    }
}
