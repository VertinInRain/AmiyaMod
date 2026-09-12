using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Powers;

/// <summary>
/// 黑冠（魔王形态）：造成的伤害翻倍（仅对敌人的伤害，自伤不翻倍）；
/// 每打出一张攻击牌，受到 2 点伤害（可被格挡）。
/// </summary>
public sealed class BlackCrownPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/BlackCrownPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "黑冠"),
        ("description", "造成的伤害翻倍。每打出一张攻击牌，受到 2 点伤害。")
    };

    public override decimal ModifyDamageMultiplicative(MegaCrit.Sts2.Core.Entities.Creatures.Creature? target, decimal amount, ValueProp props, MegaCrit.Sts2.Core.Entities.Creatures.Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        // 仅持有者对敌人造成的伤害翻倍；自伤（target==Owner）不翻倍（旧版 bug 源）
        if (dealer == Owner && target != Owner)
        {
            return 2m;
        }
        return 1m;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (cardPlay.Card?.Owner?.Creature != Owner || cardPlay.Card.Type != CardType.Attack)
        {
            return;
        }
        // 每出一张攻击牌自伤 2（固定值：Unpowered 不吃力量/易伤，可被格挡，走伤害管线）
        await CreatureCmd.Damage(choiceContext, Owner, 2m, ValueProp.Move | ValueProp.Unpowered, Owner, null, null);
    }
}
