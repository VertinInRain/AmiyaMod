using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Powers;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Cards;

/// <summary>
/// 神圣复苏（远古，技能，3/2 费）：固有，虚无。
/// 打出后 2 回合内无法打出非攻击牌；第 3 回合开始时，将体力恢复到战斗开始状态
/// （回到最大生命），并对所有敌人造成恢复量 2 倍的伤害（纯伤害，不吃加成）。
/// 实现 ITomeCard：尘封魔典（DustyTome）会固定把它（升级后）加入牌组。
/// </summary>
public sealed class SeeLight : BaseAmiyaCard, ITomeCard
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { CardKeyword.Innate, CardKeyword.Ethereal, CardKeyword.Exhaust };

    public SeeLight() : base(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override List<(string, string)>? CardLocalization => new()
    {
        ("title", "神圣复苏"),
        ("description", "#打出后 2 回合内无法打出非攻击牌；第 3 回合开始时，将体力恢复到战斗开始状态，并对所有敌人造成恢复量 2 倍的伤害。")
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SeeLightPower>(choiceContext, Owner!.Creature, 1m, Owner.Creature, null, silent: true);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3 → 2
    }
}

/// <summary>神圣复苏 Power：封禁非攻击牌 2 回合；第 3 回合开始回血 + 全体敌人纯伤害。</summary>
public sealed class SeeLightPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/SeeLightPower.png";
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    private int _turn = 1; // 打出回合 = 第 1 回合；封禁第 1、2 回合；第 3 回合开始结算

    public override List<(string, string)>? Localization => new()
    {
        ("title", "神圣复苏"),
        ("description", "无法打出非攻击牌。第 3 回合开始时，将体力恢复到战斗开始状态，并对所有敌人造成恢复量 2 倍的伤害。")
    };

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        return card.Type == CardType.Attack;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        await base.AfterPlayerTurnStart(choiceContext, player);
        // 多人：钩子会对每个玩家的回合开始触发，只数自己主人的回合，
        // 否则两个玩家的回合都会让 _turn 自增（3 回合的等待会变成 1.5 回合）
        if (Owner?.Player == null || player != Owner.Player)
        {
            return;
        }
        _turn++;
        if (_turn < 3)
        {
            return;
        }
        // 恢复到"进入战斗时"的生命值（快照），而非满血
        int target = FormManagerPower.Of(Owner)?.CombatStartHp ?? Owner.MaxHp;
        decimal restored = Math.Max(0m, (decimal)(target - Owner.CurrentHp));
        await CreatureCmd.Heal(Owner, restored);
        Log.Info($"[Amiya] sacred revival: healed {restored}");
        decimal damage = restored * 2m;
        if (damage > 0m && Owner.CombatState != null)
        {
            // dealer=null：纯伤害，不吃力量等任何加成
            await CreatureCmd.Damage(choiceContext, Owner.CombatState.Enemies, damage, ValueProp.Move, null, null, null);
            await CombatManager.Instance.CheckWinCondition();
            Log.Info($"[Amiya] sacred revival: {damage} damage to all enemies");
        }
        await PowerCmd.Remove(this);
    }
}
