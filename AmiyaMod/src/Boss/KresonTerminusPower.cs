using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amiya.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的【终点】：8 个克雷松回合后引爆（角标 = 剩余回合数，描述里的 {Amount} 同一个数）。
/// 结算顺序：
///   1. 先永久解除无敌（否则自爆会被自己的无敌挡住）；
///   2. 消耗所有玩家 抽牌堆/手牌/弃牌堆 中的每一张【锚点】——每张独立结算一次
///      「无视格挡的生命损失」：普通 7 点，升级后的锚点 9 点；
///   3. 克雷松自身失去全部生命（战斗结束；若玩家已被炸死则直接判负）。
///
/// 计数方式：克雷松自己的回合结束时把 Amount 减 1，减到最后一回合后结算——
/// 第 8 个克雷松回合结束即第 9 回合开始，与设计描述一致。
/// </summary>
public sealed class KresonTerminusPower : CustomPowerModel
{
    /// <summary>倒计时初值（克雷松自己的回合数）。</summary>
    public const int BaseTurns = 8;

    public override string? CustomPackedIconPath => "res://Amiya/images/powers/KresonTerminusPower.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "终点"),
        ("description", "{Amount} 回合后，消耗玩家抽牌堆、手牌、弃牌堆中所有【锚点】，每张令玩家失去 7（升级后 9）点生命，随后自身失去所有生命。")
    };

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy || Owner?.CombatState == null || Owner.IsDead)
        {
            return;
        }
        if (Amount > 1)
        {
            SetAmount(Amount - 1, silent: true);
            return;
        }
        await Detonate(choiceContext);
    }

    private async Task Detonate(PlayerChoiceContext choiceContext)
    {
        ICombatState combatState = Owner.CombatState;

        // 1) 解除无敌：自爆必须能杀死自己
        if (Owner.GetPower<KresonInvinciblePower>() is { } invincible)
        {
            invincible.LiftForever();
        }

        // 2) 消耗所有玩家三个牌堆里的锚点（每个玩家的牌堆归属明确，多人下各算各的）
        foreach (Player player in combatState.Players.ToList())
        {
            List<CardModel> anchors = PileType.Draw.GetPile(player).Cards.Where(c => c is Anchor)
                .Concat(PileType.Hand.GetPile(player).Cards.Where(c => c is Anchor))
                .Concat(PileType.Discard.GetPile(player).Cards.Where(c => c is Anchor))
                .ToList();
            foreach (CardModel anchor in anchors)
            {
                decimal hpLoss = anchor.IsUpgraded ? 9m : 7m;
                await CreatureCmd.Damage(choiceContext, player.Creature, hpLoss,
                    ValueProp.Unblockable | ValueProp.Unpowered, Owner, null, null);
            }
        }

        // 3) 自身失去全部生命 → 战斗结束
        await CreatureCmd.Kill(Owner);
        // 自爆发生在回合结束流程里，补一次胜负判定，避免战斗不结算
        await CombatManager.Instance.CheckWinCondition();
    }
}
