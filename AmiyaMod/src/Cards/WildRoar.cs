using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>旷野轰鸣（稀有，墓园）：每回合结束时，对所有敌人造成 16/20 点伤害（仅我方回合结束，Q48）。</summary>
public sealed class WildRoar : BaseAmiyaCard
{
    public override AmiyaTag AmiyaTags => AmiyaTag.Graveyard;

    public WildRoar() : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "旷野轰鸣"),
        ("description", "#每回合结束时，对所有敌人造成 {IfUpgraded:show:20|16} 点伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WildRoarPower>(choiceContext, Owner!.Creature, IsUpgraded ? 20m : 16m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
    }
}

/// <summary>旷野轰鸣 Power（层数=伤害）。我方回合结束时结算。</summary>
public sealed class WildRoarPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/WildRoarPower.png";
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "旷野轰鸣"),
        ("description", "每回合结束时，对所有敌人造成等量伤害。")
    };

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == CombatSide.Player && participants.Contains(Owner))
        {
            await CreatureCmd.Damage(choiceContext, Owner.CombatState.Enemies, Amount, ValueProp.Move, Owner, null, null);
        }
    }
}
