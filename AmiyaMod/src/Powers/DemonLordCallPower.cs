using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Amiya.Powers;

/// <summary>
/// 魔王之唤（Counter Power）：由 FormManagerPower 在打出第一张魔王牌时懒加载施加，
/// 之后每张魔王牌 +1 层。达到 7 层进入魔王形态（黑冠/燃烬/裁决由 FormManagerPower.EnterDemonLord 授予）。
/// </summary>
public sealed class DemonLordCallPower : CustomPowerModel
{
    public override string? CustomPackedIconPath => "res://Amiya/images/powers/DemonLordCallPower.png";
    public static DemonLordCallPower? Current { get; private set; }

    /// <summary>
    /// 多人安全取用：Current 只指向"本地玩家"的实例，结算逻辑必须按实际归属者取，
    /// 否则两名阿米娅玩家会各读自己的层数导致状态分歧（掉线）。
    /// </summary>
    public static DemonLordCallPower? Of(Creature? creature) => creature?.GetPower<DemonLordCallPower>();

    public static DemonLordCallPower? Of(Player? player) => Of(player?.Creature);

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization => new()
    {
        ("title", "魔王之唤"),
        ("description", "达到 7 层时进入魔王形态。")
    };

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Current = this;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        Current = null;
        return Task.CompletedTask;
    }
}
