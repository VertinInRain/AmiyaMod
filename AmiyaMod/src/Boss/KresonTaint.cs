using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

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
