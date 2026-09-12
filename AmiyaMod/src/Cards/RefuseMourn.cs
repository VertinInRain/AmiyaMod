using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>拒绝哀悼（罕见）：每消耗或变化一张牌，对所有敌人造成 4/6 点伤害。</summary>
public sealed class RefuseMourn : BaseAmiyaCard
{
    public RefuseMourn() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "拒绝哀悼"),
        ("description", "#每消耗或变化一张牌，对所有敌人造成 {IfUpgraded:show:6|4} 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<RefuseMournPower>(choiceContext, Owner!.Creature, IsUpgraded ? 6m : 4m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>
/// 拒绝哀悼 Power（层数=伤害）。消耗由 AfterCardExhausted 结算；
/// 变化（变形）由 AmiyaTransformNotifyPatch 统一结算。
/// </summary>
public sealed class RefuseMournPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/RefuseMournPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "拒绝哀悼"),
        ("description", "每消耗或变化一张牌，对所有敌人造成等量伤害。")
    };

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        await base.AfterCardExhausted(choiceContext, card, causedByEthereal);
        if (card.Owner?.Creature == Owner && Owner.CombatState != null)
        {
            await Amiya.Patches.AmiyaTransformNotify.DealDamageToAllEnemies(choiceContext, Owner, Amount);
        }
    }
}
