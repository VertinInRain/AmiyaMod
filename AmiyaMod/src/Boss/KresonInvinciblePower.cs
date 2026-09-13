using System.Collections.Generic;
using System.Threading.Tasks;
using Amiya.Art;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Amiya.Boss;

/// <summary>
/// 克雷松的【无敌】：体力不会减少。
///
/// 防护分三层（单靠一层都不够）：
///  1. ModifyDamageMultiplicative → 0：在格挡结算之前整体归零，
///     不仅不掉血，格挡也不会被消耗，伤害显示为 0；
///  2. 四个 ModifyHpLost* 守卫 → 0：拦住「无视格挡」的生命损失（原版 BufferPower 用的就是这条）；
///  3. 外观 HpDisplay = InfiniteWithoutNumbers：血条显示 ∞。
///
/// 解除窗口（锚点）用 DynamicVar 里的计数器实现，不走「移除再施加状态」——
/// 那样会有 0.2 秒停顿、而且可能被其它效果拦截。
/// 窗口语义：打出锚点后开启，覆盖接下来的 1 次（升级后 2 次）出牌，
/// 在这张牌的效果结束时消耗掉一次；回合结束（玩家方回合结束）无条件恢复无敌。
/// </summary>
public sealed class KresonInvinciblePower : CustomPowerModel
{
    private const string WindowKey = "Window";
    private const string PendingKey = "Pending";
    private const string LiftedKey = "Lifted";

    /// <summary>开启窗口的那次出牌（窗口在它自己的效果结束后才真正打开）。</summary>
    private CardPlay? _opener;

    public override string? CustomPackedIconPath => PlaceholderArt.Power("no_draw_power");

    public override PowerType Type => PowerType.Buff;

    /// <summary>不带角标数字（0/1 的层数没有意义）。</summary>
    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar(WindowKey, 0m),
        new DynamicVar(PendingKey, 0m),
        new DynamicVar(LiftedKey, 0m)
    };

    /// <summary>剩余「无敌关闭」的出牌次数。</summary>
    private int Window
    {
        get => DynamicVars[WindowKey].IntValue;
        set => DynamicVars[WindowKey].BaseValue = value;
    }

    private bool Lifted => DynamicVars[LiftedKey].IntValue > 0;

    /// <summary>当前是否处于无敌（体力不会减少）。</summary>
    public bool Invincible => !Lifted && Window <= 0;

    /// <summary>刚上身时就把血条切成 ∞（此时生物节点可能还没建好，贴图部分会安全跳过）。</summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        RefreshPresentation();
        return Task.CompletedTask;
    }

    /// <summary>锚点结算：请求「这张牌效果结束后」开启解除无敌窗口。</summary>
    public void OpenAfterCard(CardPlay opener, int cardPlays)
    {
        _opener = opener;
        DynamicVars[PendingKey].BaseValue = cardPlays;
    }

    /// <summary>终点自爆前永久解除无敌（自爆必须能杀死自己）。</summary>
    public void LiftForever()
    {
        DynamicVars[LiftedKey].BaseValue = 1m;
        Window = 0;
        DynamicVars[PendingKey].BaseValue = 0m;
        _opener = null;
        RefreshPresentation();
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
        => Invincible && target == Owner ? 0m : 1m;

    public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard(target, amount);

    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard(target, amount);

    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard(target, amount);

    public override decimal ModifyHpLostAfterOstyLate(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
        => Guard(target, amount);

    private decimal Guard(Creature target, decimal amount)
        => Invincible && target == Owner && amount > 0m ? 0m : amount;

    /// <summary>
    /// 窗口在「这张牌的效果结束」时才开启，所以打出锚点的那次出牌本身不会消耗窗口；
    /// AfterCardPlayedLate 是所有模型 AfterCardPlayed 都跑完之后才轮到的第二遍，是唯一的正确时机。
    /// </summary>
    public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Lifted)
        {
            return Task.CompletedTask;
        }
        if (_opener != null && ReferenceEquals(cardPlay, _opener))
        {
            _opener = null;
            // 累加：窗口已经开着时又打一张锚点，次数叠加（而不是覆盖掉原来的窗口）
            Window = System.Math.Min(4, Window + DynamicVars[PendingKey].IntValue);
            DynamicVars[PendingKey].BaseValue = 0m;
            RefreshPresentation();
            return Task.CompletedTask;
        }
        if (Window > 0)
        {
            Window--;
            if (Window <= 0)
            {
                RefreshPresentation();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>回合结束无条件恢复无敌（「本回合」限制）。</summary>
    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Player && !Lifted)
        {
            bool changed = Window > 0 || DynamicVars[PendingKey].IntValue > 0 || _opener != null;
            Window = 0;
            DynamicVars[PendingKey].BaseValue = 0m;
            _opener = null;
            if (changed)
            {
                RefreshPresentation();
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>血条 ∞ + 贴图切换（无敌 / 解除无敌）。</summary>
    private void RefreshPresentation()
    {
        if (Owner?.CombatState == null)
        {
            return;
        }
        Owner.HpDisplay = Invincible ? HpDisplay.InfiniteWithoutNumbers : HpDisplay.Normal;
        KresonVisuals.SetState(Owner, Invincible ? KresonVisuals.State.Invincible : KresonVisuals.State.Released);
    }
}
